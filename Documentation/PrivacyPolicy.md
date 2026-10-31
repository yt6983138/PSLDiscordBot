When we say "the bot", we refer to the Phigros Score Lookup bot developed by our team,
and when we say "our team", we refer to the Phigros Score Lookup Team.

Our team, and the bot, are not associated with Pigeon Games, TapTap, and Discord.

# Privacy Policy

Last updated: 2026-10-31

The use of this application ("Bot") in a server requires the collection of some specific user data ("Data"). The Data collected includes, but is not limited to, the items listed under "Storage of Data" below. Use of the Bot is considered an agreement to the terms of this Policy.

## Why We Process Data

We process Data only to provide the features you request: score lookup, image rendering, local collections, save archiving and transfer (only on your request), moderation and abuse prevention, and support. Registering or logging in is your authorization for the Bot to process your Data for these purposes. You can withdraw this authorization at any time with `/account delete`. Save archives are removed separately, see "Retention" below.

## Access to Data

The messages sent by users are only read if they are sent in our Discord server, in the anti-raid channel or verify channel. We do not store your message content.

Access to Data is only permitted to the Bot's developers, and only in the scope required for the development, testing, and implementation of features for the Bot. Data is not sold, provided to, or shared with any third party, except as described under "Third Parties" below, or where required by law or a Terms of Service agreement. You can view the Data upon request to our team.

## Third Parties

Some of your Data necessarily passes through the following third parties. Their handling of Data is governed by their own policies, not this one.

1. **Discord:** the messages and interactions you send to the Bot, and the Bot's replies, pass through Discord. See the [Discord Privacy Policy](https://discord.com/privacy).
2. **Phigros cloud save services:** to fetch your save, the Bot's Phigros library contacts only TapTap services (login), LeanCloud (the Phigros cloud save backend), and the save file CDNs Qiniu and AWS. The Bot sends your Phigros token to the cloud save backend. When you transfer a save to the China server, the Bot writes your save data to your destination China server account.
3. **TapTap login:** if you log in through TapTap's QR login, that login flow is governed by TapTap's own privacy policy ([TapTap Global](https://www.taptap.io/doc/privacy-policy/), [TapTap China](https://www.taptap.cn/doc/privacy-policy/)). The Bot never receives your TapTap password, but it temporarily receives a TapTap authorization token scoped to Phigros only, which is discarded right after use and not stored.
4. **QR code image service:** the login message may include a convenience link that generates a QR code image through `api.qrserver.com`. The link contains your one-time TapTap login address, and it is only sent to that service if you open the link. You can ignore the link and scan the login address by other means.

We do not sell or share your Data with anyone else.

## Storage of Data

Data is stored on servers operated by our team, in a SQLite database and in files. These servers may be located outside your country or region (including outside mainland China). By using the Bot, you acknowledge that your Data may be transferred and stored there. The China transfer feature, when you request it, sends your save data to the China server cloud.

The database and files are protected by server access controls. The database, including your Phigros token, and the archive files are not encrypted by the Bot. All connections from the Bot to TapTap, LeanCloud, Qiniu, and AWS use HTTPS. No guarantee is provided and the bot owners assume no liability for the unintentional or malicious breach of Data. In the event of an unauthorised Data access, users will be notified through the Discord client application.

The Data we store:

1. **Account data:** your Discord user ID, your Phigros token, the region (international or not) of that token, which data source you currently use (cloud account or local collection), and the level of the Terms of Service you agreed to. Your Phigros token is kept even while you use a local collection, so you can switch back to it.
2. **Preferences:** settings you chose, such as public visibility, formatting options and image count.
3. **Local collections:** if you use a local collection, the scores and profile you entered or imported (nickname, introduction, avatar, background, challenge rank and currency). A local collection you are not currently using is kept until you delete your account with `/account delete`.
4. **Save archives:** copies of your Phigros cloud saves and player information, stored as files on the Bot's server and kept so that you can import them into a local collection or request them later. The archive files are not encrypted by the Bot.
5. **User-submitted content:** aliases you submit, which are linked to your Discord user ID.
6. **Reports:** the text, attachments and Discord user ID of reports you send with `/report-problem`, stored in a SQLite database and files on the Bot's server.

Your TapTap user information is not stored. The Phigros-scoped TapTap authorization token received temporarily during QR login is used only to obtain your Phigros token and is discarded immediately afterwards; it is not stored. Your Phigros game save is not stored by the Bot except as described above (local collections and save archives). Access to all of this Data is limited as described in the "Access to Data" section above.

## Retention

1. **Account data, token, preferences and local collections:** kept until you delete your account with `/account delete`.
2. **Save archives:** kept for up to 6 months from creation, then deleted. `/account delete` does not remove archives; request their removal as described under "User Rights".
3. **Reports:** kept as long as needed to handle the report, and deleted on request.
4. **Aliases:** kept while in use, and removed on request.

## User Rights

At any time, you have the right to request to view the Data pertaining to your Discord account, to retrieve your archived saves, to correct your Data, and to request the removal of that Data, including your archived saves. You may submit a request through a ticket in our Discord server mentioned in the bot description, or with the `/report-problem` command. We may ask you to confirm your identity, for example by making the request from the Discord account concerned. We aim to respond within a reasonable time.

You can also remove your registration, preferences, leaderboard entry and local collection yourself with `/account delete`. Save archives are not removed by this command; request their removal as described above.

## Changes to This Policy

We may update this Policy. Updates are announced in our Discord server, and the "Last updated" date above changes.

## Underage Users

The use of the Bot is not permitted for minors under the age of 13, or under the age of legal consent for their country. This is in compliance with the [Discord Terms of Service](https://discord.com/terms). No information will be knowingly stored from an underage user. If it is found out that a user is underage we will take all necessary action to delete the stored data.

## Questions

If you have any questions or are concerned about what data might be stored from your account, contact our team members. For more information check the [Discord Terms Of Service](https://discord.com/terms).
