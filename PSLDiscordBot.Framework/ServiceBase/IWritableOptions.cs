using Microsoft.Extensions.Options;

namespace PSLDiscordBot.Framework.ServiceBase;

public interface IWritableOptions<T> : IOptions<T> where T : class, new()
{
	void Update(Func<T, T> applyChanges);
}
