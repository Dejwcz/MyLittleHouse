$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$cliHome = Join-Path $root ".dotnet"
$nugetRoot = Join-Path $root ".nuget"

$paths = @(
    $cliHome,
    $nugetRoot,
    (Join-Path $nugetRoot "packages"),
    (Join-Path $nugetRoot "http-cache"),
    (Join-Path $nugetRoot "plugins-cache"),
    (Join-Path $nugetRoot "temp")
)

foreach ($path in $paths) {
    New-Item -ItemType Directory -Force -Path $path | Out-Null
}

Remove-Item Env:\HTTP_PROXY -ErrorAction SilentlyContinue
Remove-Item Env:\HTTPS_PROXY -ErrorAction SilentlyContinue
Remove-Item Env:\ALL_PROXY -ErrorAction SilentlyContinue

$env:DOTNET_CLI_HOME = $cliHome
$env:MSBuildDisableWorkloadResolver = "1"
$env:MSBuildEnableWorkloadResolver = "false"
$env:DOTNET_NO_WORKLOADS = "1"
$env:NUGET_PACKAGES = Join-Path $nugetRoot "packages"
$env:NUGET_HTTP_CACHE_PATH = Join-Path $nugetRoot "http-cache"
$env:NUGET_PLUGINS_CACHE_PATH = Join-Path $nugetRoot "plugins-cache"
$env:NUGET_TEMP_PATH = Join-Path $nugetRoot "temp"

$solution = Join-Path $root "src\api\MujDomecek.sln"

dotnet restore $solution
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

dotnet build $solution --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
