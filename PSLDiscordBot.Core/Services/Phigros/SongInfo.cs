namespace PSLDiscordBot.Core.Services.Phigros;
public class SongInfo(
	string name,
	string artist,
	string illustrator,
	string charterEZ,
	string charterHD,
	string charterIN,
	string charterAT)
{
	public string Name { get; set; } = name;
	public string Artist { get; set; } = artist;
	public string Illustrator { get; set; } = illustrator;
	public string CharterEZ { get; set; } = charterEZ;
	public string CharterHD { get; set; } = charterHD;
	public string CharterIN { get; set; } = charterIN;
	public string CharterAT { get; set; } = charterAT;
}
