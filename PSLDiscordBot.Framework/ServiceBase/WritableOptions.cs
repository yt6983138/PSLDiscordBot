using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PSLDiscordBot.Framework.ServiceBase;

public class WritableOptions<T> : IWritableOptions<T> where T : class, new()
{
	private readonly IHostEnvironment _environment;
	private readonly IOptionsMonitor<T> _options;
	private readonly IConfigurationRoot _configuration;
	private readonly string _section;
	private readonly string _file;

	public WritableOptions(
		IHostEnvironment environment,
		IOptionsMonitor<T> options,
		IConfigurationRoot configuration,
		string section,
		string file)
	{
		this._environment = environment;
		this._options = options;
		this._configuration = configuration;
		this._section = section;
		this._file = file;
	}

	public T Value => this._options.CurrentValue;
	public T Get(string name) => this._options.Get(name);

	public void Update(Func<T, T> applyChanges)
	{
		Microsoft.Extensions.FileProviders.IFileProvider fileProvider = this._environment.ContentRootFileProvider;
		Microsoft.Extensions.FileProviders.IFileInfo fileInfo = fileProvider.GetFileInfo(this._file);
		string? physicalPath = fileInfo.PhysicalPath;

		JObject jObject = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(physicalPath!))!;
		T? sectionObject = jObject.TryGetValue(this._section, out JToken? section) ?
			JsonConvert.DeserializeObject<T>(section.ToString()) : (this.Value ?? new T());

		T result = applyChanges.Invoke(sectionObject!);

		jObject[this._section] = JObject.Parse(JsonConvert.SerializeObject(result));
		File.WriteAllText(physicalPath!, JsonConvert.SerializeObject(jObject, Formatting.Indented));
		this._configuration.Reload();
	}
}