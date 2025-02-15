namespace PSLDiscordBot.Core.Utility;
public struct OneOf<T1, T2>
{
	public T1 Value1
	{
		readonly get => this.IsValue2 ? throw new InvalidOperationException() : field;
		set
		{
			field = value;
			this.IsValue2 = false;
		}
	}
	public T2 Value2
	{
		readonly get => this.IsValue2 ? field : throw new InvalidOperationException();
		set
		{
			field = value;
			this.IsValue2 = true;
		}
	}
	public bool IsValue2 { get; private set; } // if no value is set, it's value1

	public OneOf(T1 value1)
	{
		this.Value1 = value1;
		this.Value2 = default!;
		this.IsValue2 = false;
	}
	public OneOf(T2 value2)
	{
		this.Value1 = default!;
		this.Value2 = value2;
		this.IsValue2 = true;
	}

	public readonly T Match<T>(Func<T1, T> value1Func, Func<T2, T> value2Func)
		=> this.IsValue2 ? value2Func.Invoke(this.Value2) : value1Func.Invoke(this.Value1);
	public readonly void Match(Action<T1> value1Action, Action<T2> value2Action)
	{
		if (this.IsValue2)
			value2Action.Invoke(this.Value2);
		else
			value1Action.Invoke(this.Value1);
	}

	public static implicit operator OneOf<T1, T2>(T1 value)
		=> new(value);
	public static implicit operator OneOf<T1, T2>(T2 value)
		=> new(value);
}
