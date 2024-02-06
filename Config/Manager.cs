using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using yt6983138.Common;
using Discord.Net;
using Discord.WebSocket;
using System.Runtime.Loader;

namespace PSLDiscordBot;
public static class Manager
{
	public const string ConfigLocation = "./Config.json";
	public static bool FirstStart { get; private set; }
	public static Config Config { get; set; }
	public static Logger Logger { get; set; }
	public static DiscordSocketClient SocketClient { get; set; } = new();
	public static Dictionary<ulong, UserData> RegisteredUsers { get; set; }
	public static IReadOnlyDictionary<string, float[]> Difficulties { get; set; }
	public static IReadOnlyDictionary<string, string> Names { get; set; }
	static Manager()
	{
		FileInfo file = new(ConfigLocation);
		if (!file.Directory!.Exists)
			file.Directory!.Create();
		try
		{
			Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigLocation))!;
			FirstStart = false;
		}
		catch
		{
			Config = new();
			FirstStart = true;
		}
		FileInfo user = new(Config.UserDataLocation);
		if (!file.Directory!.Exists)
			file.Directory!.Create();
		try
		{
			RegisteredUsers = JsonConvert.DeserializeObject<Dictionary<ulong, UserData>>(File.ReadAllText(Config.UserDataLocation))!;
		}
		catch
		{
			RegisteredUsers = new();
		}

		Logger = new(Config.LogLocation, Config.Verbose);
		AssemblyLoadContext.Default.Unloading += _ => { WriteEverything(); Console.WriteLine("Shutting down..."); };
		
		if (FirstStart)
		{
			using (HttpClient client = new())
			{
				Task<byte[]> diff = client.GetByteArrayAsync(@"https://yt6983138.github.io/Assets/RksReader/3.4.3/difficulty.csv");
				Task<byte[]> name = client.GetByteArrayAsync(@"https://yt6983138.github.io/Assets/RksReader/3.4.3/info.csv");
				diff.Wait(); // ^ no async constructor :(
				name.Wait();
				File.WriteAllBytes(Config.DifficultyCsvLocation, diff.Result);
				File.WriteAllBytes(Config.NameCsvLocation, name.Result);
			}
		}
		ReadCsvs();
		WriteEverything();
	}
	public static void WriteEverything()
	{
		File.WriteAllText(ConfigLocation, JsonConvert.SerializeObject(Config));
		File.WriteAllText(Config.UserDataLocation, JsonConvert.SerializeObject(RegisteredUsers));
	}
	private static void ReadCsvs()
	{
		string[] csvFile = File.ReadAllLines(Config.DifficultyCsvLocation);
		Dictionary<string, float[]> diffculties = new();
		foreach (string line in csvFile)
		{
			try
			{
				float[] diffcultys = new float[4];
				string[] splitted = line.Split(",");
				for (byte i = 0; i < splitted.Length; i++)
				{
					if (i > 4 || i == 0) { continue; }
					if (!float.TryParse(splitted[i], out diffcultys[i - 1])) { Console.WriteLine($"Error processing {splitted[i]}"); }
				}
				// Console.WriteLine($"{splitted[0]}, {diffcultys[0]}, {diffcultys[1]}, {diffcultys[2]}, {diffcultys[3]}");
				diffculties.Add(splitted[0], diffcultys);
			}
			catch (Exception ex)
			{
				Logger.Log(LoggerType.Error, ex);
			}
		}
		Difficulties = diffculties;

		string[] csvFile2 = File.ReadAllLines(Config.NameCsvLocation);
		Dictionary<string, string> names = new();
		foreach (string line in csvFile2)
		{
			try
			{
				string[] splitted = line.Split(@"\");
				names.Add(splitted[0], splitted[1]);
			}
			catch (Exception ex)
			{
				Logger.Log(LoggerType.Error, ex);
			}
		}
		Names = names;
	}
}
