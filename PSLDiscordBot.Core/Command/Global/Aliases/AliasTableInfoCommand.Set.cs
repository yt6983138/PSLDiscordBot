using PSLDiscordBot.Core.Models.SongAlias;

namespace PSLDiscordBot.Core.Command.Global.Aliases;

public partial class AliasTableInfoCommand
{
	private sealed class SetNode : SubcommandNode
	{
		public SetNode(AliasTableInfoCommand root) : base(root)
		{
		}

		public override OneOf<string, LocalizedString> PSLName => nameof(Operation.Set).ToLower();
		public override OneOf<string, LocalizedString> PSLDescription => "Set information about table in current server";

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
			.AddOption(
				"field",
				ApplicationCommandOptionType.Integer,
				"Field to set.",
				isRequired: true,
				choices: BuilderUtility.CreateChoicesFromEnum<Field>())
			.AddOption(
				"value",
				ApplicationCommandOptionType.String,
				"Value to set. Please refer to the manual about what to fill.",
				isRequired: true);

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			using AliasService.StaticTableRequester staticRequester = this._aliasService.GetStaticTableRequester();
			AliasTableAttribute tableAttribute = staticRequester.GetTableAttributeOrDefault(AliasTableIdType.Server, arg.GuildId.EnsureNotNull());

			Field field = option.GetOption<Field>("field");
			string valueString = option.GetOption<string>("value");
			switch (field)
			{
				case Field.AllowInheritance:
					tableAttribute.AllowInheritance = bool.Parse(valueString);
					break;
				case Field.InheritsFrom:
					if (string.IsNullOrWhiteSpace(valueString) || valueString == "null")
					{
						tableAttribute.InheritsFrom = null;
						break;
					}

					AliasTableAttribute targetTableAttribute = staticRequester.GetTableAttributeOrDefault(valueString);
					if (!targetTableAttribute.AllowInheritance)
					{
						await arg.QuickReply("The specified table to inherit from does not exist or does not allow inheritance. Please check the table id and try again.");
						return;
					}

					tableAttribute.InheritsFrom = valueString;
					break;
				case Field.OverriddenSongAliases:
					List<string> songIds = valueString.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();

					string[] nonExistingSongIds = songIds.Where(x => this._phigrosService.NonMultiLanguageInfos.Songs.All(y => y.Id != x)).ToArray();
					if (nonExistingSongIds.Length > 0)
					{
						await arg.QuickReply($"Following song id does not exist: `{string.Join("`, `", nonExistingSongIds)}`");
						return;
					}

					tableAttribute.OverriddenSongAliases = valueString.Split(',').Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToList();
					break;
			}
			await staticRequester.AddOrUpdateAttribute(tableAttribute);
			await staticRequester.SaveChangesAsync();

			await arg.QuickReply("Done");
		}
	}
}
