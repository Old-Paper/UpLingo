param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = [IO.Path]::GetFullPath($PSScriptRoot)
$portableDotnet = Join-Path (Split-Path $root -Parent) "dotnet-sdk\dotnet.exe"
$dotnet = if (Test-Path -LiteralPath $portableDotnet) { $portableDotnet } else { "dotnet" }
[xml]$project = Get-Content -LiteralPath (Join-Path $root "Win11SubscriberWidget.csproj") -Raw -Encoding UTF8
$assemblyName = [string]($project.Project.PropertyGroup.AssemblyName | Select-Object -First 1)
if ([string]::IsNullOrWhiteSpace($assemblyName)) { throw "AssemblyName is missing." }

& $dotnet build (Join-Path $root "Win11SubscriberWidget.csproj") -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

$output = Join-Path $root ("bin\" + $Configuration + "\net48")
$exe = Join-Path $output ($assemblyName + ".exe")
$logic = Start-Process -FilePath $exe -ArgumentList "--logic-test" -PassThru -Wait -WindowStyle Hidden
if ($logic.ExitCode -ne 0) { throw "Logic test process failed." }
$report = Get-Content -LiteralPath (Join-Path $output "logic-test.log") -Raw -Encoding UTF8
if ($report.Trim() -ne "PASS") { throw "Logic test failed: $report" }

$self = Start-Process -FilePath $exe -ArgumentList "--self-test" -PassThru -Wait -WindowStyle Hidden
if ($self.ExitCode -ne 0) { throw "Startup self-test failed." }
Write-Output "PASS: build, logic test, startup self-test"
