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

The Framework references the Analyzer project both as an analyzer and as a normal runtime reference that flows to plugins, so the Analyzer and Roslyn ship with the bot; only projects that reference it as an analyzer (Framework, Core) run its generators. It does not depend on Core or concrete plugins at compile time; it discovers plugins at runtime by scanning `./Plugins/` for DLLs. Other generated interfaces use Antelcat.AutoGen.

## The Plugin System
Every plugin implements `IPlugin<TSelf>` (which extends `IPlugin`). It declares static `Name`, `Description`, `Author`, and `Priority`, plus static `Load` and `Setup`. The normal startup/shutdown sequence is:
1. `TSelf.Load(WebApplicationBuilder)`: Register services into the DI container, including `Configure<DiscordClientServiceConfig>` for the bot token and gateway intents. No plugin instance exists yet; register MVC assemblies with `AddAssemblyToMvc<TSelf>()`.
2. `builder.Build()`: Framework builds the `WebApplication`.
3. `TSelf.Setup(WebApplication)`: Resolve services, hook events, register argument receivers, and return the plugin instance.
4. Framework starts the Discord clients and applies the shared MVC middleware.
5. Plugin arguments are parsed and handled.
6. `CommandResolveService.SetupEverything()`: Hook command dispatch and command-upload handling.
7. `ILifetimeService.AfterMainInitialize`: Raised (and awaited) immediately before `App.RunAsync`; Discord clients have already been started.
8. `App.RunAsync()`: Run the web host until shutdown.
9. `ILifetimeService.BeforeMainExiting`: Pre-unload hook on normal host exit.
10. `IPlugin.Unload(WebApplication, bool isSafeUnload)`: Plugin cleanup; a throwing plugin is logged and later plugins still unload.

Lifetime events are async events (`[GenerateAsyncEvent]`, `AsyncEventHandler<T>`, namespace `PSLDiscordBot.Analyzer.AsyncEventHelper`) whose handlers run sequentially. `ILifetimeService.Track` registers running work for the shutdown drain, and `SignalHostLifetime` turns SIGINT/SIGTERM into the `ShutdownRequested` event.

`PluginResolveService` scans `./Plugins/` and its **immediate subdirectories only**, not deeper descendants, ignoring folders/files prefixed with `__`. It discovers classes implementing `IPlugin<>`, keeps one plugin per assembly by convention, and logs each assembly's informational version. Plugins are sorted by ascending `Priority` before `Load` and `Setup`: **lower values run earlier**.

`Setup` creates the plugin, so pass services to the constructor or keep them in readonly fields instead of `null!` fields (fields that are only set later by Discord events may stay nullable). There is no parameterless-constructor requirement and no `Version` member. Plugins are not registered in the DI container, so do not resolve a plugin from it: share state through services. Do not depend on `Program`; inject `ILifetimeService` for shutdown and background-task tracking. Contribute web middleware through `IMvcConfigurationService` at the appropriate stage instead of adding a separate host. Because plugins load in priority order, when several plugins configure `DiscordClientServiceConfig` the last token written wins.

## The Command System

### Discovery
`CommandResolveService` uses reflection to scan all loaded assemblies for types that:
1. Inherit from `BasicCommandBase` (slash commands), `BasicUserCommandBase`, or `BasicMessageCommandBase`
2. Have `[AddToGlobal]` attribute

Each discovered type is registered as a keyed singleton in the DI container (keyed by its own `Type`). The framework handles command upload/update on the Discord client's `Ready` event. Keep per-interaction mutable state in callback locals, not command instance fields.

### The inheritance pattern

- `BasicCommandBase`: Framework slash-command contract; no automatic Core account checks or deferral.
  - `CommandBase`: Injects common services, supports localization, and runs `MasterConstraints` before the callback. The default master constraints are `RegisteredConstraint` then `TosConstraint`.
    - A command that needs less (for example, a guest command) overrides `MasterConstraints` with a shorter list, such as `[]`. There is no separate guest base class.
    - `AdminCommandBase`: Seals `MasterConstraints` to `AdminConstraint` (checks the configured admin ID) followed by any virtual `AdditionalConstraints`, so registration/TOS are not required and a derived command can add constraints but cannot drop the admin check. Defaults to `BotDm` context; derived commands can broaden `InteractionContextTypes` without removing the runtime admin check.

`BasicCommandBase` defines `Name`, `Description`, `CompleteBuilder`, and `Execute`. Core-based commands implement `PSLName`, `PSLDescription`, and `CompleteBuilder` instead of overriding `Name` or `Description`.

`CommandBase` seals `Name`, `Description`, **and `Execute()`**. `Execute()` resolves the subcommand path (before deferral, so an unmatched path throws), creates a short-lived database requester only to load the caller's `UserData` (null if unregistered), calls `DeferAsync()`, checks the master, group, and leaf constraints in that order, and then calls the callback. A failing constraint sends its own reply and stops the call.

A flat command overrides `RootCallback(SocketSlashCommand, UserData?)`. A command with subcommands overrides `CreateChildren()` to return `SubcommandNode` / `SubcommandGroupNode` instances (created once, on first use), and each leaf implements `Callback(SocketSlashCommand, SocketSlashCommandDataOption?, UserData?)`; `RootCallback` is not called for those. Nodes can add their own `Constraints`, and a leaf controls its deferral with `IsEphemeral` (default `true`, not inherited from the root or group). Nodes are long-lived singletons, so keep per-call state in locals. Create nodes with `new`; the order returned is the order Discord shows. A root with children writes `CompleteBuilder => this.BasicBuilder`, nodes are conventionally nested in the root class (partial files) and receive the root through their constructor, and a group holds only leaves. A node's `Constraints` run after the root's `MasterConstraints`; see `Template/SubcommandCommand.cs`.

Constraints (`IConstraint` and the classes in `PSLDiscordBot.Core/Command/Constraints/`) are DI singletons. `CommandBase` and `CommandNode` expose only the registered and TOS constraints as fields; other constraints (admin, alias) are resolved by the derived base or command that uses them, or passed to a node through its constructor. Custom constraints are created in a constructor or injected during plugin `Setup`. `AliasServerAdminCommandBase` (guild-install and guild context only) works the same way: it seals `MasterConstraints` as the alias-admin constraint, then registered, then TOS, followed by the virtual `AdditionalConstraints`. Do not bypass the registration, TOS, admin, visibility or command-specific consent checks to simplify a command. User data is nullable, so a command without the registered constraint must handle null.

`UserData` is `UserData?` in every callback; it is null when the user is not registered, so use `data!` only where a registered constraint guarantees a value. The callback receives no requester: a command that needs the database creates its own with `this._dataBaseService.NewRequester()`, and nothing is saved on dispose, so every write path must call `SaveChangesAsync` explicitly (for `UserData`, `AddOrReplaceUserDataAsync` then `SaveChangesAsync`). Create and dispose a requester per operation; never keep a `DbContext` on a singleton command or share one across concurrent work. Requesters are no-tracking (reads, including `FindAsync`, return untracked entities), so editing a loaded entity does nothing until you call the matching update method and save. `AddOrReplace*`, `SetOrReplaceMiscInfo` and `UpsertLocalSaveAsync` only stage changes (use one save for a multi-step write), while `RemoveLeaderboardEntryAsync` and `DeleteLocalSaveAsync` use `ExecuteDelete` and apply immediately. A forgotten `SaveChangesAsync` compiles and silently loses the write. Saves do not roll back on failure, so use a transaction where atomicity matters. The alias, statistics and report requesters have different save contracts and some helpers save internally, so check the actual requester first. `EnsureCreated` runs once in the `DataBaseService` constructor. Use the existing `QuickReply` / `QuickReplyWithAttachments` helpers to modify the deferred response rather than acknowledging the interaction again.

To add a command, start from the appropriate template in `PSLDiscordBot.Core/Command/Global/Template/` (normal, guest, admin, subcommand, or component), rename it, and uncomment `[AddToGlobal]`. Implement its builder and callback while preserving the selected constraints.

### Autocomplete

Autocomplete is dispatched by the Framework: `CommandResolveService` subscribes `AutocompleteExecuted`, raises the cancellable `BeforeAutocompleteExecutes` hook, finds the command by `Data.CommandName` with the same lookup as slash commands, and calls the virtual `BasicCommandBase.HandleAutocomplete(SocketAutocompleteInteraction)` on a thread pool thread (always; `RunOnDifferentThread` describes the slash command run, not autocomplete). The task is tracked with `ILifetimeService.Track` and failures raise `OnAutocompleteError`. The default handler sends nothing. An autocomplete interaction only accepts `RespondAsync(IEnumerable<AutocompleteResult>)` within 3 seconds (no defer, text reply or follow-up), so hooks cannot send messages. If a hook cancels, the command is unknown, or the handler throws, nothing is sent and the interaction expires. Handler failures, including Discord.Net's `TimeoutException` for a late reply, raise `OnAutocompleteError`.

Core's `CommandBase` seals `HandleAutocomplete`. It matches the leading group/leaf entries of the interaction options against node names, using the same name-and-type rule as the slash path (`TryMatchNode`; an unmatched path answers empty instead of throwing). It then builds an `AutocompleteContext` (focused option, the leaf's other options, and the language from the interaction locale) and calls the virtual `Autocomplete(context)` on the root (flat commands) or on the `SubcommandNode` leaf, which returns plain `AutocompleteSuggestion` values. Only the base applies Discord's limits when building the `AutocompleteResult`s: at most 25 suggestions, names cut to 100 characters, and string values over 100 characters, null values and empty names dropped. It logs the elapsed time at Debug, logs dropped or surplus suggestions and an unmatched path at Warning (name cuts are not logged), and responds exactly once, so a handler must never respond itself: `QuickReply`, deferral and follow-ups are invalid on an autocomplete interaction. Handlers must be in-memory only: no `DbContext`, no `UserData` read, no constraints, no deferral, and no per-call state on the node. An option opts in with `isAutocomplete: true` and must then be handled in `Autocomplete`. `PSLPlugin` reports `OnAutocompleteError` like slash and component errors (through `OnException`: logged at Error and sent to the admin); users get no error text. See the commands in `Template/`.
