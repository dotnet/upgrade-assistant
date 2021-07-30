try {
    dotnet workload install microsoft-android-sdk-full microsoft-ios-sdk-full --verbosity diag
}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'RestoreToolset' -Message $_
  ExitWithExitCode 1
}

ExitWithExitCode 0