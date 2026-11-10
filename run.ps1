[CmdletBinding()]
param(
    [switch]$BuildOnly,
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'win-mouse-macro\win-mouse-macro.csproj'
$executable = Join-Path $PSScriptRoot "win-mouse-macro\bin\$Configuration\net472\win-mouse-macro.exe"

if (-not $BuildOnly) {
    $existing = Get-Process -Name 'win-mouse-macro' -ErrorAction SilentlyContinue |
        Where-Object { $_.Path -eq $executable } |
        Select-Object -First 1
    if ($existing) {
        Write-Host 'Mouse macro is already running. Close its window before rebuilding changed code.'
        return
    }
}

$command = Get-Command dotnet.exe -ErrorAction SilentlyContinue
$dotnet = if ($command) { $command.Source } else { Join-Path $env:ProgramFiles 'dotnet\dotnet.exe' }
if (-not (Test-Path $dotnet)) {
    throw 'Install the Windows .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0, then run this task again.'
}

$buildArguments = @(
    'build',
    ('"{0}"' -f $project),
    '--configuration',
    $Configuration,
    '--nologo'
)

Write-Host "Building $Configuration..."
$build = Start-Process -FilePath $dotnet -ArgumentList $buildArguments -WorkingDirectory $PSScriptRoot -NoNewWindow -Wait -PassThru
if ($build.ExitCode -ne 0) {
    throw "Build failed (exit code $($build.ExitCode)). See the compiler output above."
}
if (-not (Test-Path $executable)) {
    throw "The build did not produce $executable."
}

if (-not $BuildOnly) {
    Start-Process -FilePath $executable -WorkingDirectory (Split-Path $executable)
    Write-Host 'Mouse macro launched in Windows.'
}
