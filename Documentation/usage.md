## Quick Starting Guide
If you are first time using this, please follow the guide.
1. Use `/link-token <token>` or `/login` to link your account first. Having problem? See `/link-token` or `/login` usage.
2. (Optional) Set your score precision using `/set-precision`.
3. (Optional, this is almost optional in every case) Use `/get-time-index` to find the save you want to see (See `/get-time-index` usage)
4. Now, you can get your scores by using `/get-scores [index]` or `/get-photo [index]`. Also `/about-me` for summary view.
## Command usage

Options in `<example>` are _required_ options, in `[example]` are _optional_ options.
### /help
Usage: `/help` <br/>
Prints this guide.
### /login
Usage: `/login <is_international>` <br/>
Example: `/login true` <br/>
Login with TapTap. Once you do this you no longer need to do `/link-token`. <br/>
The `is_international` depends on your Phigros version, if you are using international version of Phigros<sup>[1]</sup>, set it to `true`. <br/>
<sup>[1]</sup>: At this time of this part being written, only `Phigros (Global)` downloaded from taptap.io and version >3.14.1 is international version, otherwise it is China version. <br/>
### /link-token
**Notice: You should use `/login` instead! This is obsolete.**<br/>
Usage: `/link-token <token> <is_international>` <br/>
Example: `/link-token abcde12345fghij67890klmpq false` <br/>
Link your token. You must link your token before doing anything (except `/help`). <br/>
Token is a string which has length of 25, only contains numbers and lowercase alphabets. <br/>
Warning: people CAN use token to find your personal information, so do NOT leak your token! 
If you leaked it, logout in Phigros immediately. (Don't worry we don't use your token for personal information) <br/>
How to find token: <br/>
[Chinese version](https://potent-cartwheel-e81.notion.site/Phigros-Bot-f154a4b0ea6446c28f62149587cd5f31)
#### Android - way 1

1. Find .userdata at `Android/data/com.PigeonGames.Phigros/files/`
(or more precisely, `/storage/emulated/[userid]/Android/data/com.PigeonGames.Phigros/files/.userdata`)
> Some device may differ, then find folder named `com.PigeonGames.Phigros` and try to find `.userdata` underneath it.
2. Open the `.userdata` file with any text editors, and find `"sessionToken": "abcdefg"`, the `abcdefg` is your token, save it carefully.
> For example, the file shows `"sessionToken": "abcdefghij1234567890abcde"`, then your token is `abcdefghij1234567890abcde`.

#### Android - way 2

Download [the file](http://qxsky.top:886/externalLinksController/chain/getstk.apk?ckey=UbcekU4SrbrP56nuuJsjSG4sR6XVva0QpH6cgRxykQ%2BLVKfVVy1N9ftDKol27wSM) and follow the guide. (May be Chinese)

#### iOS - way 1

Use [Ai-Si helper](https://m.i4.cn/) to export the backup, find the folder `AppDomain-games.Pigeon.Phigros/Documents` and `.userdata` is inside. You can also find file named `f48523d73831bfbdc9faf74eca5bf2999ca5bf54`, it is the `.userdata` file but with different name, then follow the Android way 1 step.

#### iOS - way 2

Summary: Grabbing the request sent from Phigros client to Phigros server (`https://phigrosserver.pigeongames.cn/1.1/classes/_GameSave`), this is faster but I only recommend it for **advanced users**.

1. Download a proxy app on your device.
2. Download [HTTP Toolkit](https://httptoolkit.com/) on your computer, and connect your device and your computer to same Wi-Fi. 
3. Get your computer IP and port which HTTP Toolkit is listening on (default 8000)，you can check it on main page (Proxy Port:8000), then install cert exported from HTTP Toolkit.
4. Add new proxy, and enter IP and port gotten from above step, and start it.
5. Start the service, then open Phigros and do sync, grab the request in HTTP Toolkit, then check the header of the request, the value of `x-lc-session` is your token.

#### iOS - way 3
Use an Android device temporally and follow the upper part.
### /get-time-index
Usage: `/get-time-index` <br/>
It prints out all your save time and index that means it, 0 is always latest. You must do `/link-token` or `/login` first.
### /export-scores
Usage: `/get-all-scores [index]` <br/>
Example: `/get-all-scores 0` <br/>
It gives you a CSV attachment that has all your scores. You must do `/link-token` or `/login` first.
### /get-scores
Usage: `/get-scores [index] [count]` <br/>
Example: `/get-scores 0 114514` <br/>
It gives you a table of your scores, rks, status (with specified length) etc. You must do `/link-token` or `/login` first.
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
Get your token. You must do `/link-token` or `/login` first.
### /song-scores
Usage: `/song-scores <search> [index]` <br/>
Example: `/song-scores volcanic 0` <br/>
It searches all your scores, you can input song name, a song alias, or song id to find the song. 
You must do `/link-token` or `/login` first. <br/>
### /get-photo
Usage: `/get-photo [count] [index]` <br/>
Example: `/get-photo 69 0` <br/>
Gives you a cool picture about your b19&1phi scores. You must do `/link-token` or `/login` first.
### /about-me
Usage: `/about-me [index]` <br/>
Example: `/about-me 0` <br/>
Gives you a cool picture about your statistics. You must do `/link-token` or `/login` first.
### /song-info
Usage: `/song-info <search>` <br/>
Example: `/song-info 321` <br/>
It searches songs in database, you can input song name, a song alias, or song id to find the song. 
You must do `/link-token` or `/login` first. <br/>
### /song-scores
Usage: `/song-scores <search> [index]` <br/>
Example: `/song-scores 2085 0` <br/>
It searches songs in database, you can input song name, a song alias, or song id to find the song, and show your score in cool images.
You must do `/link-token` or `/login` first. <br/>
### /poke
Usage: `/poke` <br/>
Poke me awa
