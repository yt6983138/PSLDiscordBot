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

	private readonly AliasAdminStrictConstraint _aliasAdminStrictConstraint;

	public AliasChangeAdminCommand(IServiceProvider provider) : base(provider)
	{
		this._aliasAdminStrictConstraint = provider.GetRequiredService<AliasAdminStrictConstraint>();
	}

	public override OneOf<string, LocalizedString> PSLName => "alias-change-admin";
	public override OneOf<string, LocalizedString> PSLDescription => "[Server super admin command] Add or remove admin role from the server table";

	// the strict check runs last to keep today's rejection order; ADR 0008 moves it to the admin-role leaf
	public override IReadOnlyList<IConstraint> MasterConstraints =>
	[
		this._registeredConstraint,
		this._tosConstraint,
		this._aliasAdminStrictConstraint
	];

	public override InteractionContextType[] InteractionContextTypes => [InteractionContextType.Guild];
	public override ApplicationIntegrationType[] IntegrationTypes => [ApplicationIntegrationType.GuildInstall];

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder
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

	public override async Task RootCallback(SocketSlashCommand arg, UserData? data)
	{
		Operation operation = arg.GetOption<Operation>("operation");
		IRole role = arg.GetOption<IRole>("target");

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
