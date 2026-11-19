param(
    [Parameter(Mandatory=$true)][string]$DiscRoot,
    [Parameter(Mandatory=$true)][string]$OutputDirectory,
    [string]$DnaDirectory,
    [switch]$Classics
)
$ErrorActionPreference = 'Stop'
$generator = Join-Path $DiscRoot 'UTILITY\FGW.EXE'
if ($Classics -and -not $DnaDirectory) { throw 'Specify the Classics DNA directory with -DnaDirectory.' }
if (-not $DnaDirectory) { $DnaDirectory = Join-Path $DiscRoot 'UTILITY\DNA' }
$fishFiles = if ($Classics) {
    @(@('Arowana.dna', 'Arowana.fsh'), @('Medaka.dna', 'Medaka.fsh'),
      @('M_Angel.dna', 'M_Angel.fsh'), @('Pleco.dna', 'Pleco.fsh'))
} else {
    @(@('TETRA.DNA', 'tetra.fsh'), @('CORYD.DNA', 'catfish.fsh'))
}
foreach ($item in $fishFiles) {
    $source = Join-Path $DnaDirectory $item[0]
    $target = Join-Path $OutputDirectory $item[1]
    if (Test-Path -LiteralPath $target) { throw "File already exists: $target" }
    if ($source.Contains('**') -or $target.Contains('**')) { throw 'The fish generator uses ** as a delimiter.' }
    $process = Start-Process -FilePath $generator -ArgumentList "$source**$target**20" -WorkingDirectory $OutputDirectory -PassThru
    try {
        if (-not $process.WaitForExit(15000)) {
            $process.Kill()
            $process.WaitForExit()
            throw "Fish generation timed out: $($item[0])"
        }
        if ($process.ExitCode -ne 1 -or -not (Test-Path -LiteralPath $target -PathType Leaf)) {
            throw "Fish generation failed: $($item[0])"
        }
    } finally {
        $process.Dispose()
    }
}
