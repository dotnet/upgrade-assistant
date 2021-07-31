try {
    $dotnetSDKVersion = $GlobalJson.tools.dotnet
    dotnet --version
    Write-Host 'dotnetRoot check' Join-Path $dotnetRoot "sdk\$dotnetSDKVersion"
    dotnet --sdk-version $dotnetSDKVersion workload install microsoft-android-sdk-full microsoft-ios-sdk-full
}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'RestoreToolset' -Message $_
  ExitWithExitCode 1
}

ExitWithExitCode 0