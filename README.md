# ReClass.NET LoadBinary Plugin
A plugin which allows ReClass.NET to load files from disk and use their contents.

## Installation
- Compile or download from https://github.com/KN4CK3R/ReClass.NET-LoadBinaryPlugin
- Copy the dll files in the appropriate Plugin folder (ReClass.NET/x86/Plugins or ReClass.NET/x64/Plugins)
- Start ReClass.NET and check the plugins form if the LoadBinary plugin is listed. Open the "Native" tab and switch all available methods to the LoadBinary plugin.
- The process selection will ask for a file to load.

## Compiling
If you want to compile the ReClass.NET LoadBinary Plugin just fork the repository and create the following folder structure. If you don't use this structure you need to fix the project references.

```
..\ReClass.NET\
..\ReClass.NET\ReClass.NET\ReClass.NET.csproj
..\ReClass.NET-LoadBinaryPlugin
..\ReClass.NET-LoadBinaryPlugin\LoadBinaryPlugin.csproj
```