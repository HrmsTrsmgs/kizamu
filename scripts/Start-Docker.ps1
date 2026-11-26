param(
    [string]$Distribution = 'Ubuntu'
)

$ErrorActionPreference = 'Stop'
$composeFile = Join-Path $PSScriptRoot '../compose.yaml'

if ($IsWindows) {
    $composeFile = wsl.exe --distribution $Distribution --exec wslpath -a $composeFile
    if ($LASTEXITCODE -ne 0) { throw 'Composeファイルのパスを変換できませんでした。' }
    wsl.exe --distribution $Distribution --user root --exec docker compose -f $composeFile up -d
} else {
    docker compose -f $composeFile up -d
}
if ($LASTEXITCODE -ne 0) { throw 'Dockerコンテナーの起動に失敗しました。' }
