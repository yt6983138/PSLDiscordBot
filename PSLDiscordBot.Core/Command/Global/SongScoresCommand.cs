using Discord;
using Discord.WebSocket;
using PhigrosLibraryCSharp.Cloud.DataStructure;
using PhigrosLibraryCSharp.GameRecords;
using PSLDiscordBot.Core.Command.Global.Base;
using PSLDiscordBot.Core.ImageGenerating;
using PSLDiscordBot.Core.Localization;
using PSLDiscordBot.Core.Services;
using PSLDiscordBot.Core.UserDatas;
using PSLDiscordBot.Core.Utility;
using PSLDiscordBot.Framework;
using PSLDiscordBot.Framework.CommandBase;
using PSLDiscordBot.Framework.DependencyInjection;
using PSLDiscordBot.Framework.Localization;

namespace PSLDiscordBot.Core.Command.Global;

[AddToGlobal]
public class SongScoresCommand : CommandBase
{
	#region Injection
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	[Inject]
	public ImageGenerator ImageGenerator { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	#endregion

	public override bool IsEphemeral => false;

	public override LocalizedString? NameLocalization => this.Localization[PSLNormalCommandKey.SongScoresName];
	public override LocalizedString? DescriptionLocalization => this.Localization[PSLNormalCommandKey.SongScoresDescription];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder.AddOption(
			this.Localization[PSLCommonOptionKey.SongSearchOptionName],
			ApplicationCommandOptionType.String,
			this.Localization[PSLCommonOptionKey.SongSearchOptionDescription],
			isRequired: true)
		.AddOption(
			this.Localization[PSLCommonOptionKey.IndexOptionName],
			ApplicationCommandOptionType.Integer,
			this.Localization[PSLCommonOptionKey.IndexOptionDescription],
			isRequired: false,
			minValue: 0);

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		string search = arg.GetOption<string>(this.Localization[PSLCommonOptionKey.SongSearchOptionName]);
		int index = arg.GetIndexOption(this.Localization);

		List<SongAliasPair> searchResult = await requester.FindFromIdOrAlias(search, this.PhigrosDataService.IdNameMap);
		if (searchResult.Count == 0)
		{
			await arg.QuickReply(this.Localization[PSLCommonMessageKey.SongSearchNoMatch]);
			return;
		}

		PhigrosLibraryCSharp.SaveSummaryPair? pair = await data.SaveCache.GetAndHandleSave(
			arg,
			this.PhigrosDataService.DifficultiesMap,
			this.Localization,
			index);
		if (pair is null)
			return;
		(Summary summary, GameSave save) = pair.Value;
		GameUserInfo userInfo = await data.SaveCache.GetGameUserInfoAsync(index);
		GameProgress progress = await data.SaveCache.GetGameProgressAsync(index);
		UserInfo outerUserInfo = await data.SaveCache.GetUserInfoAsync();

		(CompleteScore? best, double rks) = PSLUtils.SortRecord(save);

		List<CompleteScore> scoresToShow = save.Records
			.Where(x =>
				searchResult.Any(y => y.SongId == x.Id))
			.ToList();

		if (scoresToShow.Count == 0)
		{
			await arg.QuickReply(this.Localization[PSLNormalCommandKey.SongScoresSongNotPlayed]);
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

		MemoryStream image = await this.ImageGenerator.MakePhoto(
			save.Records,
			best,
			data,
			summary,
			userInfo,
			progress,
			outerUserInfo,
			this.ConfigService.Data.SongScoresRenderInfo,
			rks,
			this.ConfigService.Data.DefaultRenderImageType,
			this.ConfigService.Data.RenderQuality,
			cancellationToken: this.ConfigService.Data.RenderTimeoutCTS.Token,
			extraArguments: extraArg
		);

		await arg.QuickReplyWithAttachments(
			[new(image, "ScoreAnalysis.png"),
				PSLUtils.ToAttachment(
					GetScoresCommand.ScoresFormatter(
						arg,
						scoresToShow,
						this.PhigrosDataService.IdNameMap,
						int.MaxValue,
						data,
						this.Localization,
						false,
						false,
						false),
					"Query.txt")],
			this.Localization[PSLNormalCommandKey.SongScoresQueryResult],
			search);
	}
}
