namespace PSLDiscordBot.Core.ImageGenerating.TMPTag.Elements.BuiltIn;
public class TMPString : ITMPTagElement, ITMPTagElementParsable
{
	public static string HtmlTagName => "__string";

	public string TagName { get => ""; }
	public object? TagAttributeValue { get => null; set { } }
	public List<ITMPTagElement> ChildElements { get => new(); set { } }
	public string InnerHtml { get; set; } = "";

	public string ToHtml()
	{
		return this.InnerHtml;
	}
	public string ToTextOnly()
	{
		return this.InnerHtml;
	}
}
