# Getting Started (For Developers)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/yt6983138/PSLDiscordBot)

Project paths and build commands in this guide are relative to the PSLDiscordBot repository root, containing `PSLDiscordBot.sln`. When checked out as the `PSLDiscordBotBase/` submodule, use that directory. Runtime paths such as `./Plugins/` are relative to the process working directory.

## Prerequisites

### Build requirements
- .NET 10 SDK. Framework, Core, and CoreRunHelper target `net10.0`; the Analyzer targets `netstandard2.0`. Framework and Core enable preview C# features.
- Windows is the established build environment; the Framework prebuild step uses Windows commands.
- Debug Core builds reference an ignored local `Secret.cs`. Keep local credential definitions out of version control; do not invent credentials or bypass this prerequisite.

Build the standalone solution from its repository root:

```powershell
dotnet build "PSLDiscordBot.sln" --configuration Debug --nologo --verbosity minimal
```

### Runtime assets and image generation
- A Chromium-based browser. Configure `Config.ChromiumLocation`; Debug defaults to the `DEBUG_CHROME_LOCATION` environment variable.
- The Saira font family installed.
- [External game assets](https://github.com/yt6983138/PSLDiscordBot.Resources) and the corresponding Phigros metadata files.
- For `CoreRunHelper`, set **`ASSETS_COPY_PATH`** to the asset directory linked as `./Assets/`, and **`INFO_COPY_PATH`** to the metadata directory copied into `./PSL/`.

The browser, fonts, assets, and launcher environment variables are runtime prerequisites, not requirements for `dotnet build`. CoreRunHelper stages files and starts the Framework; it is not a build-only validation tool.

## Solution Layout
The project is laid like this:
1. `PSLDiscordBot.Framework`: Host, entry point, Discord gateway, DI container, plugin loading, command dispatch
2. `PSLDiscordBot.Core`: Plugin, all business logic, commands, services etc
3. `PSLDiscordBot.Analyzer`: Roslyn analyzer and localization-key source generator
4. `CoreRunHelper`: Launcher that stages Core assets, metadata, and plugin files for debugging

The Framework references the Analyzer project as a build-time analyzer. It does not depend on Core or concrete plugins at compile time; it discovers plugins at runtime by scanning `./Plugins/` for DLLs. Other generated interfaces use Antelcat.AutoGen.

## The Plugin System
Every plugin implements `IPlugin`. The normal startup/shutdown sequence is:
1. `IPlugin.Load(WebApplicationBuilder)`: Register services into the DI container.
2. `IPlugin.ConfigureDiscordClient(...)`: Configure the bot token and gateway intents.
3. `builder.Build()`: Framework builds the `WebApplication`.
4. `Program.AfterPluginsLoaded`: Hook after host construction, before plugin setup.
5. `IPlugin.Setup(WebApplication)`: Resolve services and hook events.
6. Framework starts the Discord clients and applies the shared MVC middleware.
7. `Program.AfterArgParse`: Hook after plugin arguments have been parsed and handled.
8. `CommandResolveService.SetupEverything()`: Hook command dispatch and command-upload handling.
9. `Program.AfterMainInitialize`: Hook immediately before `App.RunAsync`; Discord clients have already been started.
10. `App.RunAsync()`: Run the web host until shutdown.
11. `Program.BeforeMainExiting`: Pre-unload hook on normal host exit.
12. `IPlugin.Unload(WebApplication, bool isSafeUnload)`: Plugin cleanup.

`PluginResolveService` scans `./Plugins/` and its **immediate subdirectories only**, not deeper descendants, ignoring folders/files prefixed with `__`. Plugin entry classes must be constructible without constructor arguments. Plugins are sorted by ascending `Priority` before lifecycle calls: **lower values run earlier**.

## The Command System

### Discovery
`CommandResolveService` uses reflection to scan all loaded assemblies for types that:
1. Inherit from `BasicCommandBase` (slash commands), `BasicUserCommandBase`, or `BasicMessageCommandBase`
2. Have `[AddToGlobal]` attribute

Each discovered type is registered as a keyed singleton in the DI container (keyed by its own `Type`). The framework handles command upload/update on the Discord client's `Ready` event. Keep per-interaction mutable state in callback locals, not command instance fields.

### The inheritance pattern

- `BasicCommandBase`: Framework slash-command contract; no automatic Core account checks or deferral.
  - `CommandBase`: Injects common services, requires registration and (by default) TOS acceptance, and supports localization.
    - `GuestCommandBase`: Permits unregistered users and skips TOS checks by default; derived commands can require TOS acceptance.
    - `AdminCommandBase`: Checks the configured admin ID and skips registration/TOS requirements. Defaults to `BotDm` context; derived commands can broaden `InteractionContextTypes` without removing the runtime admin check.

`BasicCommandBase` defines `Name`, `Description`, `CompleteBuilder`, and `Execute`. Core-based commands implement `PSLName`, `PSLDescription`, `CompleteBuilder`, and `Callback` instead of overriding `Name` or `Description`.

`CommandBase` seals the `Name` and `Description` overrides, **not `Execute()`**. Guest/Admin bases override `Execute()` to apply their own access checks. Put ordinary command logic in `Callback`; these Core bases create/dispose a database requester and call `DeferAsync()` before invoking it.

Normal `CommandBase.Callback` receives registered, non-null `UserData`. Guest/Admin callbacks receive nullable `UserData`. Reuse the provided requester, and use the existing `QuickReply` / `QuickReplyWithAttachments` helpers to modify the deferred response rather than acknowledging the interaction again.

To add a command, start from the appropriate template in `PSLDiscordBot.Core/Command/Global/Template/`, rename it, and uncomment `[AddToGlobal]`. Implement its builder and callback while preserving the selected base's access checks.
