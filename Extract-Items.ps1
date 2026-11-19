[CmdletBinding()]
param(
    [string]$Compact,
    [string]$Classics,
    [string]$OutputDirectory,
    [string]$ToolsDirectory,
    [string]$SevenZip = (Join-Path $env:ProgramFiles '7-Zip\7z.exe')
)
$ErrorActionPreference = 'Stop'
if (-not $Compact -and -not $Classics) { throw 'Specify -Compact, -Classics, or both.' }
. (Join-Path $PSScriptRoot 'Build.Common.ps1')
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $PSScriptRoot 'dist-items' }
if (-not $ToolsDirectory) { $ToolsDirectory = Join-Path $PSScriptRoot '.tools' }
$OutputDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputDirectory)
$ToolsDirectory = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($ToolsDirectory)
$unshieldv3 = Join-Path $ToolsDirectory 'unshieldv3.exe'
$unshield = Join-Path $ToolsDirectory 'unshield\unshield.exe'
$requiredTools = @($SevenZip)
if ($Compact) {
    $Compact = (Get-Item -LiteralPath $Compact).FullName
    $requiredTools += $unshieldv3
}
if ($Classics) {
    $Classics = (Get-Item -LiteralPath $Classics).FullName
    $requiredTools += $unshield
}
foreach ($tool in $requiredTools) {
    if (-not [IO.File]::Exists($tool)) { throw "Missing $tool. Run Initialize-Tools.ps1 first." }
}
if (Test-Path -LiteralPath $OutputDirectory) { throw "Choose a new output directory: $OutputDirectory" }
if (-not ('AquaCompactItems' -as [type])) { Add-Type -Path (Join-Path $PSScriptRoot 'CompactItems.cs') }

function Extract-Compact([string]$Disc, [string]$Scratch, [string]$Pack) {
    $setup = Expand-CompactSetup $Disc (Join-Path $Scratch 'compact-setup')
    $tanks = New-Folder (Join-Path $Pack 'Tanks')
    $starter = Get-OneFile (Join-Path $setup 'ITEMS') '*.azn'
    Copy-Item -LiteralPath $starter -Destination (Join-Path $tanks 'Starter.azn')
    $kits = @(Get-ChildItem -LiteralPath (Join-Path $Disc 'SHOP') -Filter '*.AZN' -File -Recurse | Sort-Object FullName)
    if ($kits.Count -ne 10) { throw 'Expected ten Compact tank kits' }
    foreach ($tank in $kits) { Copy-Item -LiteralPath $tank.FullName -Destination $tanks }
    $fish = New-Folder (Join-Path $Pack 'Fish')
    & (Join-Path $PSScriptRoot 'generate-fish.ps1') -DiscRoot $Disc -OutputDirectory $fish
    foreach ($name in 'tetra.fsh', 'catfish.fsh') {
        $data = [IO.File]::ReadAllBytes((Join-Path $fish $name))
        if ($data.Length -lt 4096 -or [BitConverter]::ToUInt32($data, 20) -ne 1500) {
            throw "Incomplete generated fish file: $name"
        }
    }
    $inventory = @()
    foreach ($tank in (Get-ChildItem -LiteralPath $tanks -Filter '*.azn' -File | Sort-Object Name)) {
        $inventory += [AquaCompactItems]::Extract($tank.FullName, $Pack)
    }
    $inventory += [AquaCompactItems]::Extract((Join-Path $setup 'SYSTEM\AQUAZONE.REZ'), $Pack)
    $inventory | Export-Csv -LiteralPath (Join-Path $Pack 'Inventory.csv') -Encoding UTF8 -NoTypeInformation
}
function Extract-Classics([string]$Disc, [string]$Scratch, [string]$Pack, [string]$CompactDisc) {
    $archive = Get-OneFile $Disc 'data1.cab'
    $extracted = Join-Path $Scratch 'classics-data'
    # Preserve the original CP932 names with the Windows Japanese code page.
    Invoke-Tool $unshield @('-R', '-d', $extracted, 'x', $archive)
    $classicsData = $extracted
    $itemFolder = -join ([char[]](0xff71,0xff72,0xff83,0xff91,0xff8c,0xff67,0xff72,0xff99))
    $categories = @{
        '.plt'='Plants'; '.fd'='Food'; '.med'='Medicine'; '.acc'='Accessories\Classics'
        '.grv'='Gravel'; '.bmp'='Backgrounds'
    }
    foreach ($item in (Get-ChildItem -LiteralPath (Join-Path $extracted $itemFolder) -File)) {
        $category = $categories[$item.Extension.ToLowerInvariant()]
        if ($category) {
            $folder = New-Folder (Join-Path $Pack $category)
            Copy-Item -LiteralPath $item.FullName -Destination $folder
        }
    }
    $installer = Get-OneFile $Disc 'AZ1JP_ACCKIT01.exe'
    $cabinet = Join-Path $Scratch 'kit.cab'
    [AquaBuildBytes]::ExtractCabinet($installer, $cabinet)
    $kit = Join-Path $Scratch 'kit'
    Invoke-Tool $SevenZip @('x', '-y', "-o$kit", $cabinet)
    $archive = Get-OneFile $kit 'data1.cab'
    $extracted = Join-Path $Scratch 'kit-data'
    Invoke-Tool $unshield @('-R', '-d', $extracted, 'x', $archive)
    $items = @(Get-ChildItem -LiteralPath $extracted -Filter '*.acc' -File -Recurse)
    if ($items.Count -ne 30) { throw 'Expected thirty accessories in the Classics kit' }
    $folder = New-Folder (Join-Path $Pack 'Accessories\30-item-set')
    foreach ($item in $items) { Copy-Item -LiteralPath $item.FullName -Destination $folder }
    if ($CompactDisc) {
        $dna = Split-Path (Get-OneFile $classicsData 'Arowana.dna')
        $graphics = Get-OneFile $classicsData 'GP23000.rez'
        $generated = New-Folder (Join-Path $Scratch 'classics-generated-fish')
        & (Join-Path $PSScriptRoot 'generate-fish.ps1') -DiscRoot $CompactDisc -OutputDirectory $generated -DnaDirectory $dna -Classics
        $fish = New-Folder (Join-Path $Pack 'Fish')
        foreach ($name in 'Arowana', 'Medaka', 'M_Angel', 'Pleco') {
            $leaf = $name + '.fsh'
            [AquaCompactItems]::ConvertClassicsFish((Join-Path $generated $leaf), (Join-Path $fish $leaf), $graphics)
        }
    }

}
$parent = New-Folder (Split-Path $OutputDirectory)
$scratch = New-Folder (Join-Path $parent ('aquazone-build-' + [Guid]::NewGuid().ToString('N')))
try {
    $stage = New-Folder (Join-Path $scratch 'release')
    $compactDisc = $null
    if ($Compact) {
        $compactDisc = Expand-Disc $Compact (Join-Path $scratch 'compact-disc')
        Extract-Compact $compactDisc $scratch (New-Folder (Join-Path $stage 'Compact'))
    }
    if ($Classics) {
        $disc = Expand-Disc $Classics (Join-Path $scratch 'classics-disc')
        Extract-Classics $disc $scratch (New-Folder (Join-Path $stage 'Classics')) $compactDisc
    }
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'docs\Items.md') -Destination (Join-Path $stage 'README.md')
    if (Test-Path -LiteralPath $OutputDirectory) { throw "Output appeared during extraction: $OutputDirectory" }
    Move-Item -LiteralPath $stage -Destination $OutputDirectory
    Write-Host "Extracted local items: $OutputDirectory"
} finally {
    Remove-Item -LiteralPath $scratch -Recurse -Force
}
