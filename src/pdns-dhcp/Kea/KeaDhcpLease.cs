using System.Buffers;
using System.Globalization;

using DotNext.Buffers;

using Microsoft.Toolkit.HighPerformance.Buffers;

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
			using ArrayPoolBufferWriter<char> writer = new(text.Length);
			while (reader.RemainingCount > 0)
			{
				writer.Write(reader.Read(esc_pos));
				reader.Advance(EscapeTag.Length);

				bool converted = false;
				char escapedChar = default;
				if (reader.RemainingCount >= 2)
				{
					if (byte.TryParse(reader.RemainingSpan[..2], NumberStyles.AllowHexSpecifier, null, out var b))
					{
						converted = true;
						escapedChar = (char)b;
						reader.Advance(2);
					}
				}

				if (converted)
				{
					writer.Write(escapedChar);
				}
				else
				{
					writer.Write(EscapeTag);
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
