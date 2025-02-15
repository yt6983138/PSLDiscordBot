using PSLDiscordBot.Core.UserDatas;
using SmartFormat.Core.Extensions;
using SmartFormat.Core.Parsing;

namespace PSLDiscordBot.Core.Utility;
public class UserFormatFormatter : IFormatter // TODO: replace this in some score formatters
{
	public string Name { get; set; } = "user";
	public bool CanAutoDetect { get; set; }

	public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
	{
		Format? format = formattingInfo.Format;
		UserData? userData = formattingInfo.FormatDetails.OriginalArgs
			.OfType<UserData>()
			.FirstOrDefault();

		if (format is null
			|| formattingInfo.CurrentValue is not IFormattable current
			|| userData is null)
		{
			return false;
		}

		formattingInfo.Write(current.ToString(userData.ShowFormat, null));

		return true;
	}
}
