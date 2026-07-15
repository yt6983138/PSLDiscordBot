global using Discord;
global using Discord.WebSocket;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;
global using PhigrosLibraryCSharp;
global using PhigrosLibraryCSharp.CloudSave;
global using PhigrosLibraryCSharp.CloudSave.HttpModels;
global using PhigrosLibraryCSharp.CloudSave.Login;
global using PSLDiscordBot.Core.Command.Global.Base;
global using PSLDiscordBot.Core.Localization;
global using PSLDiscordBot.Core.Models;
global using PSLDiscordBot.Core.Services;
global using PSLDiscordBot.Core.Utility;
global using PSLDiscordBot.Framework;
global using PSLDiscordBot.Framework.CommandBase;
global using PSLDiscordBot.Framework.Localization;
global using PSLDiscordBot.Framework.Utilities;

// they put a bunch of other async stuff in the library, and importing them create a bunch of conflicts with new bcl
global using AsyncReaderWriterLock = Nito.AsyncEx.AsyncReaderWriterLock;
