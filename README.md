# Simple mouse macro for windows
![app pic](https://i.imgur.com/UUSORS7.png)
## Features
- record mouse clicks and drags
- record keyboard keys and shortcuts
- playback with human-like randomness (timing, click positions, cursor movement)
- reset
- loop
- delay

## Installation Instructions
- Download the `win-mouse-macro.zip` file from https://github.com/resetcat/win-mouse-macro/releases/
- Extract the contents to a desired location.
- Run `win-mouse-macro.exe` to start the application.

## Run the source locally in VS Code

On Windows, open this folder in VS Code, then press `Ctrl+Shift+P`, select
`Tasks: Run Task`, and choose **Run mouse macro**. The task builds the project
and opens the Windows desktop app. Use `Ctrl+Shift+B` to build without launching.
The tasks also work from WSL when Windows executable interop is enabled and the
project is on a Windows drive (for example, under `/mnt/c`).

You can also run this from a Windows PowerShell terminal in the project folder:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\run.ps1
```

Building requires the **Windows .NET 10 SDK** (already installed on this
machine). If needed, download it from https://dotnet.microsoft.com/download/dotnet/10.0.
The app still targets .NET Framework 4.7.2. The SDK restores its reference
assemblies through NuGet on the first build, so an internet connection is needed
initially. Full Visual Studio is not required.

Close the app before rebuilding changed code. The F5 (record), F6 (play/stop),
and F8 (reset) shortcuts work while the app window has keyboard focus.

## Publishing a new version

Releases are a plain zip of the Release build. There is no `setup` installer
anymore: building one requires Visual Studio, and the app runs fine without it.

Run this from a PowerShell terminal in the project folder:

```powershell
dotnet build win-mouse-macro\win-mouse-macro.csproj -c Release
Compress-Archive -Force -DestinationPath win-mouse-macro\bin\Release\win-mouse-macro.zip -Path win-mouse-macro\bin\Release\net472\win-mouse-macro.exe, win-mouse-macro\bin\Release\net472\win-mouse-macro.exe.config
```

Then upload `win-mouse-macro\bin\Release\win-mouse-macro.zip` as a new release at
https://github.com/resetcat/win-mouse-macro/releases/

## System Requirements
- Windows 10 or newer recommended.
- .NET Framework 4.7.2 installed.
