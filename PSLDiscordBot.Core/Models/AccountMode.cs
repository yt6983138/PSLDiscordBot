namespace PSLDiscordBot.Core.Models;

/// <summary>
/// Persisted account state. Bit 0 (<see cref="International"/>) is the cloud region and is retained in local mode;
/// bit 1 (<see cref="Local"/>) selects local mode. Cloud China is 0b00 (the default), Cloud International is 0b01.
/// </summary>
[Flags]
public enum AccountMode
{
	China = 0b00,
	[Obsolete(UserData.InternationalObsoleteMessage)]
	International = 0b01,
	Local = 0b10,
}
