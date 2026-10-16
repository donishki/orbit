param([ValidateSet("x64", "x86")][string]$Architecture = "x64", [string]$IsccPath = "${env:ProgramFiles(x86)}\Inno Setup 7\ISCC.exe")
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
if (!(Test-Path -LiteralPath $IsccPath)) { throw "Inno Setup compiler not found. Supply -IsccPath with the path to ISCC.exe." }
Push-Location $projectRoot
try {
    dotnet publish Orbit.csproj -c Release -r "win-$Architecture" --self-contained true -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false -o "release/installer-payload-$Architecture"
    if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
    Copy-Item README.txt "release/installer-payload-$Architecture/README.txt"
    & $IsccPath "/DArch=$Architecture" installer/Orbit.iss
    if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
} finally { Pop-Location }
