$dotnetRoot = Join-Path $RepoRoot '.dotnet'

try {
  dotnet workload install maui --verbosity diag --temp-dir $dotnetRoot

  dotnet tool install -g Redth.Net.Maui.Check
  maui-check --main --force-dotnet --non-interactive --fix --skip androidsdk --skip xcode --skip vswin --skip vsmac --skip edgewebview2

  maui-check --main --force-dotnet --non-interactive --fix --skip androidsdk --skip xcode --skip vswin --skip vsmac --skip edgewebview2
}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'RestoreToolset' -Message $_
  ExitWithExitCode 1
}

ExitWithExitCode 0