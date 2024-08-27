using System.Text;

namespace PSLDiscordBot.Core.ImageGenerating.TMPTag.Elements.BuiltIn;
public class AllCaps : ITMPTagElement, ITMPTagElementParsable
{
	private const string RealTagName = "allcaps";

	public static string HtmlTagName => RealTagName;

	public string TagName => RealTagName;
	public object? TagAttributeValue { get; set; }
	public List<ITMPTagElement> ChildElements { get; set; } = new();
	public string InnerHtml { get; set; } = "";

	public string ToHtml()
	{
		StringBuilder sb = new();
		if (this.TagAttributeValue is null)
			sb.Append($"<{this.TagName}>");
		else sb.Append($"<{this.TagName}={this.TagAttributeValue}>");

		foreach (ITMPTagElement element in this.ChildElements)
		{
			sb.Append(element.ToHtml());
		}

		sb.Append($"</{this.TagName}>");
		return sb.ToString();
	}
	public string ToTextOnly()
	{
		StringBuilder sb = new();
		foreach (ITMPTagElement element in this.ChildElements)
		{
			sb.Append(element.ToTextOnly());
		}

		return sb.ToString();
	}
}
