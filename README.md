# Omukade Cheyenne
![logo](logo.png)

This is the server software for running private Pokemon TCG Live servers (codename Rainier). It provides complete support for all gameplay aspects (eg, cards, gamerules), and basic matchmaking and friend support, without relying on official Pokemon TCG Live servers.
(Clients may still require official servers for storing decks, maintaining friend lists, and fetching assets.)

## Requirements
* [.NET 6 Runtime or SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) for your platform
* [Rainier Card Definition Fetcher](https://github.com/Hastwell/Rainier.CardDefinitionFetcher)
* A set of current TCGL assemblies
* Supports Windows x64 and Linux x64 + ARM64
* Clients need to use a Connector (such as [Native Omukade Connector](https://github.com/Hastwell/Rainer.NativeOmukadeConnector))
to connect their game to an Omukade Cheyenne or other Omukade server.
* For developing:
    * Visual Studio 2022 (any edition) with C#
    * [Procedual Assembly Rewriter](https://github.com/Hastwell/Omukade.ProcedualAssemblyRewriter)

## Usage

### First-Time Setup
Before using this application, you must have a current copy of the Pokemon TCG Live gamerules and card data, retrieved by Rainier Card Definition Fetcher.
This data must be updated regularly, especially after any maintainance on the official servers. Either copy these to `PTCGL-CardDefinitions` under the server install directory, or set `carddata-directory` in config.json to the desired directory.

This application uses AutoPAR to load the TCGL assemblies. Before using this application, you must do one of the following:
* Windows Only, Recommended: Install Pokemon TCG Live. It will be auto-detected by AutoPAR and used for this application.
* Windows Only: Add the setting `autopar-search-folder` with the location of your TCGL install directory to config.json. Backslashes and quotes must be escaped (`\\` and `\"` respectively).
* Windows, Linux: Copy the TCGL assemblies (DLL files) from your TCGL install directory (`C:\Install\Folder\Pokémon Trading Card Game Live\Pokemon TCG Live_Data\Managed`) to the folder `autopar` under the server's directory.
  `autopar-search-folder` can be used to set any other name or location for this directory if prefered. *You must manually update this folder whenever the game updates!*

The default port the server uses is 10850. 10851 will be used in the future to provide WSS support.
### Configuration
#### config.json
* autopar-search-folder - The folder to search for the Pokemon TCG Live assemblies, usually the game's `Managed` directory or a copy thereof. **Required if** the Windows version of PTCGL is not installed on this host.
* carddata-directory - The folder containing all game rules downloaded by Rainier Card Definition Fetcher. Defaults to `PTCGL-CardDefinitions` under the server's directory.
* ws-port - The port to run the server on. Defaults to 10850.
* cardsource-overrides-enable - Enables experimental support for overriding card definitions with custom versions.
* cardsource-overrides-directory - The directory to search for card definition overrides. Defaults to `overrides`.
* discord-error-webhook-enable - Enables support for notifing server operators of errors via a Discord webhook. Default is `false`.
* discord-error-webhook-url - The URL to use for the Discord error webhook. **Required if** discord-error-webhook-enable is `true`.
* enable-reporting-all-implemented-cards - Whether to enable reporting all implemented expanded cards to clients. Default is `true`.

## Compiling

### Rainier Dependencies with AutoPAR
Before building this project, you'll need to run ManualPAR (part of the [Procedual Assembly Rewriter](https://github.com/Hastwell/Omukade.ProcedualAssemblyRewriter)) against the TCGL assemblies to produce a version
with public members that can be accessed by this tool.

These assemblies need to be located in:
```
[your sources folder]
|- Rainier-Assemblies
|  |- 1.3.11.156349.20221208_0543_PAR (or whatever version is latest)
|
|- Omukade.Cheyenne
|  |- Omukade.Cheyenne.sln
```

### Building
* Use Visual Studio 2022 or later, build the project.
* With the .NET 6 SDK, `dotnet build Omukade.Cheyenne.sln`

### Publishing
* With Visual Studio, publish the project using one of the included publish profiles.
* With the .NET 6 SDK, `dotnet publish Omukade.Cheyenne.sln -p:PublishProfile="Omukade.Cheyenne\Properties\PublishProfiles\[target platform].pubxml"`

The resulting binary can be pulled from `Omukade.Cheyenne\bin\Release\net6.0\publish\[target platform]\`
All included publish profiles are configured to create a single framework-dependent binary.

## License
This software is licensed under the terms of the [GNU AGPL v3.0](https://www.gnu.org/licenses/agpl-3.0.en.html)