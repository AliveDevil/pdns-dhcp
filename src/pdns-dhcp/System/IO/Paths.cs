#if !(LINUX || MACOS || WINDOWS)
#define TARGET_ANY
#endif

#if TARGET_ANY || LINUX
#define TARGET_LINUX
#endif
#if TARGET_ANY || MACOS
#define TARGET_MACOS
#endif
#if TARGET_ANY || WINDOWS
#define TARGET_WINDOWS
#endif

using System.Buffers;

using Microsoft.Toolkit.HighPerformance.Buffers;

namespace System.IO;

public static class PathEx
{
	public static string ExpandPath(string path)
	{
#if TARGET_ANY
		if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
		{
#endif
#if TARGET_LINUX || TARGET_MACOS
			return ExpandPathUnixImpl(path);
#endif
#if TARGET_ANY
		}
		else if (OperatingSystem.IsWindows())
		{
#endif
#if TARGET_WINDOWS
			return ExpandPathWindowsImpl(path);
#endif
#if TARGET_ANY
		}
		else
		{
			throw new PlatformNotSupportedException();
		}
#endif
	}

#if TARGET_LINUX || TARGET_MACOS
	private static string ExpandPathUnixImpl(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return path;
		}

		var reader = new SequenceReader<char>(new(path.AsMemory()));
		if (!reader.TryReadTo(out ReadOnlySpan<char> read, '$'))
		{
			return path;
		}

		using ArrayPoolBufferWriter<char> result = new();
		while (true)
		{
			result.Write(read);

			int skip = 0;
			if (reader.UnreadSpan[0] == '{' && reader.UnreadSpan.IndexOf('}') is not -1 and int s)
			{
				read = reader.UnreadSpan[1..s];
				skip = s + 1;
			}
			else
			{
				var propertyReader = new SequenceReader<char>(reader.UnreadSequence);
				while (!propertyReader.End && propertyReader.UnreadSpan[0] is char c && (c == '_' || char.IsAsciiLetterOrDigit(c)))
				{
					propertyReader.Advance(1);
				}

				read = reader.UnreadSpan[..(int)propertyReader.Consumed];
				skip = read.Length;
			}

			if (skip != 0 && Environment.GetEnvironmentVariable(read.ToString()) is { } env)
			{
				result.Write(env);
				reader.Advance(skip);
			}
			else
			{
				result.Write(['$']);
			}

			if (!reader.TryReadTo(out read, '$'))
			{
				break;
			}
		}

		result.Write(reader.UnreadSpan);
		return result.WrittenSpan.ToString();
	}
#endif

#if TARGET_WINDOWS
	private static string ExpandPathWindowsImpl(string path)
	{
		return Environment.ExpandEnvironmentVariables(path);
	}
#endif
}
