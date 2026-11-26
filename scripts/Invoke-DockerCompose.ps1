param(
    [string]$Distribution,
    [string[]]$ComposeCommand,
    [string]$FailureMessage
)

$ErrorActionPreference = 'Stop'
$composeFile = Join-Path $PSScriptRoot '../compose.yaml'

if ($IsWindows) {
    $composeFile = wsl.exe --distribution $Distribution --exec wslpath -a $composeFile
    if ($LASTEXITCODE -ne 0) { throw 'Composeファイルのパスを変換できませんでした。' }
    wsl.exe --distribution $Distribution --user root --exec docker compose -f $composeFile @ComposeCommand
} else {
    docker compose -f $composeFile @ComposeCommand
}
if ($LASTEXITCODE -ne 0) { throw $FailureMessage }
