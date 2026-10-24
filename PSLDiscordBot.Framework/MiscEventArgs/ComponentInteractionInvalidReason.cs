namespace PSLDiscordBot.Framework.MiscEventArgs;

public enum ComponentInteractionInvalidReason
{
	None,
	MalformedId,
	UsedNonce,
	ExpiredProcess,
	CommandNotFound,
	CommandNotSupported
}
