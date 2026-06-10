# Alias System
This system is implemented to prevent pollution with global tables in large server, reducing confusion and wrong song lookups.

In this system, aliases are stored in different tables. Each server can contain one table which can inherit single table, or be inherited.
[Original issue](https://github.com/yt6983138/PSLDiscordBot/issues/32)

## Server Super Admin Commands (Require `Administrator` permission)

### /alias-change-admin
Usage: `/alias-change-admin <target> <operation>` <br/>
Example: `/alias-change-admin @RoleName add` <br/>

Add or remove an admin role from the server alias table. Users with roles registered via this command can run [Server admin command]s.

## Server Admin Commands 

### /alias-audit
Usage: `/alias-audit <subcommand> [options]` <br/>
Subcommands:
- `info <for-song> <alias>`: Get info about an alias.
- `modify <for-song> <alias> <new-alias>`: Modify an alias to a new string.
- `remove <for-song> <alias>`: Remove an alias.

Audit alias table. Retrieve or manage detailed metadata records for aliases in your server.

### /alias-table-info
Usage: `/alias-table-info <subcommand> [options]` <br/>
Subcommands:
- `get`: Get information about the alias table in the current server.
- `set <field> <value>`: Set information about the table in the current server. Fields include `AllowInheritance`, `InheritsFrom`, and `OverriddenSongAliases`.
	- The value for `AllowInheritance` is a boolean (true/false).
	- The value for `InheritsFrom` is a string. (eg. `Global_0` for global table, `Server_123` for server with id of `123`)
		- If the specified table does not exist or the target table does not have inheritance enabled, the command will fail.
	- The value for `OverriddenSongAliases` is a comma-separated list of song ids. (eg. `Glaciaxion.SunsetRay.0,Realms.HinkikAHimitsu.0`)
		- Overridden songs will only have their aliases specified in the current table, and will not inherit aliases for those songs from parent tables.
		- For example, if song `Song.Artist.0` has alias `A` in the global table, and `B` in the server table, then only alias `B` will be active for that song in the server.

## Server Member Commands

### /alias-modify-server
Usage: `/alias-modify-server <operation> <for-song> <alias-to-operate>` <br/>
Example: `/alias-modify-server Add volcanic vol` <br/>
Add or remove an alias for a specific song in the server-specific alias table. This allows your server to have custom song aliases without affecting other servers.

## Global Member commands

### /alias-modify-global
Usage: `/alias-modify-global <operation> <for-song> <alias-to-operate>` <br/>
Example: `/alias-modify-global Remove volcanic vol` <br/>
Add or remove an alias for a specific song in the global alias table. This affects *all* servers and users. Please think twice before using this command.
