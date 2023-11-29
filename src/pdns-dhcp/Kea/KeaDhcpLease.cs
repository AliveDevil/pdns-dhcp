using System.Buffers;
using System.Globalization;

using DotNext.Buffers;

namespace pdns_dhcp.Kea;

public static class KeaDhcpLease
{
	private static ReadOnlySpan<char> EscapeTag => ['&', '#', 'x'];

	// ref: https://github.com/isc-projects/kea/blob/Kea-2.5.3/src/lib/util/csv_file.cc#L479
	public static string Unescape(in ReadOnlySpan<char> text)
	{
		return text.IndexOf(EscapeTag) switch
		{
			-1 => text.ToString(),
			int i => SlowPath(i, text)
		};

		static string SlowPath(int esc_pos, in ReadOnlySpan<char> text)
		{
			SpanReader<char> reader = new(text);
			ArrayBufferWriter<char> writer = new(text.Length);
			while (reader.RemainingCount > 0)
			{
				writer.Write(reader.Read(esc_pos));
				reader.Advance(EscapeTag.Length);
				if (EscapeTag.Length <= reader.RemainingCount - 2)
				{
					var digits = reader.Read(2);
					if (byte.TryParse(digits, NumberStyles.AllowHexSpecifier, null, out var escaped_char))
					{
						writer.Write((char)escaped_char);
					}
					else
					{
						writer.Write(EscapeTag);
						writer.Write(digits);
					}
				}

				esc_pos = reader.RemainingSpan.IndexOf(EscapeTag);
				if (esc_pos == -1)
				{
					writer.Write(reader.ReadToEnd());
				}
			}

			return writer.WrittenSpan.ToString();
		}
	}
}
