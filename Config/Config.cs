using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSLDiscordBot;
public class Config
{
	public bool Verbose { get; set; } = true;
	public int AutoSaveInterval { get; set; } = 20 * 1000 * 60; // 20min
	public string Token { get; set; } = "";
	public string LogLocation { get; set; } = "./Latest.log";
	public string UserDataLocation { get; set; } = "./UserData.json";
	public string DifficultyCsvLocation { get; set; } = "./difficulty.csv";
	public string NameCsvLocation { get; set; } = "./info.csv";
	public string HelpMDLocation { get; set; } = "./help.md";
}
