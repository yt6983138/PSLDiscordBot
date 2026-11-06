namespace PSLDiscordBot.Core.Command.Constraints;
public interface IConstraint
{
	/// <summary>
	/// Checks whether the command may continue. A failing constraint sends its own reply and returns <see langword="false"/>.
	/// </summary>
	/// <param name="arg">The deferred interaction.</param>
	/// <param name="data">The user's data, null if the user is not registered.</param>
	Task<bool> Check(SocketSlashCommand arg, UserData? data);
}
