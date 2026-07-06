# Builds the mod assembly. Requires the .NET SDK (for `dotnet build`) or MSBuild
# (bundled with Visual Studio/Rider) to be available on PATH.
#
# Note: this repo's Source\HospitalityRoomService.csproj targets net472 with modern
# C# syntax (file-scoped namespaces, pattern matching, etc.), which requires an
# SDK-aware compiler. The legacy csc.exe that ships with Windows (v4.0.30319) only
# supports up to C# 5 and cannot build this project - if that's all you have, install
# the .NET SDK (https://dotnet.microsoft.com/download) or open Source\HospitalityRoomService.csproj
# in Visual Studio / Rider instead.

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$csproj = Join-Path $root 'Source\HospitalityRoomService.csproj'

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
if ($dotnet) {
    & dotnet build $csproj -c Release
    exit $LASTEXITCODE
}

$msbuild = Get-Command msbuild -ErrorAction SilentlyContinue
if ($msbuild) {
    & msbuild $csproj /p:Configuration=Release
    exit $LASTEXITCODE
}

Write-Error "Neither 'dotnet' nor 'msbuild' was found on PATH. Install the .NET SDK, or open $csproj in Visual Studio/Rider and build from there."
exit 1
