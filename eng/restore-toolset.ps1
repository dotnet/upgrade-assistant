$dotnetRoot = Join-Path $RepoRoot '.dotnet'

try {
    dotnet workload install maui --verbosity diag --temp-dir $dotnetRoot
}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'RestoreToolset' -Message $_
  ExitWithExitCode 1
}

ExitWithExitCode 0