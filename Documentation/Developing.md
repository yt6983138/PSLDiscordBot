# Getting Started (For Developers)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/yt6983138/PSLDiscordBot)

## Prerequisites
- .net 8.0 SDK
- A Chromium-based browser (for image generation)
- Saira font family installed
- [External game asset repo](https://github.com/yt6983138/PSLDiscordBot.Resources) (also setting environment variable `ASSET_COPY_PATH`)

## Solution Layout
The project is laid like this:
1. `PSLDiscordBot.Framework`: Host, entry point, Discord gateway, DI container, plugin loading, command dispatch
2. `PSLDiscordBot.Core`: Plugin, all business logic, commands, services etc
3. `PSLDiscordBot.Analyzer`: Analyzer and source generator for various things
4. `CoreRunHelper`: Launcher that properly set up the `Core` for debugging
5. `AssemblyFixer`: A plugin that resolves runtime assembly conflicts for `System.Text.Json` etc. This will be removed upon upgrading to .net 10.

The `Framework` never directly references any other project. Instead, it discovers plugin at runtime by scanning the `./Plugins/` directory for DLLs.

## The Plugin System
Every plugin should implement `IPlugin`, and it has following life cycle:
1. `IPlugin.Load()`: Register services into the DI container (`WebApplicationBuilder` phase)
2. `IPlugin.ConfigureDiscordClient()`: Set bot token, gateway intents
3. `builder.Build()`: Framework builds the `WebApplication`
4. `Program.AfterPluginsLoaded`: Hook for executing some pre-setup or post-build codes
5. `IPlugin.Setup()`: Resolve services, hook events (`WebApplication` phase)
6. `Program.AfterArgParse`: Hook for post-argument parsing
7. `Program.AfterMainInitialize`: Hook right before `App.RunAsync`, the last hook before bot is live
8. `App.RunAsync()`: Bot is live
9. `Program.BeforeMainExiting`: Pre-unload hook
10. `IPlugin.Unload()`: Graceful shutdown

`PluginResolveService` scans `./Plugins/` and its subdirectories for assemblies, ignoring any folder or file prefixed with `__`. It instantiates all classes implementing `IPlugin` and sorts them by `Priority` before calling each lifecycle method.

## The Command System

### Discovery
`CommandResolveService` uses reflection to scan all loaded assemblies for types that:
1. Inherit from `BasicCommandBase` (slash commands), `BasicUserCommandBase`, or `BasicMessageCommandBase`
2. Have `[AddToGlobal]` attribute

Each discovered type is registered as a keyed singleton in the DI container (keyed by its own `Type`). The framework handles initial command upload/update for you.

### The inheritance pattern

- `BasicCommandBase`: Most basic command base
  - `CommandBase`: Injects common services, checks registration + TOS, also supports localization
    - `GuestCommandBase`: Skips registration check, works for unregistered users
    - `AdminCommandBase`: Only admin can execute, and must be in bot dm

`BasicCommandBase` defines the contract every command must fulfill:

`CommandBase` seals `Execute()` and instead exposes `Callback()`, you would typically implement your logic inside callback. Before calling `Callback()`, `Execute()` automatically:
- Calls `DeferAsync()` to acknowledge the interaction
- Does checks mentioned above

If you wants to create a new command, the best way is just copy template from `Command/Global/Template` directory and uncomment `[AddToGlobal]` line.
