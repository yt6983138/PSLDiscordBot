using FuzzySharp;
using PhiInfo.Core.Models.Information;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class CollectionCommand : CommandBase
{
	private record class Context(SocketSlashCommand Command);
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

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder
		.AddSubCommandGroup(
			this._localization[PSLNormalCommandKey.CollectionGroupListName],
			this._localization[PSLNormalCommandKey.CollectionGroupListDescription],
			group => group.AddSubCommand(
					this._localization[PSLNormalCommandKey.CollectionSubCommandListChaptersName],
					this._localization[PSLNormalCommandKey.CollectionSubCommandListChaptersDescription],
					x => x.AddLanguageOption(this._phigrosService, this._localization))
				.AddSubCommand(
					this._localization[PSLNormalCommandKey.CollectionSubCommandListFilesName],
					this._localization[PSLNormalCommandKey.CollectionSubCommandListFilesDescription],
					x => x.AddOption(
							this._localization[PSLNormalCommandKey.CollectionOptionChapterName],
							ApplicationCommandOptionType.String,
							this._localization[PSLNormalCommandKey.CollectionOptionChapterListDescription],
							isRequired: true,
							choices: this.GetChapterChoices())
						.AddLanguageOption(this._phigrosService, this._localization))
				.AddSubCommand(
					this._localization[PSLNormalCommandKey.CollectionSubCommandListTipsName],
					this._localization[PSLNormalCommandKey.CollectionSubCommandListTipsDescription],
					x => x.AddLanguageOption(this._phigrosService, this._localization)))
		.AddSubCommandGroup(
			this._localization[PSLNormalCommandKey.CollectionGroupReadName],
			this._localization[PSLNormalCommandKey.CollectionGroupReadDescription],
			group => group.AddSubCommand(
				this._localization[PSLNormalCommandKey.CollectionSubCommandReadChapterName],
				this._localization[PSLNormalCommandKey.CollectionSubCommandReadChapterDescription],
				x => x.AddOption(
						this._localization[PSLNormalCommandKey.CollectionOptionChapterName],
						ApplicationCommandOptionType.String,
						this._localization[PSLNormalCommandKey.CollectionOptionChapterReadDescription],
						isRequired: true,
						choices: this.GetChapterChoices())
					.AddLanguageOption(this._phigrosService, this._localization))
				.AddSubCommand(
					this._localization[PSLNormalCommandKey.CollectionSubCommandReadItemName],
					this._localization[PSLNormalCommandKey.CollectionSubCommandReadItemDescription],
					x => x.AddOption(
							this._localization[PSLNormalCommandKey.CollectionOptionSearchName],
							ApplicationCommandOptionType.String,
							this._localization[PSLNormalCommandKey.CollectionOptionSearchDescription],
							isRequired: true)
						.AddLanguageOption(this._phigrosService, this._localization))
				.AddSubCommand(
					this._localization[PSLNormalCommandKey.CollectionSubCommandReadRandomTipName],
					this._localization[PSLNormalCommandKey.CollectionSubCommandReadRandomTipDescription],
					x => x.AddLanguageOption(this._phigrosService, this._localization)));

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

			if (string.IsNullOrWhiteSpace(instance.Subtitle))
				result[item.Key] = instance.Title;
			else
				result[item.Key] = $"{instance.Title} - {instance.Subtitle}";
		}

		return LocalizedString.Create(result);
	}

	public override Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		Context context = new(arg);
		return RouteSubCommandGroup(arg.Data, context,
			RouteToSubcommand(this._localization[PSLNormalCommandKey.CollectionGroupListName], context,
				new(this._localization[PSLNormalCommandKey.CollectionSubCommandListChaptersName], this.HandleListChapters),
				new(this._localization[PSLNormalCommandKey.CollectionSubCommandListFilesName], this.HandleListFiles),
				new(this._localization[PSLNormalCommandKey.CollectionSubCommandListTipsName], this.HandleListTips)),
			RouteToSubcommand(this._localization[PSLNormalCommandKey.CollectionGroupReadName], context,
				new(this._localization[PSLNormalCommandKey.CollectionSubCommandReadChapterName], this.HandleReadChapter),
				new(this._localization[PSLNormalCommandKey.CollectionSubCommandReadItemName], this.HandleReadItem),
				new(this._localization[PSLNormalCommandKey.CollectionSubCommandReadRandomTipName], this.HandleRandomTip)));
	}

	private async Task HandleListChapters(SocketSlashCommandDataOption option, Context context)
	{
		Language language = option.GetLanguageOption(this._phigrosService, this._localization, context.Command);
		List<Folder> collection = this._phigrosService.MultiLanguageInfos[language].Collections.EnsureNotNull();

		ColumnTextBuilder builder = new("Title", "Subtitle", "File Count");
		foreach (Folder item in collection)
		{
			builder.WithRow(item.Title, item.Subtitle, item.Files.Where(x => !string.IsNullOrEmpty(x.Content)).Count().ToString());
		}

		await context.Command.QuickReplyWithAttachments(" ", PSLUtils.ToAttachment(builder.Build().ToString(), "Chapters.txt"));
	}
	private async Task HandleListFiles(SocketSlashCommandDataOption option, Context context)
	{
		Language language = option.GetLanguageOption(this._phigrosService, this._localization, context.Command);
		List<Folder> collection = this._phigrosService.MultiLanguageInfos[language].Collections.EnsureNotNull();
		Folder chapter = collection.First(x => x.AddressableCoverPath == option.GetOption<string>(this._localization[PSLNormalCommandKey.CollectionOptionChapterName]));

		int actualCount = 0;
		ColumnTextBuilder builder = new("Name", "Date", "Category", "Supervisor", "Key");
		foreach (FileItem item in chapter.Files)
		{
			if (string.IsNullOrEmpty(item.Content)) continue;

			builder.WithRow(item.Name, item.Date, item.Category, item.Supervisor, item.Key);
			actualCount++;
		}

		string reply = $"""
			## {chapter.Title} {(string.IsNullOrWhiteSpace(chapter.Subtitle) ? "" : $"- {chapter.Subtitle}")}
			- Total Files: {actualCount}
			""";

		await context.Command.QuickReplyWithAttachments(reply, PSLUtils.ToAttachment(builder.Build().ToString(), "Files.txt"));
	}
	private async Task HandleListTips(SocketSlashCommandDataOption option, Context context)
	{
		Language language = option.GetLanguageOption(this._phigrosService, this._localization, context.Command);
		List<string> tips = this._phigrosService.MultiLanguageInfos[language].Tips;

		await context.Command.QuickReplyWithAttachments($"{tips.Count} available:", PSLUtils.ToAttachment(string.Join("\n", tips), "Tips.txt"));
	}

	private async Task HandleReadChapter(SocketSlashCommandDataOption option, Context context)
	{
		Language language = option.GetLanguageOption(this._phigrosService, this._localization, context.Command);
		List<Folder> collection = this._phigrosService.MultiLanguageInfos[language].Collections.EnsureNotNull();
		Folder chapter = collection.First(x => x.AddressableCoverPath == option.GetOption<string>(this._localization[PSLNormalCommandKey.CollectionOptionChapterName]));

		await context.Command.QuickReply($"""
			## {chapter.Title} {(string.IsNullOrWhiteSpace(chapter.Subtitle) ? "" : $"- {chapter.Subtitle}")}
			- Internal game cover path: `{chapter.AddressableCoverPath}`
			- Total Files: {chapter.Files.Where(x => !string.IsNullOrEmpty(x.Content)).Count()}
			""");
	}
	private async Task HandleReadItem(SocketSlashCommandDataOption option, Context context)
	{
		const double MinimumSimilarity = 0.75d;

		Language language = option.GetLanguageOption(this._phigrosService, this._localization, context.Command);
		List<Folder> collection = this._phigrosService.MultiLanguageInfos[language].Collections.EnsureNotNull();
		string search = option.GetOption<string>(this._localization[PSLNormalCommandKey.CollectionOptionSearchName]).Trim().ToLowerInvariant();

		// search collections
		List<CollectionSearchInfo> searchResults = [];
		foreach (Folder item in collection)
		{
			foreach (FileItem file in item.Files)
			{
				if (string.IsNullOrEmpty(file.Content)) continue;

				double keySimilarity = CalculateSimilarity(search, file.Key);
				double nameSimilarity = CalculateSimilarity(search, file.Name);

				double similarity = Math.Max(keySimilarity, nameSimilarity);
				searchResults.Add(new(file, item, similarity));
			}
		}

		searchResults.Sort();
		searchResults.Reverse();

		CollectionSearchInfo topResult = searchResults.First();

		if (searchResults.Count == 0 || topResult.Similarity < MinimumSimilarity)
		{
			await context.Command.QuickReply(this._localization[PSLNormalCommandKey.CollectionNoMatchingItemFound]);
			return;
		}

		List<CollectionSearchInfo> similarItems = searchResults.Where(x => x.Similarity > MinimumSimilarity).ToList();
		similarItems.Remove(topResult);

		await context.Command.QuickReplyWithAttachments($"""
			## {topResult.Item.Name}
			- In chapter: {topResult.CorrespondingChapter.Title}
			- Key: `{topResult.Item.Key}`
			- Date: `{topResult.Item.Date}`
			- Category: `{topResult.Item.Category}`
			- Supervisor: `{topResult.Item.Supervisor}`
			- Special properties: {(string.IsNullOrEmpty(topResult.Item.Properties) ? "<None>" : $"`{topResult.Item.Properties}`")}
			> Other similar items: {string.Join(", ", similarItems.Select(x => $"`{x.Item.Name}`"))}
			""", PSLUtils.ToAttachment(topResult.Item.Content.Replace(@"\r\n", @"\n").Replace(@"\n", "\n"), "Content.txt"));

		static double CalculateSimilarity(string a, string b)
		{
			if (a == b) return 1.0;
			return Fuzz.Ratio(a, b) * 0.01d;
		}
	}
	private async Task HandleRandomTip(SocketSlashCommandDataOption option, Context context)
	{
		Language language = option.GetLanguageOption(this._phigrosService, this._localization, context.Command);
		List<string> tips = this._phigrosService.MultiLanguageInfos[language].Tips;

		await context.Command.QuickReply(tips[Random.Shared.Next(tips.Count)]);
	}
}
file static class Extension
{
	public static SlashCommandOptionBuilder AddLanguageOption(this SlashCommandOptionBuilder builder, PhigrosService phigrosService, LocalizationService localization, bool isRequired = false)
	{
		return builder.AddOption(
			localization[PSLNormalCommandKey.CollectionOptionLanguageName],
			ApplicationCommandOptionType.Integer,
			localization[PSLNormalCommandKey.CollectionOptionLanguageDescription],
			isRequired: isRequired,
			choices: phigrosService.MultiLanguageInfos.Select(x => BuilderUtility.CreateChoice(x.Key.ToString(), (int)x.Key)).ToArray());
	}
	public static Language GetLanguageOption(
		this SocketSlashCommandDataOption option,
		PhigrosService phigrosService,
		LocalizationService localization,
		SocketSlashCommand arg)
	{
		Language desiredLang = option.GetOptionOrDefault(localization[PSLNormalCommandKey.CollectionOptionLanguageName], LocalizationHelper.FromCode(arg.UserLocale));
		if (phigrosService.MultiLanguageInfos.ContainsKey(desiredLang))
			return desiredLang;

		if (phigrosService.MultiLanguageInfos.ContainsKey(Language.EnglishUS))
			return Language.EnglishUS;

		return phigrosService.MultiLanguageInfos.First().Key;
	}
}
