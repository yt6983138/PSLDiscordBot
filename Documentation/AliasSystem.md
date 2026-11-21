# Alias System
This system is implemented to prevent pollution with global tables in large server, reducing confusion and wrong song lookups.

In this system, aliases are stored in different tables. Each server can contain one table which can inherit single table, or be inherited.
[Original issue](https://github.com/yt6983138/PSLDiscordBot/issues/32)

## Which song wins when several match equally

This applies to the commands that resolve a song by search (strict matching over the merged alias table). When a name or alias matches several songs with the same score, the song is chosen in this order: a match on the song's id or name comes before a match on an alias; any remaining tie goes to the song whose id is first in ordinal (case-sensitive) comparison. The result is the same every time. Autocomplete lists are separate: they rank by match tier and keep song order on ties.

## Rules for adding an alias

`/alias-modify-global Add`, `/alias-modify-server Add` and `/alias-audit modify` each check only the table being edited (the global table, or this server's table) plus the song data. No other alias table is read: not the global table for a server add, not other servers' tables, and not inherited tables. The check is not atomic, so two simultaneous adds can both pass.

The new alias is compared case-insensitively, as search does, and is rejected when it equals, in this order:
1. an alias already stored for the same song in that table;
2. the id or name of any song, including the target song's own;
3. an alias stored for another song in that table.

Cross-table note: a server alias may equal another song's global alias. That is allowed, but the commands that resolve a song by search then find both songs. `/alias-modify-*` and `/alias-audit` need exactly one match, so they reply with a multiple-match error; picking the song from autocomplete (which sends the song id) avoids that. Where a single result is picked, the tie order above applies and may choose the other song. A server admin can fix this by overriding the other song on the server (`OverriddenSongAliases` in `/alias-table-info set`). A server may also hold a copy of its own song's global alias; the copy stays if the global one is removed.

## Server Super Admin Commands (Require `Administrator` permission)

### `/alias-change-admin <target> <operation>`
Add or remove an admin role from the server alias table. Users with roles registered via this command can run `Server admin command`s.

> Example: `/alias-change-admin @RoleName add`

Options:
1. `target`: Target role to modify.
2. `operation`: Operation to perform (e.g., `add` or `remove`).

## Server Admin Commands 

### `/alias-audit <subcommand> [options]`
Audit alias table. Retrieve or manage detailed metadata records for aliases in your server.

Subcommands:
- `info <for-song> <alias>`: Get info about an alias.
- `modify <for-song> <alias> <new-alias>`: Modify an alias to a new string. The new alias follows the same rules as `/alias-modify-server Add` (see "Rules for adding an alias"), except the alias being replaced is ignored, so a case-only rename such as `Foo` to `foo` works. Renaming to the identical text, or to another case variant that is also stored, is rejected. Rejections use the same localized replies as `/alias-modify-server Add`; the command's other replies are English.
- `remove <for-song> <alias>`: Remove an alias.

Note: the `for-song` option suggests only songs that have an alias in this server's table (matched by id, name or server alias; the picked value is the song id). While the field is empty it lists the first 25 of those songs in song order. The `alias` option suggests that song's aliases in this server's table (all of them while the field is empty, otherwise those containing the typed text); nothing is suggested when the song is not chosen yet, does not resolve to exactly one match, or before the server's alias table has been loaded.

### `/alias-table-info <subcommand> [options]`
Manage information about the alias table in the current server.

Subcommands:
- `get`: Get information about the alias table in the current server.
- `set <field> <value>`: Set information about the table in the current server. Fields include `AllowInheritance`, `InheritsFrom`, and `OverriddenSongAliases`.
	- The value for `AllowInheritance` is a boolean (true/false).
	- The value for `InheritsFrom` is a string. (eg. `Global_0` for global table, `Server_123` for server with id of `123`)
		- If the specified table does not exist or the target table does not have inheritance enabled, the command will fail.
		- Use `null` or empty string to specify no inherited table.
	- The value for `OverriddenSongAliases` is a comma-separated list of song ids. (eg. `Glaciaxion.SunsetRay.0,Realms.HinkikAHimitsu.0`)
		- Overridden songs will only have their aliases specified in the current table, and will not inherit aliases for those songs from parent tables.
		- For example, if song `Song.Artist.0` has alias `A` in the global table, and `B` in the server table, then only alias `B` will be active for that song in the server.

## Server Member Commands

### `/alias-modify-server <operation> <for-song> <alias-to-operate>`
Add or remove an alias for a specific song in the server-specific alias table. This allows your server to have custom song aliases without affecting other servers.

> Example: `/alias-modify-server Add volcanic vol`

Options:
1. `operation`: Operation to perform (e.g., `Add` or `Remove`).
2. `for-song`: The song identifier or search term.
3. `alias-to-operate`: The alias string to add or remove.

An `Add` follows "Rules for adding an alias", applied to this server's table only. `for-song` is resolved against this server's merged alias table (its own, inherited and global aliases, honouring `OverriddenSongAliases`) and also matches song ids and names, and it needs exactly one match. `Remove` removes only aliases in this server's table.

The `for-song` option suggests songs while typing (this server's aliases and the global ones, a server alias is shown first on a tie; the picked value is the song id). When the operation is `Remove`, `alias-to-operate` suggests the chosen song's existing aliases in this server's table (all of them while the field is empty, otherwise those containing the typed text). Nothing is suggested for `Add`, when the operation is not chosen yet, when the song does not resolve to exactly one match, or before the alias tables have been loaded.

## Global Member commands

### `/alias-modify-global <operation> <for-song> <alias-to-operate>`
Add or remove an alias for a specific song in the global alias table. This affects *all* servers and users. Please think twice before using this command.

> Example: `/alias-modify-global Remove volcanic vol`

Options:
1. `operation`: Operation to perform (e.g., `Add` or `Remove`).
2. `for-song`: The song identifier or search term.
3. `alias-to-operate`: The alias string to add or remove.

An `Add` follows "Rules for adding an alias", applied to the global table.

Autocomplete works as for `/alias-modify-server`, except the aliases suggested by `alias-to-operate` come from the global table.
