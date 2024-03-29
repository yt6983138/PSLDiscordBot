﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using yt6983138.Common;
using Discord.Net;
using Discord.WebSocket;
using System.Runtime.Loader;
using ICSharpCode.SharpZipLib.Zip;

namespace PSLDiscordBot;
public static class Manager
{
	private static volatile Dictionary<ulong, UserData> _registeredUsers;

	public const string ConfigLocation = "./Config.json";
	public static bool FirstStart { get; private set; }
	public static Config Config { get; set; }
	public static Logger Logger { get; set; }
	public static DiscordSocketClient SocketClient { get; set; } = new();
	public static Dictionary<ulong, UserData> RegisteredUsers
	{
		get => _registeredUsers;
		set => _registeredUsers = value;
	}
	public static IReadOnlyDictionary<string, float[]> Difficulties { get; set; }
	public static IReadOnlyDictionary<string, string> Names { get; set; }
	static Manager()
	{
		FileInfo file = new(ConfigLocation);
		if (!file.Directory!.Exists)
			file.Directory!.Create();
		try
		{
#if DEBUG
			throw null!;
#endif
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
		AppDomain.CurrentDomain.ProcessExit += (_, _2) => { WriteEverything(); Console.WriteLine("Shutting down..."); };
#if DEBUG
		if (true)
#else
		if (FirstStart)
#endif
		{
			using (HttpClient client = new())
			{
				Task<byte[]> diff = client.GetByteArrayAsync(@"https://yt6983138.github.io/Assets/RksReader/Latest/difficulty.csv");
				Task<byte[]> name = client.GetByteArrayAsync(@"https://yt6983138.github.io/Assets/RksReader/Latest/info.csv");
				Task<byte[]> help = client.GetByteArrayAsync(@"https://raw.githubusercontent.com/yt6983138/PSLDiscordBot/master/help.md");
				Task<byte[]> zip = client.GetByteArrayAsync(@"https://github.com/yt6983138/PSLDiscordBot/raw/master/Assets.zip");
				diff.Wait(); // ^ no async constructor :(
				name.Wait();
				help.Wait();
				zip.Wait();
				File.WriteAllBytes(Config.DifficultyCsvLocation, diff.Result);
				File.WriteAllBytes(Config.NameCsvLocation, name.Result);
				File.WriteAllBytes(Config.HelpMDLocation, help.Result);
#if DEBUG
				var asset = new DirectoryInfo("./Assets");
				Logger.Log(LoggerType.Verbose, "copying");
				asset.Create();

				new DirectoryInfo(Secret.AssetsFolder).CopyFilesRecursively(asset);
#else
				File.WriteAllBytes("./Assets.zip", zip.Result);
				var fastZip = new FastZip();
				fastZip.ExtractZip("./Assets.zip", ".", "");
#endif
			}
		}
		ReadCsvs();
		Task.Run(AutoSave);
	}
	public static void WriteEverything()
	{
		File.WriteAllText(ConfigLocation, JsonConvert.SerializeObject(Config));
		File.WriteAllText(Config.UserDataLocation, JsonConvert.SerializeObject(RegisteredUsers));
	}
	private async static void AutoSave()
	{
		while (true)
		{
			lock (RegisteredUsers)
			{
				WriteEverything();
			}
			Logger.Log(LoggerType.Verbose, "Auto saved.");
			await Task.Delay(Config.AutoSaveInterval);
		}
	}
	public static void ReadCsvs()
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
