using PSLDiscordBot.Core.Models.LocalImport;
using System.Text;

namespace PSLDiscordBot.Core.Command.Global;

public partial class LocalCommand
{
	// first payload byte routes the component to its workflow store; the rest is the pending ID
	private const byte ImportProceedComponentKind = 0x02;

	private const int ImportFileMaxBytes = 5 * 1024 * 1024;

	// downloads uploaded attachments for /local import
	private static readonly HttpClient ImportAttachmentClient = new();
	private static readonly TimeSpan ImportTimeout = TimeSpan.FromSeconds(30);

	/// <summary>
	/// callee should return directly if result is null. An exception is thrown if unhandlable.
	/// Once parsed, it should passed to <see cref="HandleLocalImportAsync(IDiscordInteraction, UserData?, ParsedLocalImport, LocalImportMode)"/>
	/// </summary>
	/// <param name="arg"></param>
	/// <param name="option"></param>
	/// <param name="parseFunc"></param>
	/// <returns></returns>
	private async Task<ParsedLocalImport?> HandleCommonImports(
		SocketSlashCommand arg,
		SocketSlashCommandDataOption option,
		Func<string, ParsedLocalImport> parseFunc)
	{
		IAttachment attachment = option.GetOption<IAttachment>(this._localization[PSLLocalCommandKey.LocalImportFileOptionName]);
		if (attachment.Size > ImportFileMaxBytes)
		{
			await arg.QuickReply(this._localization[PSLLocalCommandKey.LocalImportFileTooLarge]);
			return null;
		}

		// one deadline covers the download and every body read; shutdown cancellation is not handled here
		using CancellationTokenSource timeout = new(ImportTimeout);
		try
		{
			byte[] preamble = Encoding.UTF8.GetPreamble();

			using HttpResponseMessage response = await ImportAttachmentClient.GetAsync(attachment.Url, timeout.Token);
			response.EnsureSuccessStatusCode();
			Span<byte> bytes = (await response.Content.ReadAsByteArrayAsync(timeout.Token)).AsSpan();
			if (bytes.StartsWith(preamble))
				bytes = bytes[preamble.Length..];
			string str = Encoding.UTF8.GetString(bytes);
			ParsedLocalImport result = parseFunc.Invoke(str);
			return result;
		}
		catch (OperationCanceledException) when (timeout.IsCancellationRequested)
		{
			await arg.QuickReply(this._localization[PSLLocalCommandKey.LocalImportFileDownloadFailed]);
			return null;
		}
		catch (Exception ex) when (ex is HttpRequestException or IOException)
		{
			await arg.QuickReply(this._localization[PSLLocalCommandKey.LocalImportFileDownloadFailed]);
			return null;
		}
	}

	private async Task HandleImportCsv(SocketSlashCommand arg, UserData? data, SocketSlashCommandDataOption option)
	{
		UserData? localData = await this.GetRegisteredUserOrReplyAsync(arg, data);
		if (localData is null) return;

		LocalImportMode mode = (LocalImportMode)option.GetIntegerOptionAsInt32OrDefault(this._localization[PSLLocalCommandKey.LocalImportModeOptionName]);
		ParsedLocalImport? parsed = await this.HandleCommonImports(arg, option, this._importService.ParseCSV);
		if (parsed is null) return;

		await this.HandleLocalImportAsync(arg, localData, parsed, mode);
	}

	/// <summary>
	/// Android PlayerPrefs import entry point. Local mode is required first. See: <see cref="HandleImportCsv"/>
	/// </summary>
	private async Task HandleImportAndroid(SocketSlashCommand arg, UserData? data, SocketSlashCommandDataOption option)
	{
		UserData? localData = await this.GetRegisteredUserOrReplyAsync(arg, data);
		if (localData is null) return;

		LocalImportMode mode = (LocalImportMode)option.GetIntegerOptionAsInt32OrDefault(this._localization[PSLLocalCommandKey.LocalImportModeOptionName]);
		ParsedLocalImport? parsed = await this.HandleCommonImports(arg, option, this._importService.ParseAndroidPlayerPref);
		if (parsed is null) return;

		await this.HandleLocalImportAsync(arg, localData, parsed, mode);
	}

	/// <summary>
	/// Imports the retained cloud binding's save into the active local collection. Local mode is required; the binding is
	/// read without changing modes or leaderboard state, and the result goes through the common preparation/confirmation.
	/// </summary>
	private async Task HandleImportCloud(SocketSlashCommand arg, UserData? data, SocketSlashCommandDataOption option)
	{
		UserData? localData = await this.GetRegisteredUserOrReplyAsync(arg, data);
		if (localData is null) return;
		if (!localData.HasCloudBinding)
		{
			await arg.QuickReply(this._localization[PSLLocalCommandKey.LocalImportCloudNoBinding]);
			return;
		}

		LocalImportMode mode = (LocalImportMode)option.GetIntegerOptionAsInt32OrDefault(this._localization[PSLLocalCommandKey.LocalImportModeOptionName]);
		SaveContext? save = await this._saveService.TryHandleCloudAsync(localData.SaveCache, arg, raiseFetchedHook: false);
		if (save is null) return;

		ParsedLocalImport parsed = new(
			[],
			save.ReadGameRecord(),
			await localData.SaveCache.GetPlayerInfoAsync(),
			save.ReadGameUserInfo(),
			save.ReadGameProgress());

		await this.HandleLocalImportAsync(arg, localData, parsed, mode);
	}

	/// <summary>
	/// Common handler for every import source. Prepares the parsed rows against the explicit local collection. A result without
	/// a confirmation trigger is committed at once; any other result is staged until the owner clicks Proceed.
	/// </summary>
	public async Task HandleLocalImportAsync(IDiscordInteraction arg, UserData? data, ParsedLocalImport parsed, LocalImportMode mode)
	{
		UserData? localData = await this.GetRegisteredUserOrReplyAsync(arg, data);
		if (localData is null) return;

		LocalSaveData save;
		try
		{
			save = await this._saveService.ReadLocalSaveAsync(localData.UserId);
		}
		catch (SaveSourceUnavailableException ex) when (ex.Reason == SaveSourceUnavailableReason.LocalSaveMissing)
		{
			await arg.QuickReply(this._localization[PSLCommonKey.SaveHandlerOnLocalSaveMissing]);
			return;
		}

		StagedLocalImport prepared = this._importService.StageImport(localData.UserId, save, parsed, mode);
		if (!prepared.RequiresConfirmation)
		{
			bool applied = await this._saveService.CommitLocalImportAsync(prepared);
			string report = this.GetImportPreviewText(arg, prepared.Report);
			await arg.QuickReply(applied
				? $"{this._localization[PSLLocalCommandKey.LocalImportApplied].GetFormatted(arg.UserLocale)}\n{report}"
				: $"{this._localization[PSLLocalCommandKey.LocalEditNoChange].GetFormatted(arg.UserLocale)}\n{report}");
			return;
		}

		Guid importId = this._importStore.Add(localData.UserId, prepared);
		MessageComponent controls = new ComponentBuilderV2()
			.WithActionRow([new ButtonBuilder()
				.WithCustomId(this.CreateComponentId([ImportProceedComponentKind, .. importId.ToByteArray()]))
				.WithLabel(this._localization[PSLLocalCommandKey.LocalImportProceedButton], arg)
				.WithStyle(ButtonStyle.Success)])
			.Build();

		string proceedHint = this._localization[PSLLocalCommandKey.LocalImportProceedHint]
			.GetFormatted(arg.UserLocale, LocalWorkflowStore<StagedLocalImport>.LifeTime);

		FileAttachment[] issues = [];
		if (prepared.Report.Issues.Count > 0)
		{
			ColumnTextBuilder builder = new(["Reason", "Message"]);
			foreach (ImportIssue issue in prepared.Report.Issues)
			{
				builder.WithRow([issue.Reason.ToString(), issue.Message]);
			}
			issues = [PSLUtils.ToAttachment(builder.Build().ToString(), "issues.txt")];
		}

		await arg.QuickReplyWithAttachments(
			message => message.Components = controls,
			$"{this.GetImportPreviewText(arg, prepared.Report)}\n{proceedHint}",
			issues);
	}

	private async Task ProceedImportAsync(SocketMessageComponent component, UserData userData, Guid importId)
	{
		// atomic, owner-bound take; the prepared result is applied as-is and is never reloaded or remerged
		StagedLocalImport? prepared = this._importStore.TryTake(userData.UserId, importId);
		if (prepared is null)
		{
			await component.QuickReply(this._localization[PSLLocalCommandKey.LocalImportExpired]);
			return;
		}

		bool applied = await this._saveService.CommitLocalImportAsync(prepared);
		await component.QuickReply(message => message.Components = null, this._localization[applied ? PSLLocalCommandKey.LocalImportApplied : PSLLocalCommandKey.LocalEditNoChange]);
	}

	private string GetImportPreviewText(IDiscordInteraction interaction, ImportReport report)
	{
		return this._localization[PSLLocalCommandKey.LocalImportPreview].GetFormatted(
			interaction.UserLocale,
			report);
	}
}
