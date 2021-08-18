$dotnetRoot = Join-Path $RepoRoot '.dotnet'

try {
  # dotnet tool install -g Redth.Net.Maui.Check
  # maui-check --ci --non-interactive --fix --skip androidsdk --skip xcode --skip vswin --skip vsmac --skip edgewebview2
  # dotnet workload install android-aot ios maccatalyst tizen maui tvos macos --skip-manifest-update --no-cache --disable-parallel --verbosity diag --temp-dir $dotnetRoot
  dotnet workload install maui --verbosity diag --temp-dir $dotnetRoot
  dotnet workload install android-aot --verbosity diag --temp-dir $dotnetRoot
  dotnet workload install ios --verbosity diag --temp-dir $dotnetRoot
  
}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'RestoreToolset' -Message $_
  ExitWithExitCode 1
}

ExitWithExitCode 0