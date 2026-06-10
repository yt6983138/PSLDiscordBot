using Microsoft.Extensions.DependencyInjection;
using PSLDiscordBot.Core.Models.SongAlias;

namespace PSLDiscordBot.Core.Command.Global.Aliases;

[AddToGlobal]
public class AliasChangeAdminCommand : CommandBase
{
	private enum Operation
	{
		Add,
		Remove
	}

	private readonly PSLPlugin _pslPlugin;

	public AliasChangeAdminCommand(IServiceProvider provider) : base(provider)
	{
		this._pslPlugin = provider.GetRequiredService<PSLPlugin>();
	}

	public override OneOf<string, LocalizedString> PSLName => "alias-change-admin";
	public override OneOf<string, LocalizedString> PSLDescription => "[Server super admin command] Add or remove admin role from the server table";

	public override InteractionContextType[] InteractionContextTypes => [InteractionContextType.Guild];

	public override SlashCommandBuilder CompleteBuilder =>
		this.BasicBuilder
		.AddOption(
			"target",
			ApplicationCommandOptionType.Role,
			"The target to remove or add to admin role list",
			isRequired: true)
		.AddOption(
			"operation",
			ApplicationCommandOptionType.Integer,
			"The operation to do, add or remove the role.",
			isRequired: true,
			choices: BuilderUtility.CreateChoicesFromEnum<Operation>());

	public override async Task Callback(SocketSlashCommand arg, UserData data, DataBaseService.DbDataRequester requester, object executer)
	{
		Operation operation = arg.GetOption<Operation>("operation");
		IRole role = arg.GetOption<IRole>("target");

		SocketGuildUser guildUser = (SocketGuildUser)arg.User;
		if (!guildUser.GuildPermissions.Administrator
			&& guildUser.Id != this._pslPlugin.AdminUser?.Id)
		{
			// TODO: use same localization key in AliasServerAdminCommandBase
			await arg.QuickReply("You do not have the required permissions to execute this command.");
			return;
		}

		using AliasService.StaticTableRequester staticRequester = this._aliasService.GetStaticTableRequester();
		AliasTableAttribute attribute = staticRequester.GetTableAttributeOrDefault(AliasTableIdType.Server, arg.GuildId.EnsureNotNull());

		if (operation == Operation.Add)
		{
			attribute.AdminRoleIds.Add(role.Id);
		}
		else if (operation == Operation.Remove)
		{
			attribute.AdminRoleIds.Remove(role.Id);
		}

		await staticRequester.AddOrUpdateAttribute(attribute);
		await staticRequester.SaveChangesAsync();

		await arg.QuickReply($"Operation done successfully. Current admin roles: {string.Join(", ", attribute.AdminRoleIds)}");
	}
}
