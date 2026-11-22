using Microsoft.Extensions.DependencyInjection;
using PSLDiscordBot.Core.Models.SongAlias;

namespace PSLDiscordBot.Core.Command.Global.Aliases;

[AddToGlobal]
public partial class AliasServerAdminCommand : AliasServerAdminCommandBase
{
	private readonly AliasAdminStrictConstraint _aliasAdminStrictConstraint;

	public AliasServerAdminCommand(IServiceProvider provider) : base(provider)
	{
		this._aliasAdminStrictConstraint = provider.GetRequiredService<AliasAdminStrictConstraint>();
	}

	public override OneOf<string, LocalizedString> PSLName => "alias-server-admin";
	public override OneOf<string, LocalizedString> PSLDescription => "[Server admin command] Manage this server's alias table, audit aliases and admin roles";

	public override SlashCommandBuilder CompleteBuilder => this.BasicBuilder;

	protected override IReadOnlyList<CommandNode> CreateChildren() =>
	[
		new InfoGroup(this),
		new AuditGroup(this),
		new AdminRoleNode(this, this._aliasAdminStrictConstraint),
	];

	private sealed class AdminRoleNode : SubcommandNode
	{
		private enum Operation
		{
			Add,
			Remove
		}

		private readonly AliasAdminStrictConstraint _strictConstraint;

		public AdminRoleNode(AliasServerAdminCommand root, AliasAdminStrictConstraint strictConstraint) : base(root)
		{
			this._strictConstraint = strictConstraint;
		}

		public override OneOf<string, LocalizedString> PSLName => "admin-role";
		public override OneOf<string, LocalizedString> PSLDescription => "[Server super admin command] Add or remove admin role from the server table";

		// runs after the root's alias admin, registered and TOS checks
		public override IReadOnlyList<IConstraint> Constraints => [this._strictConstraint];

		public override SlashCommandOptionBuilder CompleteBuilder => this.BasicBuilder
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

		public override async Task Callback(SocketSlashCommand arg, SocketSlashCommandDataOption option, UserData? data)
		{
			Operation operation = option.GetOption<Operation>("operation");
			IRole role = option.GetOption<IRole>("target");

			using AliasService.StaticTableRequester staticRequester = this._aliasService.GetStaticTableRequester();
			AliasTableAttribute attribute = staticRequester.GetTableAttributeOrDefault(AliasTableId.Server(arg.GuildId.EnsureNotNull()));

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
}
