using System.Buffers;
using System.Collections.ObjectModel;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Text.Json;

using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;

using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

using pdns_dhcp.Dns;

namespace pdns_dhcp.PowerDns;

public class PowerDnsHandler : ConnectionHandler
{
	delegate MethodBase? HandlerConverter(in JsonElement element);

	private static readonly ReadOnlyDictionary<string, HandlerConverter> Converters;

	private readonly ILogger<PowerDnsHandler> _logger;
	private readonly DnsRepository _repository;

	static PowerDnsHandler()
	{
		Dictionary<string, HandlerConverter> converters = new(StringComparer.OrdinalIgnoreCase)
		{
			["initialize"] = ToInitialize,
			["lookup"] = ToLookup
		};

		Converters = converters.AsReadOnly();

		static InitializeMethod ToInitialize(in JsonElement element)
		{
			return new(element.Deserialize(PowerDnsSerializerContext.Default.InitializeParameters)!);
		}

		static LookupMethod ToLookup(in JsonElement element)
		{
			return new(element.Deserialize(PowerDnsSerializerContext.Default.LookupParameters)!);
		}
	}

	public PowerDnsHandler(DnsRepository repository, ILogger<PowerDnsHandler> logger)
	{
		_logger = logger;
		_repository = repository;
	}

	public override async Task OnConnectedAsync(ConnectionContext connection)
	{
		var input = connection.Transport.Input;
		JsonReaderState state = default;
		using ArrayPoolBufferWriter<byte> json = new();
		using ArrayPoolBufferWriter<byte> buffer = new();
		using var writer = connection.Transport.Output.AsStream();
		while (!connection.ConnectionClosed.IsCancellationRequested)
		{
			if (await ReadAsync(input, connection.ConnectionClosed) is not { IsCanceled: false } read)
			{
				return;
			}

			foreach (var memory in read.Buffer)
			{
				buffer.Write(memory.Span);
				if (!ConsumeJson(buffer, json, ref state))
				{
					continue;
				}

				Reply reply = BoolReply.False;
				try
				{
					MethodBase method;
					try
					{
						using var jsonDocument = JsonDocument.Parse(json.WrittenMemory);
						var root = jsonDocument.RootElement;
						if (!root.TryGetProperty("method", out var methodElement))
						{
							_logger.LogWarning("Json Document missing required property method: {document}", jsonDocument);
							continue;
						}

						if (Parse(methodElement, root.GetProperty("parameters")) is not { } methodLocal)
						{
							continue;
						}

						method = methodLocal;
					}
					finally
					{
						json.Clear();
						state = default;
					}

					reply = await Handle(method, connection.ConnectionClosed).ConfigureAwait(false);
				}
				catch (Exception e)
				{
					_logger.LogError(e, "Error");
				}
				finally
				{
					await JsonSerializer.SerializeAsync(writer, reply, PowerDnsSerializerContext.Default.Reply, connection.ConnectionClosed)
						.ConfigureAwait(false);
				}
			}

			input.AdvanceTo(read.Buffer.End);
		}

		static async ValueTask<ReadResult?> ReadAsync(PipeReader reader, CancellationToken cancellationToken)
		{
			try
			{
				return await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
			}
			catch (OperationCanceledException)
			{
				return null;
			}
		}

		static bool ConsumeJson(ArrayPoolBufferWriter<byte> inflight, ArrayPoolBufferWriter<byte> json, ref JsonReaderState state)
		{
			bool final = false;
			Utf8JsonReader reader = new(inflight.WrittenSpan, false, state);
			while (!final && reader.Read())
			{
				if (reader.TokenType == JsonTokenType.EndObject && reader.CurrentDepth == 0)
				{
					final = true;
				}
			}

			state = reader.CurrentState;
			int consumed = (int)reader.BytesConsumed;
			if (consumed > 0)
			{
				json.Write(inflight.WrittenSpan[..consumed]);

				Span<byte> buffer = default;
				var remaining = inflight.WrittenCount - consumed;
				if (remaining > 0)
				{
					buffer = inflight.GetSpan(remaining)[..remaining];
					inflight.WrittenSpan[consumed..].CopyTo(buffer);
				}

				// clear only clears up until WrittenCount
				// thus data after write-head is safe
				inflight.Clear();
				if (!buffer.IsEmpty)
				{
					inflight.Write(buffer);
				}
			}

			return final;
		}

		static MethodBase? Parse(in JsonElement element, in JsonElement parameters)
		{
			HandlerConverter? converter = default;
			return element.GetString() switch
			{
				null => null,
				{ } methodName when !Converters.TryGetValue(methodName, out converter) => new Method<MethodParameters>(methodName, parameters.Deserialize(PowerDnsSerializerContext.Default.MethodParameters)!),
				_ => converter(parameters)
			};
		}
	}

	private ValueTask<Reply> Handle(MethodBase method, CancellationToken cancellationToken = default)
	{
		return method switch
		{
			InitializeMethod { Parameters: { } init } => HandleInitialize(init),
			LookupMethod { Parameters: { } lookup } => HandleLookup(lookup),

			_ => LogUnhandled(_logger, method)
		};

		static ValueTask<Reply> LogUnhandled(ILogger logger, MethodBase method)
		{
			logger.LogWarning("Unhandled {Method}", method);
			return ValueTask.FromResult<Reply>(BoolReply.False);
		}
	}

	private ValueTask<Reply> HandleInitialize(InitializeParameters parameters)
	{
		if (_logger.IsEnabled(LogLevel.Information))
		{
			_logger.LogInformation("Handling {parameters}", parameters);
		}

		return ValueTask.FromResult<Reply>(BoolReply.True);
	}

	private ValueTask<Reply> HandleLookup(LookupParameters parameters)
	{
		AddressFamily? qtype = parameters.Qtype.ToUpperInvariant() switch
		{
			"ANY" => AddressFamily.Unknown,
			"A" => AddressFamily.InterNetwork,
			"AAAA" => AddressFamily.InterNetworkV6,
			_ => default(AddressFamily?)
		};

		if (qtype is null)
		{
			_logger.LogWarning("Unhandled QType {QType}", parameters.Qtype);
			return ValueTask.FromResult<Reply>(BoolReply.False);
		}

		if (_logger.IsEnabled(LogLevel.Information))
		{
			_logger.LogInformation("Lookup {key} in {family}", parameters.Qname, parameters.Qtype);
		}

		return FindByName(((AddressFamily)qtype, parameters.Qname.AsMemory()), _repository, _logger);

		static async ValueTask<Reply> FindByName((AddressFamily Family, ReadOnlyMemory<char> Qname) query, DnsRepository repository, ILogger logger)
		{
			QueryResult[] records = [];

			var qname = query.Qname.Trim().TrimEnd(".");
			if (qname.Span.IsWhiteSpace())
			{
				goto exitEmpty;
			}

			var results = await repository.FindAsync(record =>
			{
				if ((record.RecordType & query.Family) != record.RecordType)
				{
					return false;
				}

				return qname.Span.Equals(record.FQDN, StringComparison.OrdinalIgnoreCase);
			}).ConfigureAwait(false);

			if (results.Count == 0)
			{
				goto exitEmpty;
			}

			records = new QueryResult[results.Count];
			for (int i = 0; i < results.Count; i++)
			{
				DnsRecord record = results[i];
#pragma warning disable CS8509 // RecordType is by convention InterNetwork or InterNetworkV6
				records[i] = new(record.RecordType switch
#pragma warning restore
				{
					AddressFamily.InterNetwork => "A",
					AddressFamily.InterNetworkV6 => "AAAA"
				}, record.FQDN, record.Address.ToString(), (int)record.Lifetime.TotalSeconds);
			}

		exitEmpty:
			return new LookupReply(records);
		}
	}
}
