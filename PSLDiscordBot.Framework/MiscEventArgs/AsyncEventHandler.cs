namespace PSLDiscordBot.Framework.MiscEventArgs;

public delegate Task AsyncEventHandler<TEventArgs>(object? sender, TEventArgs e);
