function Invoke-AquaNative {
    param(
        [Parameter(Mandatory=$true)][string]$File,
        [string[]]$Arguments
    )
    $quoted = foreach ($argument in $Arguments) {
        # Apply the Windows CommandLineToArgvW backslash/quote rules.
        '"' + (($argument -replace '(\\*)"', '$1$1\"') -replace '(\\+)$', '$1$1') + '"'
    }
    $info = New-Object Diagnostics.ProcessStartInfo
    $info.FileName = $File
    $info.Arguments = $quoted -join ' '
    $info.WorkingDirectory = (Get-Location).ProviderPath
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    $info.RedirectStandardOutput = $true
    $info.RedirectStandardError = $true
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $info
    try {
        [void]$process.Start()
        $output = $process.StandardOutput.ReadToEndAsync()
        $errors = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        $stdout = $output.GetAwaiter().GetResult()
        $stderr = $errors.GetAwaiter().GetResult()
        if ($stdout) { Write-Host $stdout }
        if ($stderr) { Write-Host $stderr }
        if ($process.ExitCode -ne 0) {
            throw "Tool exited with code $($process.ExitCode): $File"
        }
    } finally { $process.Dispose() }
}
