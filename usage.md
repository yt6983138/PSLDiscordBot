## What is this?
This is a **P**higros **S**core **L**ookup Discord bot (aka PSLDiscordBot), <br/>
you can get your scores by using `/get-scores` or `/get-all-scores` etc.
## How to use this?
If you are first time using this, please follow the guide.
1. Use `/link-token <token>` to link your account first. Having problem? See `/link-token` usage.
2. (Optional) Set your score precision using `/set-precision`.
3. Use `/get-time-index` to find the save you want to see, it will make following example:
```
Index | Date
0     | 2/4/2024 00:59:59
1     | 7/24/2023 12:38:04
2     | 10/27/2023 14:31:05
...
```
If you want to view save modified at 7/24/2023 12:38:04, remember index 1. <br/>
4. Now, you can get your scores by using `/get-scores <index> [count]` or `/get-all-scores <index>`. remember, index 0 is always latest.
## Command usage
### /help
Usage: `/help` <br/>
Prints this guide.
### /link-token
Usage: `/link-token <token>` <br/>
Example: `/link-token abcde12345fghij67890klmpq` <br/>
Link your token. You must link your token before doing anything (except `/help`). <br/>
Having problem finding token? Follow this [link](https://potent-cartwheel-e81.notion.site/Phigros-Bot-f154a4b0ea6446c28f62149587cd5f31).
### /get-time-index
Usage: `/get-time-index` <br/>
It prints out all your save time and index that means it, 0 is always latest. You must do `/link-token` first.
### /get-all-scores
Usage: `/get-all-scores <index>` <br/>
Example: `/get-all-scores 0` <br/>
It gives you a CSV attachment that has all your scores. You must do `/link-token` first.
### /get-scores
Usage: `/get-scores <index> [count]` <br/>
Example: `/get-scores 0 114514` <br/>
It gives you a table of your scores, rks, status (with specified length) etc. You must do `/link-token` first.
### /set-precision
Usage: `/set-precision <number, 16 >= number >= 1>` <br/>
Example: `/set-precision 5` <br/>
It sets the precision of score show when doing `/get-scores`. Example:
number = 1: acc: 99.1 <br/>
number = 2: acc: 99.12 <br/>
... <br/>
You also must do `/link-token` first.
### /get-token
Usage: `/get-token` <br/>
Get your token. You must do `/link-token` first.
### /query
Usage: `/query <index> <regex>` <br/>
Example: `/query 0 volcanic` <br/>
It searches all your scores with `regex` parameter by regex. You must do `/link-token` first. <br/>
Hint: You can add `(?i)` before the regex string (ex. `(?i)igall`) to have case insensitive search.