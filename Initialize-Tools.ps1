[CmdletBinding()]
param(
    [string]$ToolsDirectory,
    [string]$SevenZip = (Join-Path $env:ProgramFiles '7-Zip\7z.exe')
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Native.ps1')
if (-not $ToolsDirectory) { $ToolsDirectory = Join-Path $PSScriptRoot '.tools' }
$ToolsDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ToolsDirectory)
if (-not [IO.File]::Exists($SevenZip)) { throw 'Install Windows 7-Zip or specify -SevenZip with the path to 7z.exe.' }
[void][IO.Directory]::CreateDirectory($ToolsDirectory)
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Get-VerifiedFile([string]$Url, [string]$Name, [string]$Hash) {
    $path = Join-Path $ToolsDirectory $Name
    if (-not [IO.File]::Exists($path)) {
        Write-Host "Downloading $Name"
        $temporary = $path + '.partial'
        try {
            $oldProgress = $ProgressPreference
            $ProgressPreference = 'SilentlyContinue'
            Invoke-WebRequest -UseBasicParsing -Uri $Url -OutFile $temporary
            if ((Get-FileHash -LiteralPath $temporary -Algorithm SHA256).Hash -ine $Hash) {
                throw "SHA-256 mismatch: $Name"
            }
            Move-Item -LiteralPath $temporary -Destination $path
        } finally {
            $ProgressPreference = $oldProgress
            if (Test-Path -LiteralPath $temporary) { Remove-Item -LiteralPath $temporary -Force }
        }
    }
    if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ine $Hash) {
        throw "SHA-256 mismatch: $Name"
    }
    return $path
}
function Expand-Tool([string]$Archive, [string]$Directory) {
    Invoke-AquaNative -File $SevenZip -Arguments @('x', '-y', "-o$Directory", $Archive)
}
$zigArchive = Get-VerifiedFile 'https://ziglang.org/download/0.13.0/zig-windows-x86_64-0.13.0.zip' 'zig-windows-x86_64-0.13.0.zip' 'd859994725ef9402381e557c60bb57497215682e355204d754ee3df75ee3c158'
$zig = Join-Path $ToolsDirectory 'zig-windows-x86_64-0.13.0\zig.exe'
if (-not [IO.File]::Exists($zig)) { Expand-Tool $zigArchive $ToolsDirectory }
$sourceArchive = Get-VerifiedFile 'https://codeload.github.com/wfr/unshieldv3/zip/refs/tags/v0.2.2' 'unshieldv3-0.2.2.zip' '203b99fd0ec3eb15be7aebd97ca038273ad7d7a7b37009c3e4674fd7f9108928'
$source = Join-Path $ToolsDirectory 'unshieldv3-0.2.2'
if (-not [IO.Directory]::Exists($source)) { Expand-Tool $sourceArchive $ToolsDirectory }
$extractor = Join-Path $ToolsDirectory 'unshieldv3.exe'
if (-not [IO.File]::Exists($extractor)) {
    Write-Host 'Building native Windows unshieldv3'
    $config = @('#pragma once', '#define CMAKE_PROJECT_NAME "unshieldv3"', '#define CMAKE_PROJECT_VER "0.2.2"') -join [Environment]::NewLine
    [IO.File]::WriteAllText((Join-Path $source 'config.h'), $config, [Text.Encoding]::ASCII)
    Push-Location $source
    try {
        Invoke-AquaNative -File $zig -Arguments @('cc','-target','x86_64-windows-gnu','-O2','-c','blast.c','-o','blast.o')
        Invoke-AquaNative -File $zig -Arguments @('c++','-target','x86_64-windows-gnu','-std=c++17','-O2','main.cpp','ISArchiveV3.cpp','blast.o','-o',$extractor)
    } finally { Pop-Location }
}
$unshieldArchive = Get-VerifiedFile 'https://raw.githubusercontent.com/ScoopInstaller/Binary/master/unshield/unshield-1.6.2-x64.7z' 'unshield-1.6.2-x64.7z' '662fefefc6988a3744ddc6b5b8d2e96ac21876d532f7d23a597c4ca797764a6b'
$unshield = Join-Path $ToolsDirectory 'unshield\unshield.exe'
if (-not [IO.File]::Exists($unshield)) { Expand-Tool $unshieldArchive (Split-Path $unshield) }
$winevdm = Get-VerifiedFile 'https://github.com/otya128/winevdm/releases/download/v0.9.0/otvdm-v0.9.0.zip' 'otvdm-v0.9.0.zip' '842b11aed5fa81f3e1d4272e0ee7d37f1a5a8f936de825309dda672835e16fd4'
Write-Host "Windows build tools ready in $ToolsDirectory"
