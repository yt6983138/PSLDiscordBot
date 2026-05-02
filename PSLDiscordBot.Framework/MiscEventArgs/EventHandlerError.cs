namespace PSLDiscordBot.Framework.MiscEventArgs;

public record class EventHandlerError(Type HandlerEventArgType, Exception Exception);
