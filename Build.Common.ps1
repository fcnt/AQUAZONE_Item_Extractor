. (Join-Path $PSScriptRoot 'Native.ps1')
if (-not ('AquaBuildBytes' -as [type])) { Add-Type -Path (Join-Path $PSScriptRoot 'BuildBytes.cs') }
if ([AquaBuildBytes]::GetACP() -ne 932) {
    throw 'Use the Japanese system locale (CP932). Turn off the UTF-8 beta option in Windows regional settings.'
}

function New-Folder([string]$Path) {
    [void][IO.Directory]::CreateDirectory($Path)
    return $Path
}
function Invoke-Tool([string]$File, [string[]]$Arguments) {
    Invoke-AquaNative -File $File -Arguments $Arguments
}
function Expand-Disc([string]$Path, [string]$Destination) {
    if ([IO.Directory]::Exists($Path)) { return $Path }
    Invoke-Tool $SevenZip @('x', '-y', "-o$Destination", $Path)
    return $Destination
}
function Get-OneFile([string]$Folder, [string]$Filter) {
    $found = @(Get-ChildItem -LiteralPath $Folder -Filter $Filter -File -Recurse)
    if ($found.Count -ne 1) { throw "Expected one $Filter under $Folder" }
    return $found[0].FullName
}
function Copy-Directory([string]$Source, [string]$Destination) {
    [void][IO.Directory]::CreateDirectory($Destination)
    foreach ($item in (Get-ChildItem -LiteralPath $Source -Force)) {
        Copy-Item -LiteralPath $item.FullName -Destination $Destination -Recurse
    }
}
function Expand-CompactSetup([string]$Disc, [string]$Destination) {
    $archive = Join-Path $Disc 'INSTALL\DATA.1'
    if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ine '73786587cf8926b98c76a27f8121bf4ee16a24a0219c144ce48cfc0f9ab0fc49') {
        throw 'Unexpected Compact CD version'
    }
    [void](New-Folder $Destination)
    Invoke-Tool $unshieldv3 @('extract', $archive, $Destination)
    return $Destination
}
