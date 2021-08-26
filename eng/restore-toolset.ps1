# $dotnetRoot = Join-Path $RepoRoot '.dotnet'
# $dotnetSdkVersion = $GlobalJson.tools.dotnet

try {
  dotnet workload install maui --skip-manifest-update --verbosity diag
  dotnet workload install maui-core --skip-manifest-update --verbosity diag
  dotnet workload install maui-android --skip-manifest-update --verbosity diag
  dotnet workload install maui-desktop --skip-manifest-update --verbosity diag
  dotnet workload install maui-ios --skip-manifest-update --verbosity diag
  dotnet workload install maui-mobile --skip-manifest-update --verbosity diag
  dotnet workload install microsoft-android-sdk-full --skip-manifest-update --verbosity diag
  dotnet workload install microsoft-ios-sdk-full --skip-manifest-update --verbosity diag

  # dotnet tool install -g Redth.Net.Maui.Check
  # maui-check --main --ci --non-interactive --skip androidsdk --skip xcode --skip vswin --skip vsmac --skip edgewebview2

  # dotnet workload install ios --source "https://aka.ms/dotnet/maui/main/index.json"
  # dotnet workload install macos --source "https://aka.ms/dotnet/maui/main/index.json"
  # dotnet workload install maccatalyst --source "https://aka.ms/dotnet/maui/main/index.json"
  # dotnet workload install android-aot --source "https://aka.ms/dotnet/maui/main/index.json"
  # dotnet workload install maui --source "https://aka.ms/dotnet/maui/main/index.json"
  # dotnet --version
  # dotnet --info

  # dotnet workload install maui --disable-parallel --verbosity diag --temp-dir $dotnetRoot
  # dotnet workload install android --disable-parallel --verbosity diag --temp-dir $dotnetRoot
  # dotnet workload install ios --disable-parallel --verbosity diag --temp-dir $dotnetRoot

  # dotnet tool install -g Redth.Net.Maui.Check
  # maui-check --main --force-dotnet --non-interactive --fix --skip androidsdk --skip xcode --skip vswin --skip vsmac --skip edgewebview2

  # maui-check --main --ci --non-interactive --fix --skip androidsdk --skip xcode --skip vswin --skip vsmac --skip edgewebview2
  # dotnet workload install maui --source "https://aka.ms/dotnet/maui/main/index.json"

  # dotnet workload install maui --sdk-version $dotnetSdkVersion --disable-parallel --verbosity diag --temp-dir $dotnetRoot
  # dotnet workload repair --sdk-version $dotnetSdkVersion
  # dotnet workload update --sdk-version $dotnetSdkVersion
  # dotnet workload install android-aot --sdk-version $dotnetSdkVersion --disable-parallel --verbosity diag --temp-dir $dotnetRoot
  # dotnet workload install ios --sdk-version $dotnetSdkVersion --disable-parallel --verbosity diag --temp-dir $dotnetRoot

  # trying peppers 
  # dotnet workload update --from-rollback-file workload.json --verbosity diag
  # dotnet workload install maui --skip-manifest-update --verbosity diag
  # dotnet workload install maui --source "https://aka.ms/dotnet/maui/main/index.json" --skip-manifest-update --verbosity diag
}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'RestoreToolset' -Message $_
  ExitWithExitCode 1
}

ExitWithExitCode 0