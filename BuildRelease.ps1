param(
    [string]$DeployPath = ""
)

$ErrorActionPreference = "Stop"
$workspace = [IO.Path]::GetFullPath($PSScriptRoot)
$source = Join-Path $workspace "work\decompiled-latest"
$assets = Join-Path $workspace "work\UpLingo-distributable-assets"
$outputs = Join-Path $workspace "outputs"
$portableDotnet = Join-Path $workspace "work\dotnet-sdk\dotnet.exe"
$dotnet = if (Test-Path -LiteralPath $portableDotnet) { $portableDotnet } else { "dotnet" }
[xml]$project = Get-Content -LiteralPath (Join-Path $source "Win11SubscriberWidget.csproj") -Raw -Encoding UTF8
$assemblyName = [string]($project.Project.PropertyGroup.AssemblyName | Select-Object -First 1)
if ([string]::IsNullOrWhiteSpace($assemblyName) -or $assemblyName -notmatch '^UpLingo-(.+)$') { throw "Invalid AssemblyName: $assemblyName" }
$version = $Matches[1]
$exeName = $assemblyName + ".exe"

& $dotnet build (Join-Path $source "Win11SubscriberWidget.csproj") -c Release --no-restore
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

$exeDir = Join-Path $source "bin\Release\net48"
$testExe = Join-Path $exeDir $exeName
$test = Start-Process -FilePath $testExe -ArgumentList "--logic-test" -PassThru -Wait -WindowStyle Hidden
if ($test.ExitCode -ne 0) { throw "Logic test process failed." }
$logicLog = Get-Content -LiteralPath (Join-Path $exeDir "logic-test.log") -Raw -Encoding UTF8
if ($logicLog.Trim() -ne "PASS") { throw "Logic test failed: $logicLog" }
$selfTest = Start-Process -FilePath $testExe -ArgumentList "--self-test" -PassThru -Wait -WindowStyle Hidden
if ($selfTest.ExitCode -ne 0) { throw "Startup self-test failed." }

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$staging = Join-Path (Join-Path $workspace "work") ("release-staging-" + $stamp)
New-Item -ItemType Directory -Path $staging | Out-Null

$assetNames = @(
    "check_config.bat",
    "config.example.json",
    "install_startup.bat",
    "motivational_slogans.txt",
    "README.txt",
    "run_widget.bat",
    "uninstall_startup.bat"
)
foreach ($name in $assetNames) {
    Copy-Item -LiteralPath (Join-Path $assets $name) -Destination $staging
}
Copy-Item -LiteralPath $testExe -Destination $staging
Copy-Item -LiteralPath (Join-Path $exeDir ($exeName + ".config")) -Destination $staging

$releaseSource = Join-Path $staging "src"
New-Item -ItemType Directory -Path $releaseSource | Out-Null
Copy-Item -LiteralPath (Join-Path $source "Win11SubscriberWidget.csproj") -Destination $releaseSource
Copy-Item -LiteralPath (Join-Path $source "app.config") -Destination $releaseSource
Copy-Item -LiteralPath (Join-Path $source "app.manifest") -Destination $releaseSource
Copy-Item -LiteralPath (Join-Path $source "AGENTS.md") -Destination $releaseSource
Copy-Item -LiteralPath (Join-Path $source "MAINTENANCE.md") -Destination $releaseSource
Copy-Item -LiteralPath (Join-Path $source "RunChecks.ps1") -Destination $releaseSource
Copy-Item -LiteralPath (Join-Path $source "Properties") -Destination $releaseSource -Recurse
Copy-Item -LiteralPath (Join-Path $source "Win11SubscriberWidget") -Destination $releaseSource -Recurse

New-Item -ItemType Directory -Path $outputs -Force | Out-Null
$zip = Join-Path $outputs ("UpLingo-" + $version + "-distributable-" + $stamp + ".zip")
Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $zip -CompressionLevel Optimal

if ($DeployPath) {
    $deploy = [IO.Path]::GetFullPath($DeployPath)
    if (-not (Test-Path -LiteralPath $deploy)) { throw "Deploy path does not exist: $deploy" }
    Copy-Item -LiteralPath $testExe -Destination (Join-Path $deploy $exeName) -Force
    Copy-Item -LiteralPath (Join-Path $exeDir ($exeName + ".config")) -Destination (Join-Path $deploy ($exeName + ".config")) -Force
    $deploySource = Join-Path $deploy "src"
    if (Test-Path -LiteralPath $deploySource) { Remove-Item -LiteralPath $deploySource -Recurse -Force }
    Copy-Item -LiteralPath $releaseSource -Destination $deploy -Recurse
    foreach ($name in $assetNames) {
		if ($name -eq "motivational_slogans.txt") { continue }
        Copy-Item -LiteralPath (Join-Path $assets $name) -Destination (Join-Path $deploy $name) -Force
    }
	$legacyExe = Join-Path $deploy "Win11SubscriberWidget.exe"
	$legacyConfig = Join-Path $deploy "Win11SubscriberWidget.exe.config"
	if (Test-Path -LiteralPath $legacyExe) { Remove-Item -LiteralPath $legacyExe -Force }
	if (Test-Path -LiteralPath $legacyConfig) { Remove-Item -LiteralPath $legacyConfig -Force }
	Get-ChildItem -LiteralPath $deploy -File -Filter "UpLingo-*.exe" | Where-Object { $_.Name -ne $exeName } | Remove-Item -Force
	Get-ChildItem -LiteralPath $deploy -File -Filter "UpLingo-*.exe.config" | Where-Object { $_.Name -ne ($exeName + ".config") } | Remove-Item -Force
}

$workPrefix = [IO.Path]::GetFullPath((Join-Path $workspace "work")) + [IO.Path]::DirectorySeparatorChar
$resolvedStaging = [IO.Path]::GetFullPath($staging)
if (-not $resolvedStaging.StartsWith($workPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Unsafe staging path: $resolvedStaging"
}
Remove-Item -LiteralPath $resolvedStaging -Recurse -Force
Write-Output $zip
