Chinese version](https://potent-cartwheel-e81.notion.site/Phigros-Bot-f154a4b0ea6446c28f62149587cd5f31)
# Android 
## Option 1

1. Find .userdata at `Android/data/com.PigeonGames.[Phigros or PhigrosGlobal]/files/` (or more precisely, `/storage/emulated/[userid]/Android/data/com.PigeonGames.Phigros/files/.userdata`)
    - It may differ on different devices.
2. Open the `.userdata` file with a text editor, and find `"sessionToken": "someverycrypticstring"`. The `someverycrypticstring` would be your token, save it carefully.
    - For example, the file shows `"sessionToken": "abcdefghij1234567890abcde"`, then your token is `abcdefghij1234567890abcde`.

## Option 2

Download [the file](http://qxsky.top:886/externalLinksController/chain/getstk.apk?ckey=UbcekU4SrbrP56nuuJsjSG4sR6XVva0QpH6cgRxykQ%2BLVKfVVy1N9ftDKol27wSM) and follow the guide. (May be Chinese)

# iOS
## Option 1
1. Use [Ai-Si helper](https://m.i4.cn/) to export the backup of the game.
2. Find `.userdata` inside the folder `AppDomain-games.Pigeon.Phigros/Documents`. 
    - You can also find file named `f48523d73831bfbdc9faf74eca5bf2999ca5bf54`, it is also a `.userdata` file but with different name.
3. Follow Android option 1, step 2.

## Option 2
Use an Android device temporarily and follow the guide above.

# Generic
## HTTP intercepting
By intercepting the request sent from Phigros client to Phigros server (`https://[Phigros save server domain]/1.1/classes/_GameSave`), you can get the token very quickly. But this is pretty technical so I only recommend it for _advanced users_.

1. Download a proxy app on your device.
2. Download [HTTP Toolkit](https://httptoolkit.com/) on your computer, and connect your device and your computer to same Wi-Fi. 
3. Get your computer IP and port which HTTP Toolkit is listening on (default 8000)，you can check it on main page (Proxy Port:8000), then install cert exported from HTTP Toolkit.
4. Add new proxy, and enter IP and port gotten from step above, and start it.
5. Start the service, then open Phigros and do sync, grab the request in HTTP Toolkit, then check the header of the request, the value of `x-lc-session` is your token.

