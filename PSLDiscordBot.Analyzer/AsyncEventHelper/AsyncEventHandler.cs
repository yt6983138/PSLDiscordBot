using System.Threading.Tasks;

namespace PSLDiscordBot.Analyzer.AsyncEventHelper;

public delegate Task AsyncEventHandler<TEventArgs>(object? sender, TEventArgs e);
public delegate Task<TResult> AsyncEventHandler<TEventArgs, TResult>(object? sender, TEventArgs e);
