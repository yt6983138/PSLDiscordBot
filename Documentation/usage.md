# Getting Started
If you are using this for the first time, please follow the steps to set up your account for the bot.

1. Run `/tos` command to view and accept our TOS.
2. Use `/login` to link your account first, you will see a parameter named `is_international`, this depends on where did you get your game. For example, on Apple store or TapTap (China) you would be using non-international account (fill `false` for this), on TapTap Global or Google Play, you would be using international account (fill `true`).
    - Alternatively, you can also use `/link-token` if you already have token and can't or don't want to go through the login process. You still need to fill out the `is_international` parameter.
3. (Optional) Set your score precision using `/set-precision`. Setting this makes the bot show score with more digits.
4. (Optional) Make your public visibility on by running `/set-public-visibility`. Setting this makes your score available publicly on leaderboards and let people view your scores.
5. Now, you can get your scores by using `/get-scores [index]` or `/get-photo [index]`. Also `/about-me` for summary view.

# Command usage
Options in angle brackets (`<>`) are _required_ options, and options in square brackets (`[]`) are _optional_ options.

## `/help`
Prints guide for first time use.

## `/login <is_international>`
Log you into the bot. You must log in before executing commands requiring Phigros account. 

> Example: `/login true`

Options:
1. `is_international`: The value depends on your Phigros version, if you are using international version of Phigros<sup>[1]</sup>, set it to `true`.
Login with TapTap.

<sup>[1]</sup>: At this time of this part being written, only `Phigros (Global)` downloaded from taptap.io and version >3.14.1 is international version, otherwise it is China version.

## `/link-token <token> <is_international>`
Log you into the bot using token directly. You must log in before executing commands requiring Phigros account. If you want to find your token, please see [FindToken.md](FindToken.md).

> Example: `/link-token abcde12345fghij67890klmpq false`

Options:
1. `token`: A string which has length of 25, only contains numbers and lowercase alphabets. <br/>
    - Warning: people _can_ use token to find your personal information, so do **not** leak your token! If you leaked it, logout in Phigros immediately.
2. `is_international`: See `/login` command for more information.

## `/get-time-index`
It prints out all your save time and index that points to it, 0 is always latest. You must do `/link-token` or `/login` first.

Note: It seems like only a cloud save structure update/change would make a index change. Older save may not be supported due to missing information and different encryption algorithm.

## `/export-scores [index]`
It gives you a CSV attachment that has all your scores.

> Example: `/export-scores 0`

Options:
1. `index`: Save index.

## `/get-scores [index] [count]`
It gives you a table of your scores, in a TUI-style.

> Example: `/get-scores 0 114514`

Options:
1. `index`: Save index.
2. `count`: The number of scores to display.

## `/set-precision <precision>`
Set precision of value shown on commands that fetch your scores.

> Example: `/set-precision 5`

Options:
1. `precision`: Precision. Put 1 to get acc like 99.1, 2 to get acc like 99.12, repeat.

## `/get-token`
Show your token.

## `/tos`
View the Terms of Service. Do this command twice agrees it.

## `/logout`
Logs you out of the bot. (Your settings will be wiped too!)

## `/set-public-visibility <visibility>`
Set whether your scores or statistics is visible to public or not.

> Example: `/set-public-visibility true`

Options:
1. `visibility`: Whether your data is visible to public or not.

## `/set-show-count-default <count>`
Set the default show count for `/get-photo`.

> Example: `/set-show-count-default 30`

Options:
1. `count`: The default count going to be set.

## `/set-memorable-score <score-number> <score-thoughts> [index]`
Sets your memorable score, shown in `/about-me`.

> Example: `/set-memorable-score 1 "My first Phi!"`

Options:
1. `score-number`: The score number shown in `/get-scores`, `/get-photo`, aka the Number column.
2. `score-thoughts`: Your thought about this score, like how you did it etc.
3. `index`: Save time converted to index, 0 is always latest. Do `/get-time-index` to get other index.

## `/clear-memorable-score`
Clear the memorable score.

## `/about-me [index]`
Get info about you in game.

> Example: `/about-me 0`

Options:
1. `index`: Save time converted to index, 0 is always latest. Do `/get-time-index` to get other index.

## `/get-photo [index] [count] [lower_bound] [show_what_grades] [cc_lower_bound] [cc_higher_bound] [generate_for]`
Get summary photo of your scores.

> Example: `/get-photo 0 30`

Options:
1. `index`: Save time converted to index, 0 is always latest. Do `/get-time-index` to get other index.
2. `count`: Counts to show. (Default: 30, or set through `/set-count-or-default`).
3. `lower_bound`: The lower bound of the show range. ex. `lower_bound: 69` and `count: 42` show scores from 69 to 110.
4. `show_what_grades`: Change what grades to show. Default: Show all. Use comma-separated list, ex. `S, Phi, Vu, Fc, False`.
5. `cc_lower_bound`: Change the lower bound of scores' CC to show. Inclusive.
6. `cc_higher_bound`: Change the higher bound of scores' CC to show. Inclusive.
7. `generate_for`: Generate for other user. Users without `/set-public-visibility` on won't be generated.

## `/get-money [index]`
Get amount of data/money/currency you have in Phigros.

> Example: `/get-money 0`

Options:
1. `index`: Save time converted to index, 0 is always latest. Do `/get-time-index` to get other index.

## `/more-rks [index] [give_me_at_least] [count]`
Show you a list of possible chart to push to get more rks.

> Example: `/more-rks 0 0.01`

Options:
1. `index`: Save time converted to index, 0 is always latest. Do `/get-time-index` to get other index.
2. `give_me_at_least`: The least rks you wanted to get from each chart. (Default: have Phigros shown +0.01).
3. `count`: Controls how many charts should be shown. (Default 10).

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

## `/song-info <search>`
Search info about songs.

> Example: `/song-info "Spasmodic"`

Options:
1. `search`: Searching strings, you can either put id, put alias, or put the song name.

## `/song-scores <search> [index] [generate_for]`
Get scores for a specified song(s).

> Example: `/song-scores "Spasmodic" 0`

Options:
1. `search`: Searching strings, you can either put id, put alias, or put the song name.
2. `index`: Save time converted to index, 0 is always latest. Do `/get-time-index` to get other index.
3. `generate_for`: Generate for other user. Users without `/set-public-visibility` on won't be generated.

## `/download-asset <search> [pez_chart_type]`
Download assets about song.

> Example: `/download-asset "Spasmodic" 2`

Options:
1. `search`: Searching strings, you can either put id, put alias, or put the song name.
2. `pez_chart_type`: Select which chart to pack.

## `/ping`
Check the availability of the core services.

## `/report-problem <message> [attachments]`
Report a problem to author.

> Example: `/report-problem "The bot crashed when running get-scores"`

Options:
1. `message`: Describe the issue you met/Tell what was the problem.
2. `attachments`: The attachment you want to attach (like screenshot/stacktrace), can be used to show the issue.

## Alias-related Commands
Please visit [AliasSystem.md](AliasSystem.md) for more information.
