param($Configuration)

$projects = "LooseAssembly", "LooseAssembly.NuGet"
$cli = "$PSScriptRoot\artifacts\bin\Microsoft.DotNet.UpgradeAssistant.Cli\$Configuration\net6.0\Microsoft.DotNet.UpgradeAssistant.Cli.exe"
$packageDirectory = "$PSScriptRoot\artifacts\packages\$Configuration\Shipping"

pushd $packageDirectory

$env:UA_FEATURES="EXTENSION_MANAGEMENT"

foreach($project in $projects){
  $path = "$PSScriptRoot\artifacts\bin\$project\$Configuration\net5.0\publish"
  & $cli extensions create $path
}

rm upgrade-assistant.clef

popd