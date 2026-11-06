using PhiInfo.Core.Models.Information;

namespace PSLDiscordBot.Core.Command.Global;

public partial class CollectionCommand
{
	private sealed class ListGroup : SubcommandGroupNode
	{
		private readonly CollectionCommand _root;

		public ListGroup(CollectionCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.CollectionGroupListName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.CollectionGroupListDescription];

		protected override IReadOnlyList<SubcommandNode> CreateChildren() =>
		[
			new ListChaptersNode(this._root),
			new ListFilesNode(this._root),
			new ListTipsNode(this._root),
		];
	}

	private sealed class ListChaptersNode : SubcommandNode
	{
		private readonly CollectionCommand _root;

		public ListChaptersNode(CollectionCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.CollectionSubCommandListChaptersName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.CollectionSubCommandListChaptersDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this._root.AddLanguageOption(this.BasicBuilder);

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			Language language = this._root.GetLanguageOption(option, arg);
			List<Folder> collection = this._phigrosService.MultiLanguageInfos[language].Collections.EnsureNotNull();

			ColumnTextBuilder builder = new("File Count", "Title");
			foreach (Folder item in collection)
			{
				string count = item.Files.Where(x => !string.IsNullOrEmpty(x.Content)).Count().ToString();
				builder.WithRow(count, FormatTitleAndSubTitle(item.Title, item.Subtitle));
			}

			await arg.QuickReplyWithAttachments(" ", PSLUtils.ToAttachment(builder.Build().ToString(), "Chapters.txt"));
		}
	}

	private sealed class ListFilesNode : SubcommandNode
	{
		private readonly CollectionCommand _root;

		public ListFilesNode(CollectionCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.CollectionSubCommandListFilesName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.CollectionSubCommandListFilesDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this._root.AddLanguageOption(
			this.BasicBuilder.AddOption(
				this._localization[PSLNormalCommandKey.CollectionOptionChapterName],
				ApplicationCommandOptionType.String,
				this._localization[PSLNormalCommandKey.CollectionOptionChapterListDescription],
				isRequired: true,
				choices: this._root.GetChapterChoices()));

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			Language language = this._root.GetLanguageOption(option, arg);
			List<Folder> collection = this._phigrosService.MultiLanguageInfos[language].Collections.EnsureNotNull();
			Folder chapter = collection.First(x => x.AddressableCoverPath == option.GetOption<string>(this._localization[PSLNormalCommandKey.CollectionOptionChapterName]));

			int actualCount = 0;
			ColumnTextBuilder builder = new("Date", "Category", "Supervisor", "Key", "Name");
			foreach (FileItem item in chapter.Files)
			{
				if (string.IsNullOrEmpty(item.Content)) continue;

				builder.WithRow(item.Date, item.Category, item.Supervisor, item.Key, item.Name);
				actualCount++;
			}

			string reply = $"""
				## {FormatTitleAndSubTitle(chapter.Title, chapter.Subtitle)}
				- Total Files: {actualCount}
				""";

			await arg.QuickReplyWithAttachments(reply, PSLUtils.ToAttachment(builder.Build().ToString(), "Files.txt"));
		}
	}

	private sealed class ListTipsNode : SubcommandNode
	{
		private readonly CollectionCommand _root;

		public ListTipsNode(CollectionCommand root) : base(root)
		{
			this._root = root;
		}

		public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.CollectionSubCommandListTipsName];
		public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.CollectionSubCommandListTipsDescription];

		public override SlashCommandOptionBuilder CompleteBuilder => this._root.AddLanguageOption(this.BasicBuilder);

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			Language language = this._root.GetLanguageOption(option, arg);
			List<string> tips = this._phigrosService.MultiLanguageInfos[language].Tips;

			await arg.QuickReplyWithAttachments($"{tips.Count} available:", PSLUtils.ToAttachment(string.Join("\n", tips), "Tips.txt"));
		}
	}
}
