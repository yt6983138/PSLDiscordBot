namespace PSLDiscordBot.Framework.MiscEventArgs;

public enum ComponentInteractionInvalidReason
{
	None,
	MalformedId,
	UnsupportedVersion,
	ExpiredProcess,
	CommandNotFound,
	CommandNotSupported
}
