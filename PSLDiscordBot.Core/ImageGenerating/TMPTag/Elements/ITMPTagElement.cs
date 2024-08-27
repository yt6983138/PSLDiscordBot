namespace PSLDiscordBot.Core.ImageGenerating.TMPTag.Elements;
public interface ITMPTagElement
{
	public string TagName { get; }
	/// <summary>
	/// The objects implementing this is responsible to parse the tag value on set.
	/// </summary>
	public object? TagAttributeValue { get; set; }
	public List<ITMPTagElement> ChildElements { get; set; }

	public string InnerHtml { get; set; }

	public string ToHtml();
	public string ToTextOnly();
}
