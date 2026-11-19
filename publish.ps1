param(
    [switch]$FrameworkDependent
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root "src\BtBattery.App\BtBattery.App.csproj"

$publishArgs = @(
    "publish", $project,
    "-c", "Release",
    "-r", "win-x64"
)

if ($FrameworkDependent) {
    $publishArgs += "--self-contained", "false"
} else {
    $publishArgs += @(
        "--self-contained", "true",
        "-p:PublishSingleFile=true",
        "-p:IncludeNativeLibrariesForSelfExtract=true"
    )
}

dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "发布完成。可执行文件在 src\BtBattery.App\bin\Release\ 下面的 win-x64\publish\蓝牙电量.exe"
