# Getting Started
If you are using this for the first time, please follow the steps to set up your account for the bot.

1. Run `/tos` command to view our TOS, then press the button to accept it.
2. Use `/account register qrcode` to link your account first.
    - Alternatively, you can also use `/account register token` if you already have token and can't or don't want to go through the login process.
    - If you don't want to link a cloud save, use `/account register local` to use the bot's local score system instead.
3. (Optional) Set your score precision using `/settings precision`. Setting this makes the bot show score with more digits.
4. (Optional) Make your public visibility on by running `/settings visibility`. Setting this makes your score available publicly on leaderboards and let people view your scores.
5. Now, you can get your scores by using `/records list [index]` or `/photo best [index]`. Also `/photo profile` for summary view.

# Command usage
Options in angle brackets (`<>`) are _required_ options, and options in square brackets (`[]`) are _optional_ options.

## `/help`
Prints guide for first time use.

## `/account register qrcode`
Registers your account with the bot by logging in with TapTap through a QR code. You must register before executing commands requiring Phigros account. If you are already registered, this switches your account to the new cloud save (the old binding is overwritten).

> Example: `/account register qrcode`

## `/account register token <token>`
Registers your account with the bot using token directly. If you want to find your token, please see [FindToken.md](FindToken.md). If you are already registered, this switches your account to the new cloud save (the old binding is overwritten).

> Example: `/account register token abcde12345fghij67890klmpq`

Options:
1. `token`: A string which has length of 25, only contains numbers and lowercase alphabets. <br/>
    - Warning: people _can_ use token to find your personal information, so do **not** leak your token! If you leaked it, logout in Phigros immediately.

## `/account register local`
Registers you with the bot's local score system, without linking a cloud save. Only available if you are not registered yet; if you are already registered, use `/account switch local` instead.

## `/account switch cloud`
Switches your registered account to use your linked cloud save. Your local scores are kept. Requires a previously linked cloud save; otherwise the bot replies that there is no cloud binding.

## `/account switch local`
Switches your registered account to use the local score system. Your cloud binding and any existing local scores are kept.

## `/local profile [nickname] [intro] [avatar] [background]`
Edits the profile of your local save. Omitted fields are kept; at least one must be given.
1. `nickname`: The new nickname.
2. `intro`: The new introduction.
3. `avatar`: The raw avatar key; it must be a known avatar. Suggestions of avatar keys and avatar aliases appear while typing, but submitting still needs the raw avatar key.
4. `background`: A song, given as its exact name or its id. Song suggestions from the aliases of the server you are in and the global alias table (the global table only in DMs) appear while typing; picking one fills in the song id. The save always stores the song name.

The `search` option of `/collection read item` shows suggestions for the chosen language while typing; picking one fills in the item key. Typed free text still resolves as before: an exact key first, then the closest fuzzy match.

## `/local challenge <color> <level>`
Sets the challenge mode rank of your local save. Only available while your account is in local mode.

> Example: `/local challenge 3 42`

Options:
1. `color`: The challenge rank color (pick from the choices).
2. `level`: The challenge level, from 0 to 99.

## `/local currency [kib] [mib] [gib] [tib] [pib]`
Sets the data/money/currency of your local save. Omitted units are kept; at least one unit must be given. Only available while your account is in local mode.

> Example: `/local currency mib 512 gib 3`

Options:
1. `kib`: KiB units, 0 to 1023.
2. `mib`: MiB units, 0 to 1023.
3. `gib`: GiB units, 0 to 1023.
4. `tib`: TiB units, 0 to 1023.
5. `pib`: PiB units, 0 to 1023.

## `/local score set <search> <difficulty> <score> <accuracy> <fc>`
Sets the record of one chart in your local collection, replacing the existing record of that chart if there is one. If several songs match the search, you get a menu to pick the one you meant, and nothing is saved until you pick. Only available while your account is in local mode.

> Example: `/local score set "Spasmodic" 2 1000000 100 true`

Options:
1. `search`: Searching strings, you can either put id, put alias, or put the song name. Suggestions from the aliases of the server you are in and the global alias table (the global table only in DMs) appear while typing.
2. `difficulty`: The chart difficulty (pick from the choices). The song must have a known chart of that difficulty.
3. `score`: Score from 0 to 1000000.
4. `accuracy`: Accuracy from 0 to 100.
5. `fc`: Whether the chart was a full combo.

A score of 1000000 requires 100% accuracy and the full combo flag, and 100% accuracy requires a score of 1000000 and the full combo flag; otherwise the bot rejects the input.

## `/local score remove <search> <difficulty>`
Removes the record of one chart from your local collection. Picking among several matching songs works the same as in `/local score set`. Only available while your account is in local mode.

> Example: `/local score remove "Spasmodic" 2`

Options:
1. `search`: Same as in `/local score set`.
2. `difficulty`: The chart difficulty (pick from the choices).

## `/local import csv <file> <mode>`
Imports a CSV file in the score export format (see `/records export`) into your local collection. The file must be at most 5 MiB. Works from both cloud and local mode, as long as you are registered.

If the import would not touch any existing chart, remove anything, change any non-score data (like profile data) and has no problems, it is applied at once. Otherwise the bot shows a preview (charts added, replaced, kept, removed, issues found; problems are listed in an attached report) with a Proceed button, and nothing is saved until you press it within the time shown. Only you can use the button, and it applies exactly the previewed result.

> Example: `/local import csv file:scores.csv mode:Upsert`

Options:
1. `file`: The CSV file to import.
2. `mode`: How imported records are combined with your collection. Only chart records are affected by the mode; other data that comes with the import (like profile data) replaces the existing data if it differs.
    - `Upsert`: Replace records of charts that already exist and add the missing ones; other existing records are kept.
    - `FillMissing`: Only add charts you don't have yet.
    - `ReplaceAll`: Use the imported records as your whole collection (existing records not in the import are removed).
    - `KeepBetter`: For charts you already have, keep whichever record is better (by accuracy, then score, then full combo); missing charts are added.

## `/local import android <file> <mode>`
Imports an Android PlayerPrefs XML save file into your local collection. Limits, confirmation and options are the same as in `/local import csv`.

> Example: `/local import android file:com.PigeonGames.Phigros.v2.playerprefs.xml mode:KeepBetter`

## `/local import cloud <mode>`
Imports the save of your linked cloud binding into your local collection. Requires a previously linked cloud save (`/account register qrcode` or `/account register token`); otherwise the bot replies that there is no cloud binding. An international (non-China) binding is rejected, because the international cloud save service has been discontinued. Your account mode and cloud binding are not changed. Confirmation and the `mode` option are the same as in `/local import csv`.

> Example: `/local import cloud mode:FillMissing`

## `/account status`
Shows your account status: whether you are in cloud or local mode, whether a cloud binding and a local score collection exist, and your token (partly hidden) if you have one.

## `/records list [index] [count]`
It gives you a table of your scores, in a TUI-style. Works with both cloud and local saves.

> Example: `/records list 0 114514`

Options:
1. `index`: Save time converted to index, 0 is always latest. Do `/cloud time-index` to get other index.
2. `count`: The number of scores to display.

## `/records improve [index] [give_me_at_least] [count]`
Show you a list of possible chart to push to get more rks. Only you can see the reply.

> Example: `/records improve 0 0.01`

Options:
1. `index`: Save time converted to index, 0 is always latest. Do `/cloud time-index` to get other index.
2. `give_me_at_least`: The least rks you wanted to get from each chart. (Default: have Phigros shown +0.01).
3. `count`: Controls how many charts should be shown. (Default 10).

## `/records export [index]`
It gives you a CSV attachment that has all your scores. Only you can see the reply.

> Example: `/records export 0`

Options:
1. `index`: Save time converted to index, 0 is always latest. Do `/cloud time-index` to get other index.

## `/records money [index]`
Get amount of data/money/currency you have in Phigros.

> Example: `/records money 0`

Options:
1. `index`: Save time converted to index, 0 is always latest. Do `/cloud time-index` to get other index.

## `/cloud time-index`
It prints out all your save time and index that points to it, 0 is always latest. You must do `/account register token` or `/account register qrcode` first, and your account must be in cloud mode (local accounts get a wrong-account-mode reply). Only you can see the reply.

Note: It seems like only a cloud save structure update/change would make a index change. Older save may not be supported due to missing information and different encryption algorithm.

## `/settings precision <precision>`
Set precision of value shown on commands that fetch your scores.

> Example: `/settings precision 5`

Options:
1. `precision`: Precision. Put 1 to get acc like 99.1, 2 to get acc like 99.12, repeat.

## `/tos`
View the Terms of Service. Press the green button below it to agree.

## `/account delete`
Deletes your account from the bot (including any local save data). You will be asked to confirm with a button first. (Your settings will be wiped too!)

## `/settings visibility <visibility>`
Set whether your scores or statistics is visible to public or not. Turning it off also removes you from the leaderboard.

> Example: `/settings visibility true`

Options:
1. `visibility`: Whether your data is visible to public or not.

## `/settings show-count <count>`
Set the default show count for `/photo best`.

> Example: `/settings show-count 30`

Options:
1. `count`: The default count going to be set.

## `/settings memorable-score set <score-number> <score-thoughts> [index]`
Sets your memorable score, shown in `/photo profile`.

> Example: `/settings memorable-score set 1 "My first Phi!"`

Options:
1. `score-number`: The score number shown in `/records list`, `/photo best`, aka the Number column.
2. `score-thoughts`: Your thought about this score, like how you did it etc.
3. `index`: Save time converted to index, 0 is always latest. Do `/cloud time-index` to get other index.

## `/settings memorable-score clear`
Clear the memorable score.

## `/settings view`
Shows your current settings: public visibility (on/off), score precision, the default show count for `/photo best`, and your memorable score with its thoughts. A value you never changed is shown as its default and marked `(default)` (a value explicitly set to the default also shows the mark); an unset memorable score shows "not set".

## `/photo profile [index] [generate_for]`
Get info about you in game.

> Example: `/photo profile 0`

Options:
1. `index`: Save time converted to index, 0 is always latest. Do `/cloud time-index` to get other index.
2. `generate_for`: Generate for other user. Users without `/settings visibility` on won't be generated.

## `/photo best [index] [count] [lower_bound] [show_what_grades] [show_what_difficulties] [cc_lower_bound] [cc_higher_bound] [generate_for]`
Get summary photo of your scores.

> Example: `/photo best 0 30`

Options:
1. `index`: Save time converted to index, 0 is always latest. Do `/cloud time-index` to get other index.
2. `count`: Counts to show. (Default: 30, or set through `/settings show-count`).
3. `lower_bound`: The lower bound of the show range. ex. `lower_bound: 69` and `count: 42` show scores from 69 to 110.
4. `show_what_grades`: Change what grades to show. Default: Show all. Use comma-separated list, ex. `S, Phi, Vu, Fc, False`.
5. `show_what_difficulties`: Change what difficulties to show. Default: Show all. Use comma-separated list, ex. `EZ, IN, Legacy`.
6. `cc_lower_bound`: Change the lower bound of scores' CC to show. Inclusive.
7. `cc_higher_bound`: Change the higher bound of scores' CC to show. Inclusive.
8. `generate_for`: Generate for other user. Users without `/settings visibility` on won't be generated.

## `/leaderboard [rank-using] [count]`
Displays the leaderboard.

> Example: `/leaderboard rank-using rks`

Options:
1. `rank-using`: Options for ranking criteria (Subcommand Group). If omitted, defaults to RKS. Available subcommands:
    - `accuracy [difficulty]`: Ranks by average accuracy.
    - `average-score [difficulty]`: Ranks by average score.
    - `total-score [difficulty]`: Ranks by total score.
    - `count [difficulty]`: Ranks by achieved count.
    - `rks`: Ranks by RKS.
    - `challenge-rank`: Ranks by challenge rank.
    
    Subcommand options:
    - `difficulty`: Use what difficulty to rank by (choices: `EZ`, `HD`, `IN`, `AT`, `All`).
2. `count`: Number of entries to display, defaults to 50.

## `/song info <search>`
Search info about songs. Usable without registering or accepting the TOS.

> Example: `/song info "Spasmodic"`

Options:
1. `search`: Searching strings, you can either put id, put alias, or put the song name. Suggestions from the aliases of the server you are in and the global alias table (the global table only in DMs) appear while typing; picking one fills in the song id.

## `/photo song <search> [index] [generate_for]`
Get scores for a specified song(s).

> Example: `/photo song "Spasmodic" 0`

Options:
1. `search`: Searching strings, you can either put id, put alias, or put the song name. Suggestions from the aliases of the server you are in and the global alias table (the global table only in DMs) appear while typing; picking one fills in the song id.
2. `index`: Save time converted to index, 0 is always latest. Do `/cloud time-index` to get other index.
3. `generate_for`: Generate for other user. Users without `/settings visibility` on won't be generated.

## `/song assets <search> [pez_chart_type]`
Download assets about song. Usable without registering or accepting the TOS. Only you can see the reply.

> Example: `/song assets "Spasmodic" 2`

Options:
1. `search`: Searching strings, you can either put id, put alias, or put the song name. Suggestions from the aliases of the server you are in and the global alias table (the global table only in DMs) appear while typing; picking one fills in the song id.
2. `pez_chart_type`: Select which chart to pack.

## `/ping`
Check the availability of the core services.

## `/report-problem <message> [attachments]`
Report a problem to author.

> Example: `/report-problem "The bot crashed when running records list"`

Options:
1. `message`: Describe the issue you met/Tell what was the problem.
2. `attachments`: The attachment you want to attach (like screenshot/stacktrace), can be used to show the issue.

## Alias-related Commands
Please visit [AliasSystem.md](AliasSystem.md) for more information.
