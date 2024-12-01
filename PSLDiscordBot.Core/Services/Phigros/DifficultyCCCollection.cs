namespace PSLDiscordBot.Core.Services.Phigros;

public struct DifficultyCCCollection
{
	public float EZ { get; set; }
	public float HD { get; set; }
	public float IN { get; set; }
	public float AT { get; set; }

	public float this[int index]
	{
		readonly get => index switch
		{
			0 => this.EZ,
			1 => this.HD,
			2 => this.IN,
			3 => this.AT,
			_ => throw new IndexOutOfRangeException()
		};
		set
		{
			switch (index)
			{
				case 0: this.EZ = value; break;
				case 1: this.HD = value; break;
				case 2: this.IN = value; break;
				case 3: this.AT = value; break;
				default: throw new IndexOutOfRangeException();
			}
		}
	}

	public readonly float[] ToFloats()
	{
		return [this.EZ, this.HD, this.IN, this.AT];
	}
}