using FuzzySharp;
using PhiInfo.Core.Models.Information;

namespace PSLDiscordBot.Core.Command.Global;

public partial class CollectionCommand
{
	private sealed class ReadGroup : SubcommandGroupNode
	{
		private readonly CollectionCommand _root;

		public ReadGroup(CollectionCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.CollectionGroupReadName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.CollectionGroupReadDescription];

		protected override IReadOnlyList<SubcommandNode> CreateChildren() =>
		[
			new ReadChapterNode(this._root),
			new ReadItemNode(this._root),
			new ReadRandomTipNode(this._root),
		];
	}

	private sealed class ReadChapterNode : SubcommandNode
	{
		private readonly CollectionCommand _root;

		public ReadChapterNode(CollectionCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.CollectionSubCommandReadChapterName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.CollectionSubCommandReadChapterDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this._root.AddLanguageOption(
			this.BasicBuilder.AddOption(
				this._localization[PSLNormalCommandKey.CollectionOptionChapterName],
				ApplicationCommandOptionType.String,
				this._localization[PSLNormalCommandKey.CollectionOptionChapterReadDescription],
				isRequired: true,
				choices: this._root.GetChapterChoices()));

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			Language language = this._root.GetLanguageOption(option, arg);
			List<Folder> collection = this._phigrosService.MultiLanguageInfos[language].Collections.EnsureNotNull();
			Folder chapter = collection.First(x => x.AddressableCoverPath == option.GetOption<string>(this._localization[PSLNormalCommandKey.CollectionOptionChapterName]));

			await arg.QuickReply($"""
				## {FormatTitleAndSubTitle(chapter.Title, chapter.Subtitle)}
				- Internal game cover path: `{chapter.AddressableCoverPath}`
				- Total Files: {chapter.Files.Where(x => !string.IsNullOrEmpty(x.Content)).Count()}
				""");
		}
	}

	private sealed class ReadItemNode : SubcommandNode
	{
		private readonly CollectionCommand _root;

		public ReadItemNode(CollectionCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.CollectionSubCommandReadItemName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.CollectionSubCommandReadItemDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this._root.AddLanguageOption(
			this.BasicBuilder.AddOption(
				this._localization[PSLNormalCommandKey.CollectionOptionSearchName],
				ApplicationCommandOptionType.String,
				this._localization[PSLNormalCommandKey.CollectionOptionSearchDescription],
				isRequired: true));

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			const double MinimumSimilarity = 0.75d;

			Language language = this._root.GetLanguageOption(option, arg);
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
				await arg.QuickReply(this._localization[PSLNormalCommandKey.CollectionNoMatchingItemFound]);
				return;
			}

			List<CollectionSearchInfo> similarItems = searchResults.Where(x => x.Similarity > MinimumSimilarity).ToList();
			similarItems.Remove(topResult);

			await arg.QuickReplyWithAttachments($"""
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
	}

	private sealed class ReadRandomTipNode : SubcommandNode
	{
		private readonly CollectionCommand _root;

		public ReadRandomTipNode(CollectionCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.CollectionSubCommandReadRandomTipName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.CollectionSubCommandReadRandomTipDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this._root.AddLanguageOption(this.BasicBuilder);

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			Language language = this._root.GetLanguageOption(option, arg);
			List<string> tips = this._phigrosService.MultiLanguageInfos[language].Tips;

			await arg.QuickReply(tips[Random.Shared.Next(tips.Count)]);
		}
	}
}
