$dotnetRoot = Join-Path $RepoRoot '.dotnet'

try {
    dotnet workload install microsoft-android-sdk-full microsoft-ios-sdk-full --verbosity diag --temp-dir $dotnetRoot
}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'RestoreToolset' -Message $_
  ExitWithExitCode 1
}

ExitWithExitCode 0