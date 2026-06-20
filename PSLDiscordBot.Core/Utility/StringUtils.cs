using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSLDiscordBot.Core.Utility;
public static class StringUtils
{
	public static string WithMaxLength(this string str, int maxLength)
	{
		return str[0..Math.Min(str.Length, maxLength)];
	}
	public static string ToSnakeCase(this string text, char delimiter = '_')
	{
		// consider following strings:
		// SomeAPIEndpoint -> some_api_endpoint
		// SomeApiEndpoint -> some_api_endpoint
		// someAPIEndpoint -> some_api_endpoint

		ArgumentNullException.ThrowIfNull(text);
		int currentIndex = FindNextUpperCaseIndex(0);
		if (currentIndex == -1) return text;

		List<int> indexes = [];
		while (currentIndex != -1)
		{
			indexes.Add(currentIndex);
			currentIndex = FindNextUpperCaseIndex(currentIndex + 1);
		}
		if (indexes[0] != 0) indexes.Insert(0, 0);

		StringBuilder sb = new();
		for (int i = 0; i < indexes.Count; i++)
		{
			int current = indexes[i];
			int? next = i + 1 >= indexes.Count ? null : indexes[i + 1];

			if (next is null)
			{
				sb.Append(text[current..].ToLower());
				break;
			}
			// ex. SomeText
			//     ^   ^
			if (current + 1 != next)
			{
				sb.Append(text[current..next.Value].ToLower());
				sb.Append(delimiter);
				continue;
			}
			// ex. SomeAPIEndpoint, SomeAPI
			//         ^^^              ^^
			if (current + 1 == next)
			{
				int veryEnd = current;
				while (true)
				{
					i++;
					if (i >= indexes.Count) break;
					if (indexes[i] != veryEnd + 1) break;
					veryEnd = indexes[i];
				}
				i -= 2;
				if (veryEnd == text.Length - 1)
				{
					sb.Append(text[current..].ToLower());
					break;
				}

				sb.Append(text[current..veryEnd].ToLower());
				sb.Append(delimiter);
				continue;
			}
		}

		return sb.ToString();

		int FindNextUpperCaseIndex(int start)
		{
			if (start >= text.Length) return -1;
			for (int i = start; i < text.Length; i++)
			{
				if (char.IsUpper(text[i])) return i;
			}
			return -1;
		}
	}
	public static string ToPascalCase(this string text)
	{
		if (text.Length == 0) return text;

		char[] chars = text.ToCharArray();
		chars[0] = char.ToUpper(chars[0]);
		return new(chars);
	}
}
