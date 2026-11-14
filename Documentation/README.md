# Documentation

The bot's `/help` text (`help.md` and the localized `help_*.md` files) no longer lives in this repository.

It now lives in [`yt6983138/PSLDiscordBot.Localizations`](https://github.com/yt6983138/PSLDiscordBot.Localizations) under `Documentations/Help/`. It is edited through Weblate or that repository, and is copied to `./PSL/` on deploy as `help.md` and `help_{Discord locale}.md` (for example `help_fr.md`, `help_zh-CN.md`), which is where `HelpCommand` reads it.
