using PSLDiscordBot.Core.ImageGenerating.TMPTag.Elements.BuiltIn;
using System.Reflection;
using System.Text.RegularExpressions;

namespace PSLDiscordBot.Core.ImageGenerating.TMPTag.Elements;

public static partial class TMPTagElementHelper
{
	private static IReadOnlyDictionary<string, Type> ParsableTags { get; } = typeof(ITMPTagElementParsable)
		.Assembly
		.GetTypes()
		.Where(t => t.IsAssignableTo(typeof(ITMPTagElementParsable)))
		.Where(t => t.IsAssignableTo(typeof(ITMPTagElement)))
		.Where(t => !t.IsInterface)
		.Where(t => !t.IsAbstract)
		.ToDictionary(t => t.GetProperty(
			nameof(ITMPTagElementParsable.HtmlTagName),
			BindingFlags.Public
			| BindingFlags.GetProperty
			| BindingFlags.Static)!
				.GetValue(null)!.ToString()!,
			x => x);

	[GeneratedRegex("<([^<>/]+)(?:=([^<>/]+))?>(.*?(?:(<\\1.*>.*?</\\1>)*?)[^<>/]*?)</\\1>")]
	private static partial Regex TagMatcher();

	public static List<ITMPTagElement> ParseOneLevel(string source)
	{
		MatchCollection tags = TagMatcher()
			.Matches(source);

		if (tags.Count == 0)
		{
			return [new TMPString() { InnerHtml = source }];
		}

		List<ITMPTagElement> childs = new();
		for (int i = 0; i < tags.Count; i++)
		{
			Match match = tags[i];

			Group tagName = match.Groups[1];
			Group tagAttribute = match.Groups[2];
			Group innerText = match.Groups[3];

			if (!ParsableTags.TryGetValue(tagName.Value, out Type? type))
			{
				childs.Add(new TMPString() { InnerHtml = match.Value });
				continue;
			}

			if (i == 0)
			{
				if (match.Index != 0)
				{
					childs.Add(new TMPString() { InnerHtml = source[..match.Index] });
				}
			}
			if (i == tags.Count - 1)
			{
				if ((match.Index + match.Length) != source.Length - 1)
				{
					childs.Add(new TMPString() { InnerHtml = source[(match.Index + match.Length + 1)..] });
				}
			}

			ITMPTagElement instance = (ITMPTagElement)Activator.CreateInstance(type)!;
			instance.InnerHtml = match.Value;
			instance.TagAttributeValue = string.IsNullOrWhiteSpace(tagAttribute.Value) ? null : tagAttribute.Value;

			instance.ChildElements = ParseOneLevel(innerText.Value);

			childs.Add(instance);
		}

		return childs;
	}
}
