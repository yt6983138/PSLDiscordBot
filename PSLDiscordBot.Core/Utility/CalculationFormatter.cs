using SmartFormat.Core.Extensions;
using System.Numerics;
using System.Reflection;
using yt6983138.Common;

namespace PSLDiscordBot.Core.Utility;
public class CalculationFormatter : IFormatter
{
	private static MethodInfo _processMethod = typeof(CalculationFormatter)
		.GetMethod(nameof(Process), BindingFlags.Static | BindingFlags.NonPublic)
		.EnsureNotNull();

	public string Name { get; set; } = "calc";
	public bool CanAutoDetect { get; set; } = false;

	public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
	{
		object? value = formattingInfo.CurrentValue;

		if (value is null) return false;
		Type type = value.GetType();
		if (value is not null && type.IsAssignableTo(typeof(INumber<>).MakeGenericType(value.GetType())))
		{
			return (bool)_processMethod
				.MakeGenericMethod(type)
				.Invoke(null, [value, formattingInfo])!;
		}
		return false; // Added return statement to ensure method always returns a value
	}

	private static bool Process<T>(T value, IFormattingInfo info) where T : INumber<T>
	{
		string option = info.FormatterOptions;
		char @operator = option[0];
		string format = info.Format?.GetLiteralText() ?? "";
		if (!T.TryParse(option.AsSpan()[1..], null, out T? operand) || operand is null)
		{
			return false;
		}

		if (string.IsNullOrWhiteSpace(option))
		{
			return false;
		}

		if (@operator == '+')
		{
			T result = value + operand;
			info.Write(result.ToString(format, null));
			return true;
		}
		else if (@operator == '-')
		{
			T result = value - operand;
			info.Write(result.ToString(format, null));
			return true;
		}
		else if (@operator == '*')
		{
			T result = value * operand;
			info.Write(result.ToString(format, null));
			return true;
		}
		else if (@operator == '/')
		{
			T result = value / operand;
			info.Write(result.ToString(format, null));
			return true;
		}
		else if (@operator == '%')
		{
			T result = value % operand;
			info.Write(result.ToString(format, null));
			return true;
		}

		return false;
	}
}
