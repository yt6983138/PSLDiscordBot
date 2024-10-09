using Newtonsoft.Json;
using PSLDiscordBot.Framework.DependencyInjection;
using System.Text;

namespace PSLDiscordBot.Framework.ServiceBase;
public abstract class FileManagementServiceBase<T> : InjectableBase
{
	private int _autoSaveInterval;
	private protected JsonSerializerSettings _defaultSettings = new()
	{
		ObjectCreationHandling = ObjectCreationHandling.Replace,
		Formatting = Formatting.Indented
	};

	/// <summary>
	/// set to 0 to disable
	/// </summary>
	public int AutoSaveIntervalMs
	{
		get => this._autoSaveInterval;
		set
		{
			this._autoSaveInterval = value;
			if (this.AutoSaveIntervalMs > 0)
				this.AutoSaveRunner = new(_ => this.Save(this.Data), this, 0, this.AutoSaveIntervalMs);
			else
			{
				this.AutoSaveRunner?.Dispose();
				this.AutoSaveRunner = null;
			}
		}
	}
	protected FileInfo InfoOfFile { get; set; }
	protected Timer? AutoSaveRunner { get; set; }
	public T Data { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	protected FileManagementServiceBase(string filename)
		: base()
	{
		this.LaterInitialize(filename);
	}
	/// <summary>
	/// Warning: only use this when you need to initialize everything else before this initializes. 
	/// Remember to use <see cref="LaterInitialize(string)"/> later
	/// </summary>
	protected FileManagementServiceBase()
		: base()
	{
	}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

	protected void LaterInitialize(string filename)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(filename)!);
		this.InfoOfFile = new FileInfo(filename);
		if (!this.InfoOfFile.Exists)
			this.InfoOfFile.Create().Dispose();

		if (this.Load(out T? data))
			this.Data = data;
		else
		{
			this.Data = this.Generate();
			this.Save(this.Data);
		}
	}

	/// <summary>
	/// True if loaded, false if failure
	/// </summary>
	/// <param name="data"></param>
	/// <returns></returns>
	protected abstract bool Load(out T data);
	protected abstract void Save(T data);
	public abstract T Generate();

	public virtual void Save()
		=> this.Save(this.Data);
	public bool TryLoadJsonAs<TFile>(FileInfo info, out TFile data, JsonSerializerSettings? settings = null)
	{
		settings ??= this._defaultSettings;
		FileStream? stream = null;
		try
		{
			stream = info.OpenRead();
			byte[] buffer = new byte[stream.Length];
			stream.Read(buffer);

			data = JsonConvert.DeserializeObject<TFile>(Encoding.UTF8.GetString(buffer), settings)!;
			if (data is null)
				return false;

			return true;
		}
		catch
		{
			data = default!;
			return false;
		}
		finally
		{
			stream?.Dispose();
		}
	}
	public void WriteJsonToFile<TFile>(FileInfo info, TFile data, JsonSerializerSettings? settings = null)
	{
		settings ??= this._defaultSettings;
		using FileStream stream = info.OpenWrite();
		stream.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data, settings)));
	}
}
