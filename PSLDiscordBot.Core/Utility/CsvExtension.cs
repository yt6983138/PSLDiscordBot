using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace PSLDiscordBot.Core.Utility;
public static class CsvExtension
{
	extension(CsvWriter self)
	{
		public static CsvWriter NewEmpty(CsvConfiguration? config = null)
		{
			StringBuilder sb = new();
			StringWriter sw = new(sb);
			return new(sw, config ?? new(CultureInfo.InvariantCulture));
		}
		public StringBuilder? TryGetUnderlyingStringBuilder()
		{
			self.Flush();
			if (GetWriter(self) is StringWriter sw)
			{
				return sw.GetStringBuilder();
			}
			else
			{
				return null;
			}

			// i shouldnt be doing this
			[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "writer")]
			extern static ref TextWriter GetWriter(CsvWriter self);
		}
		public StringBuilder GetUnderlyingStringBuilder()
		{
			return self.TryGetUnderlyingStringBuilder() ??
				throw new InvalidOperationException($"The underlying writer is not a {nameof(StringWriter)}, cannot get {nameof(StringBuilder)}.");
		}
		/// <summary>
		/// remember to call NextRecord()
		/// </summary>
		/// <param name="fields"></param>
		public void WriteFields(params string[] fields)
		{
			foreach (string field in fields)
			{
				self.WriteField(field);
			}
		}
	}
}
