using System.Buffers;
using System.Text.Json;

using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.HighPerformance;
using Microsoft.Toolkit.HighPerformance.Buffers;

using pdns_dhcp.Dns;

namespace pdns_dhcp.PowerDns;

public class PowerDnsHandler : ConnectionHandler
{
	private readonly ILogger<PowerDnsHandler> _logger;
	private readonly DnsRepository _repository;

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
			var read = await input.ReadAsync(connection.ConnectionClosed).ConfigureAwait(false);
			if (read.IsCanceled)
			{
				return;
			}

			foreach (var memory in read.Buffer)
			{
				buffer.Write(memory.Span);
				if (ConsumeJson(buffer, json, ref state))
				{
					var method = JsonSerializer.Deserialize(json.WrittenSpan, MethodContext.Default.Method)!;
					json.Clear();
					state = default;

					Reply reply = BoolReply.False;
					try
					{
						reply = await Handle(method, connection.ConnectionClosed).ConfigureAwait(false);
					}
					catch (Exception e) { }

					await JsonSerializer.SerializeAsync(writer, reply, ReplyContext.Default.Reply, connection.ConnectionClosed)
						.ConfigureAwait(continueOnCapturedContext: false);
				}
			}

			input.AdvanceTo(read.Buffer.End);
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
	}

	private ValueTask<Reply> Handle(Method method, CancellationToken cancellationToken = default)
	{
		return method switch
		{
			InitializeMethod { Parameters: { } init } => HandleInitialize(init),
			LookupMethod { Parameters: { } lookup } => HandleLookup(lookup),

			_ => LogUnhandled(_logger, method)
		};

		static ValueTask<Reply> LogUnhandled(ILogger logger, Method method)
		{
			logger.LogWarning("Unhandled Method {Method}", method);
			return ValueTask.FromResult<Reply>(BoolReply.False);
		}
	}

	private ValueTask<Reply> HandleInitialize(InitializeParameters parameters)
	{
		return ValueTask.FromResult<Reply>(BoolReply.True);
	}

	private ValueTask<Reply> HandleLookup(LookupParameters parameters)
	{
		switch (parameters.Qtype.ToUpperInvariant())
		{
			case "ANY":
				return ValueTask.FromResult<Reply>(new LookupReply([]));

			case "A":
				return ValueTask.FromResult<Reply>(BoolReply.False);

			case "AAAA":
				_logger.LogInformation("AAAA request: {Options}", parameters);
				return ValueTask.FromResult<Reply>(BoolReply.False);

			default:
				_logger.LogWarning("Unhandled QType {QType}", parameters.Qtype);
				return ValueTask.FromResult<Reply>(BoolReply.False);
		}
	}
}
