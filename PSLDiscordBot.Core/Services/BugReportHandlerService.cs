namespace PSLDiscordBot.Core.Services;

public delegate Task BugReportReceivedHandler(SocketUser user, string reportContent, IAttachment[] attachments);
public class BugReportHandlerService
{
	private List<BugReportReceivedHandler> _handlers = [];

	/// <summary>
	/// Warning: please do not modify arguments in the handlers
	/// </summary>
	public event BugReportReceivedHandler? OnReportReceived
	{
		add
		{
			if (value is not null)
				this._handlers.Add(value);
		}
		remove
		{
			if (value is not null)
				this._handlers.Remove(value);
		}
	}

	public BugReportHandlerService()
	{

	}

	public Task HandleReportAsync(SocketUser user, string reportContent, IAttachment[] attachments)
	{
		return Task.WhenAll(this._handlers.Select(x => x.Invoke(user, reportContent, attachments)));
	}
}
