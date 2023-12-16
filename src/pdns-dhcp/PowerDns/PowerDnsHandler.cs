using System.Buffers;
using System.Text.Json;

using Microsoft.AspNetCore.Connections;
using Microsoft.Toolkit.HighPerformance;
using Microsoft.Toolkit.HighPerformance.Buffers;

namespace pdns_dhcp.PowerDns;

public class PowerDnsHandler : ConnectionHandler
{
	public override async Task OnConnectedAsync(ConnectionContext connection)
	{
		var input = connection.Transport.Input;
		JsonReaderState state = default;
		using ArrayPoolBufferWriter<byte> json = new();
		using ArrayPoolBufferWriter<byte> buffer = new();
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
					var method = JsonSerializer.Deserialize<Method>(json.WrittenSpan);
					json.Clear();
					state = default;
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
}
