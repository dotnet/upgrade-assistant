$mauiSource = https://aka.ms/dotnet/maui/rc1/index.json
# $dotnetSdkVersion = $GlobalJson.tools.dotnet

try {
  dotnet workload install ios --source $mauiSource
  dotnet workload install android-aot --source $mauiSource
  dotnet workload install android --source $mauiSource
  dotnet workload install maccatalyst --source $mauiSource
  dotnet workload install maui --source $mauiSource

  dotnet workload list

}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'RestoreToolset' -Message $_
  ExitWithExitCode 1
}

ExitWithExitCode 0