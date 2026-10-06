<#
  Builds the release files into .\release (or -OutDir):
    BrowserSelector-<version>-portable.7z       the app in a folder, no installer
    BrowserSelector-Setup-<version>.exe         per-user installer (Inno Setup 6)
  The version comes from <InformationalVersion> in Browser-Selector.csproj.
  Needs: .NET SDK, 7-Zip, Inno Setup 6 (winget install JRSoftware.InnoSetup).
#>
param([string]$OutDir = (Join-Path $PSScriptRoot "..\release"))
$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$csproj = Join-Path $root "Browser-Selector\Browser-Selector.csproj"

[xml]$proj = Get-Content $csproj
$version = ($proj.Project.PropertyGroup | Where-Object { $_.InformationalVersion } | Select-Object -First 1).InformationalVersion
$fileVersion = ($proj.Project.PropertyGroup | Where-Object { $_.Version } | Select-Object -First 1).Version + ".0"
if (-not $version) { throw "InformationalVersion not found in $csproj" }
Write-Host "Browser Selector $version ($fileVersion)"

$sevenZip = @("$env:ProgramFiles\7-Zip\7z.exe", "${env:ProgramFiles(x86)}\7-Zip\7z.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
$iscc = @("$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe", "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 6\ISCC.exe") | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $sevenZip) { throw "7-Zip not found" }
if (-not $iscc) { throw "Inno Setup 6 not found (winget install JRSoftware.InnoSetup)" }

dotnet build (Join-Path $root "Browser-Selector.sln") -c Release -nologo -v q
if ($LASTEXITCODE -ne 0) { throw "build failed" }
$exe = Join-Path $root "Browser-Selector\bin\Release\net48\BrowserSelector.exe"

New-Item -ItemType Directory -Force $OutDir | Out-Null
$OutDir = Resolve-Path $OutDir

# Portable: a folder with the exe, license and notes. Settings go to %APPDATA%\BrowserSelector like any copy.
$stage = Join-Path ([IO.Path]::GetTempPath()) ("bs-portable-" + [Guid]::NewGuid().ToString("N"))
$folder = Join-Path $stage "BrowserSelector"
New-Item -ItemType Directory -Force $folder | Out-Null
Copy-Item $exe $folder
Copy-Item (Join-Path $root "LICENSE") (Join-Path $folder "LICENSE.txt")
@"
Browser Selector $version (portable)
https://github.com/Mohammad-Diab/Browser-Selector

1. Put this folder where it can stay: Windows remembers where the exe is when you register.
2. Run BrowserSelector.exe, click Register, then choose Browser Selector in Windows Default apps.

Settings and site rules are kept in %APPDATA%\BrowserSelector, never in this folder.

To remove it: choose another default browser, click Unregister in the settings, then delete this
folder (and %APPDATA%\BrowserSelector if you don't want to keep your settings).
"@ | Set-Content (Join-Path $folder "README.txt")
$archive = Join-Path $OutDir "BrowserSelector-$version-portable.7z"
if (Test-Path $archive) { [IO.File]::Delete($archive) }
& $sevenZip a -t7z -m0=lzma2 -mx=9 -mfb=273 -md=256m -ms=on $archive $folder | Out-Null
if ($LASTEXITCODE -ne 0) { throw "7-Zip failed" }
[IO.Directory]::Delete($stage, $true)

& $iscc /Q "/DAppVersion=$version" "/DFileVersion=$fileVersion" "/DSourceExe=$exe" "/DOutputDir=$OutDir" (Join-Path $root "installer\BrowserSelector.iss")
if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed" }

Get-ChildItem $OutDir -File | Where-Object { $_.Name -like "BrowserSelector*" } | ForEach-Object {
    "{0,-48} {1,10:N0}  {2}" -f $_.Name, $_.Length, (Get-FileHash $_.FullName -Algorithm SHA256).Hash
}
