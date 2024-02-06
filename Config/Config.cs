using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSLDiscordBot;
public class Config
{
	public bool Verbose { get; set; } = true;
	public string Token { get; set; } = "";
	public string LogLocation { get; set; } = "./Latest.log";
	public string UserDataLocation { get; set; } = "./UserData.json";
	public string DifficultyCsvLocation { get; set; } = "./difficulty.csv";
	public string NameCsvLocation { get; set; } = "./info.csv";
}
