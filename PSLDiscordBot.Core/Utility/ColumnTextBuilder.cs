﻿using SmartFormat;
using System.Text;

namespace PSLDiscordBot.Core.Utility;
public class ColumnTextBuilder
{
	public struct RowBuilder
	{
		public List<string> Columns { get; set; } = [];

		public RowBuilder() { }
		public RowBuilder(params IEnumerable<string> columns)
			=> this.Columns.AddRange(columns);

		public readonly string this[int index]
		{
			get => this.Columns[index];
			set => this.Columns[index] = value;
		}
		public readonly RowBuilder WithStringAdded(string str)
		{
			this.Columns.Add(str);
			return this;
		}
		public readonly RowBuilder WithStringAdded(string language, LocalizedString str)
			=> this.WithStringAdded(str[language]);
		public readonly RowBuilder WithObjectAdded(object obj)
			=> this.WithStringAdded(obj.ToString()!);
		public readonly RowBuilder WithFormatAdded(string format, params object?[] args)
			=> this.WithStringAdded(Smart.Format(format, args));
		public readonly RowBuilder WithFormatAdded(string language, LocalizedString format, params object?[] args)
			=> this.WithStringAdded(format.GetFormatted(language, args));
		public readonly RowBuilder WithStringInsertedAt(int index, string str)
		{
			this.Columns.Insert(index, str);
			return this;
		}
		public readonly RowBuilder WithStringInsertedAt(int index, string language, LocalizedString str)
			=> this.WithStringInsertedAt(index, str[language]);
		public readonly RowBuilder WithStringRemovedAt(int index)
		{
			this.Columns.RemoveAt(index);
			return this;
		}
	}

	public static readonly IReadOnlyList<(int Minium, int Maxium)> FullWidthCharRanges = [
	(4352, 4447),
		(8986, 8987),
		(9001, 9002),
		(9193, 9196),
		(9200, 9200),
		(9203, 9203),
		(9725, 9726),
		(9748, 9749),
		(9776, 9783),
		(9800, 9811),
		(9855, 9855),
		(9866, 9871),
		(9875, 9875),
		(9889, 9889),
		(9898, 9899),
		(9917, 9918),
		(9924, 9925),
		(9934, 9934),
		(9940, 9940),
		(9962, 9962),
		(9970, 9971),
		(9973, 9973),
		(9978, 9978),
		(9981, 9981),
		(9989, 9989),
		(9994, 9995),
		(10024, 10024),
		(10060, 10060),
		(10062, 10062),
		(10067, 10069),
		(10071, 10071),
		(10133, 10135),
		(10160, 10160),
		(10175, 10175),
		(11035, 11036),
		(11088, 11088),
		(11093, 11093),
		(11904, 11929),
		(11931, 12019),
		(12032, 12245),
		(12272, 12350),
		(12353, 12438),
		(12441, 12543),
		(12549, 12591),
		(12593, 12686),
		(12688, 12773),
		(12783, 12830),
		(12832, 12871),
		(12880, 42124),
		(42128, 42182),
		(43360, 43388),
		(44032, 55203),
		(63744, 64255),
		(65040, 65049),
		(65072, 65106),
		(65108, 65126),
		(65128, 65131),
		(65281, 65376),
		(65504, 65510),
		(94176, 94180),
		(94192, 94193),
		(94208, 100343),
		(100352, 101589),
		(101631, 101640),
		(110576, 110579),
		(110581, 110587),
		(110589, 110590),
		(110592, 110882),
		(110898, 110898),
		(110928, 110930),
		(110933, 110933),
		(110948, 110951),
		(110960, 111355),
		(119552, 119638),
		(119648, 119670),
		(126980, 126980),
		(127183, 127183),
		(127374, 127374),
		(127377, 127386),
		(127488, 127490),
		(127504, 127547),
		(127552, 127560),
		(127568, 127569),
		(127584, 127589),
		(127744, 127776),
		(127789, 127797),
		(127799, 127868),
		(127870, 127891),
		(127904, 127946),
		(127951, 127955),
		(127968, 127984),
		(127988, 127988),
		(127992, 128062),
		(128064, 128064),
		(128066, 128252),
		(128255, 128317),
		(128331, 128334),
		(128336, 128359),
		(128378, 128378),
		(128405, 128406),
		(128420, 128420),
		(128507, 128591),
		(128640, 128709),
		(128716, 128716),
		(128720, 128722),
		(128725, 128727),
		(128732, 128735),
		(128747, 128748),
		(128756, 128764),
		(128992, 129003),
		(129008, 129008),
		(129292, 129338),
		(129340, 129349),
		(129351, 129535),
		(129648, 129660),
		(129664, 129673),
		(129679, 129734),
		(129742, 129756),
		(129759, 129769),
		(129776, 129784),
		(131072, 196605),
		(196608, 262141)
];

	private List<string> _columnTitles = [];
	private List<string[]> _rows = [];

	public string Delimiter { get; set; } = " | ";
	public string ColumnEnd { get; set; } = "\n";

	public ColumnTextBuilder(params IEnumerable<string> columnTitles)
	{
		this.WithColumnTitles(columnTitles);
	}
	public ColumnTextBuilder(IDiscordInteraction interaction, params IEnumerable<LocalizedString> columnTitles)
		: this(columnTitles.Select(x => x[interaction.UserLocale])) { }
	public ColumnTextBuilder() { }

	public ColumnTextBuilder WithColumnTitles(params IEnumerable<string> columnTitles)
	{
		if (this._columnTitles.Count != 0) throw new InvalidOperationException("The column titles has already been set.");
		this._columnTitles.AddRange(columnTitles);

		return this;
	}
	public ColumnTextBuilder WithColumnTitles(string language, params IEnumerable<LocalizedString> columnTitles)
		=> this.WithColumnTitles(columnTitles.Select(x => x[language]));

	public ColumnTextBuilder WithRow(params string[] columns)
	{
		if (columns.Length > this._columnTitles.Count)
			throw new ArgumentException("Too many columns!", nameof(columns));
		this._rows.Add(columns);

		return this;
	}
	public ColumnTextBuilder WithRow(string language, params IEnumerable<LocalizedString> columns)
		=> this.WithRow(columns.Select(x => x[language]).ToArray());
	public ColumnTextBuilder WithRow(RowBuilder row)
		=> this.WithRow(row.Columns.ToArray());

	public ColumnTextBuilder WithRowInsertedAt(int index, params string[] columns)
	{
		if (columns.Length > this._columnTitles.Count)
			throw new ArgumentException("Too many columns!", nameof(columns));
		this._rows.Insert(index, columns);

		return this;
	}
	public ColumnTextBuilder WithRowInsertedAt(int index, string language, params IEnumerable<LocalizedString> columns)
		=> this.WithRowInsertedAt(index, columns.Select(x => x[language]).ToArray());
	public ColumnTextBuilder WithRowInsertedAt(int index, RowBuilder row)
		=> this.WithRowInsertedAt(index, row.Columns.ToArray());

	public ColumnTextBuilder WithRowRemovedAt(int index)
	{
		this._rows.RemoveAt(index);
		return this;
	}

	public StringBuilder Build() => this.Build(new());
	public StringBuilder Build(StringBuilder builder)
	{
		List<int> columnMaxLengths = this._columnTitles.Select(MonoSpaceColumnNeeded).ToList();
		foreach (string[] item in this._rows)
		{
			for (int i = 0; i < item.Length; i++)
			{
				columnMaxLengths[i] = Math.Max(MonoSpaceColumnNeeded(item[i]), columnMaxLengths[i]);
			}
		}

		for (int i = 0; i < this._columnTitles.Count; i++)
		{
			string str = this._columnTitles[i];
			builder.Append(str);
			builder.Append(' ', columnMaxLengths[i] - MonoSpaceColumnNeeded(str));
			if (i != this._columnTitles.Count - 1)
				builder.Append(this.Delimiter);
		}
		builder.Append(this.ColumnEnd);
		foreach (string[] item in this._rows)
		{
			for (int i = 0; i < item.Length; i++)
			{
				string str = item[i];
				builder.Append(str);
				builder.Append(' ', columnMaxLengths[i] - MonoSpaceColumnNeeded(str));
				if (i != item.Length - 1)
					builder.Append(this.Delimiter);
			}
			builder.Append(this.ColumnEnd);
		}

		return builder;
	}

	public override string ToString()
	{
		return this.Build().ToString();
	}

	public static int MonoSpaceColumnNeeded(string str) => str.Sum(x => IsFullWidth(x) ? 2 : 1);
	public static bool IsFullWidth(char first, char second = default)
	{
		int combined = first | (second << 16);
		foreach ((int Minium, int Maxium) in FullWidthCharRanges)
		{
			if (Minium <= combined && combined <= Maxium) return true;
		}
		return false;
	}
}
