using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.ImageGenerating;
using PSLDiscordBot.Core.Localization;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.Services.Phigros;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.Localization;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class SongScoresCommand : CommandBase
{
	private readonly ImageGenerator _imageGenerator;

	public SongScoresCommand(IOptions<Config> config, DataBaseService database, LocalizationService localization, PhigrosDataService phigrosData, ILoggerFactory loggerFactory, ImageGenerator imageGenerator)
		: base(config, database, localization, phigrosData, loggerFactory)
	{
		this._imageGenerator = imageGenerator;
	}

	public override bool IsEphemeral => false;

	public override OneOf<string, LocalizedString> PSLName => this._localization[PSLNormalCommandKey.SongScoresName];
	public override OneOf<string, LocalizedString> PSLDescription => this._localization[PSLNormalCommandKey.SongScoresDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			this._localization[PSLCommonOptionKey.SongSearchOptionName],
			ApplicationCommandOptionType.String,
			this._localization[PSLCommonOptionKey.SongSearchOptionDescription],
			isRequired: true)
		.AddOption(
			this._localization[PSLCommonOptionKey.IndexOptionName],
			ApplicationCommandOptionType.Integer,
			this._localization[PSLCommonOptionKey.IndexOptionDescription],
			isRequired: false,
			minValue: 0);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		string search = arg.GetOption<string>(this._localization[PSLCommonOptionKey.SongSearchOptionName]);
		int index = arg.GetIndexOption(this._localization);

		List<SongAliasPair> searchResult = await requester.FindFromIdOrAlias(search, this._phigrosDataService.IdNameMap);
		if (searchResult.Count == 0)
		{
			await arg.QuickReply(this._localization[PSLCommonMessageKey.SongSearchNoMatch]);
			return;
		}

		PhigrosLibraryCSharp.SaveSummaryPair? pair = await data.SaveCache.GetAndHandleSave(
			arg,
			this._phigrosDataService.DifficultiesMap,
			this._localization,
			index);
		if (pair is null)
			return;
		(Summary summary, GameSave save) = pair.Value;
		GameUserInfo userInfo = await data.SaveCache.GetGameUserInfoAsync(index);
		GameProgress progress = await data.SaveCache.GetGameProgressAsync(index);
		UserInfo outerUserInfo = await data.SaveCache.GetUserInfoAsync();

		List<CompleteScore> scoresToShow = save.Records
			.Where(x =>
				searchResult.Any(y => y.SongId == x.Id))
			.ToList();

		if (scoresToShow.Count == 0)
		{
			await arg.QuickReply(this._localization[PSLNormalCommandKey.SongScoresSongNotPlayed]);
			return;
		}

		#region Score preprocessing 

		CompleteScore[] scoresSameToFirstId = scoresToShow
			.Where(x => x.Id == scoresToShow[0].Id)
			.ToArray();

		var extraArg = new
		{
			Searched = new Dictionary<string, CompleteScore[]>()
		};

		IEnumerable<IGrouping<string, CompleteScore>> grouped = scoresToShow.GroupBy(x => x.Id);

		foreach (IGrouping<string, CompleteScore> item in grouped)
		{
			extraArg.Searched.Add(item.Key, item.ToArray());
		}
		#endregion

		MemoryStream image = await this._imageGenerator.MakePhoto(
			save,
			data,
			summary,
			userInfo,
			progress,
			outerUserInfo,
			this._config.Value.SongScoresRenderInfo,
			this._config.Value.DefaultRenderImageType,
			this._config.Value.RenderQuality,
			cancellationToken: this._config.Value.RenderTimeoutCTS.Token,
			extraArguments: extraArg
		);

		await arg.QuickReplyWithAttachments(
			[new(image, "ScoreAnalysis.png"),
				PSLUtils.ToAttachment(
					GetScoresCommand.ScoresFormatter(
						arg,
						new() { CreationDate = default, ModificationTime = default, Records = scoresToShow },
						this._phigrosDataService.IdNameMap,
						int.MaxValue,
						data,
						this._localization,
						false,
						false,
						false),
					"Query.txt")],
			this._localization[PSLNormalCommandKey.SongScoresQueryResult],
			search);
	}
}
