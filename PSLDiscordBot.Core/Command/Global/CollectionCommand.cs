using PhiInfo.Core.Models.Information;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public partial class CollectionCommand : CommandBase
{
	private record class CollectionSearchInfo(FileItem Item, Folder CorrespondingChapter, double Similarity) : IComparable<CollectionSearchInfo>
	{
		public int CompareTo(CollectionSearchInfo? other)
		{
			return other is null ? 1 : this.Similarity.CompareTo(other.Similarity);
		}
	}

	public CollectionCommand(IServiceProvider provider) : base(provider)
	{
	}

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.CollectionName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.CollectionDescription];

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder;

	protected override IReadOnlyList<CommandNode> CreateChildren() =>
	[
		new ListGroup(this),
		new ReadGroup(this),
	];

	private ApplicationCommandOptionChoiceProperties[] GetChapterChoices()
	{
		return this._phigrosService.DefaultMultiLanguageInfo.Collections?
			.Select(x => BuilderUtility.CreateChoice(this.CreateChapterChoiceLocalization(x.AddressableCoverPath), x.AddressableCoverPath))
			.ToArray() ?? [];
	}
	// there is no id for chapter even inside the game, so addressable path is the only way to identify it (they seem to be same over different languages)
	private LocalizedString CreateChapterChoiceLocalization(string addressablePath)
	{
		Dictionary<Language, string> result = [];
		foreach (KeyValuePair<Language, PhiInfo.CLI.MultiLanguageInfos> item in this._phigrosService.MultiLanguageInfos)
		{
			List<Folder>? collection = item.Value.Collections;
			if (collection is null) continue;

			Folder? instance = collection.FirstOrDefault(x => x.AddressableCoverPath == addressablePath);
			if (instance is null) continue;

			result[item.Key] = FormatTitleAndSubTitle(instance.Title, instance.Subtitle);
		}

		return LocalizedString.Create(result);
	}
	private static string FormatTitleAndSubTitle(string title, string? subtitle)
	{
		if (string.IsNullOrWhiteSpace(subtitle))
			return title;
		return $"{title} - {subtitle}";
	}

	private SlashCommandOptionBuilder AddLanguageOption(SlashCommandOptionBuilder builder)
	{
		return builder.AddOption(
			this._localization[PSLNormalCommandKey.CollectionOptionLanguageName],
			ApplicationCommandOptionType.Integer,
			this._localization[PSLNormalCommandKey.CollectionOptionLanguageDescription],
			isRequired: false,
			choices: this._phigrosService.MultiLanguageInfos.Select(x => BuilderUtility.CreateChoice(x.Key.ToString(), (int)x.Key)).ToArray());
	}
	private Language GetLanguageOption(SocketSlashCommandDataOption option, SocketSlashCommand arg)
	{
		Language desiredLang = option.GetOptionOrDefault(this._localization[PSLNormalCommandKey.CollectionOptionLanguageName], LocalizationHelper.FromCode(arg.UserLocale));
		if (this._phigrosService.MultiLanguageInfos.ContainsKey(desiredLang))
			return desiredLang;

		if (this._phigrosService.MultiLanguageInfos.ContainsKey(Language.EnglishUS))
			return Language.EnglishUS;

		return this._phigrosService.MultiLanguageInfos.First().Key;
	}
}
