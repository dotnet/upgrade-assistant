$dotnetRoot = Join-Path $RepoRoot '.dotnet'
$dotnetSdkVersion = $GlobalJson.tools.dotnet

try {
  
    dotnet workload install maui --verbosity diag --temp-dir $dotnetRoot
    dotnet workload repair 
    dotnet workload update --sdk-version $dotnetSdkVersion --no-cache --disable-parallel
    dotnet workload install --sdk-version $dotnetSdkVersion --no-cache --disable-parallel android-aot ios maui wasm-tools --skip-manifest-update --verbosity diag --temp-dir $dotnetRoot

}
catch {
  Write-Host $_.ScriptStackTrace
  Write-PipelineTelemetryError -Category 'RestoreToolset' -Message $_
  ExitWithExitCode 1
}

ExitWithExitCode 0