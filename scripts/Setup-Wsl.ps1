param(
    [string]$Distribution = 'Ubuntu'
)

$ErrorActionPreference = 'Stop'
if (-not $IsWindows) { throw 'このスクリプトはWindowsのPowerShell 7で実行してください。' }
if (-not (Get-Command wsl.exe -ErrorAction SilentlyContinue)) {
    throw 'WSLが見つかりません。WindowsでWSLをインストールしてから再実行してください。'
}

$distributions = @(wsl.exe --list --quiet | ForEach-Object { $_.Replace("`0", '').Trim() })
if ($LASTEXITCODE -ne 0) { throw 'WSLのディストリビューションを確認できませんでした。' }
if ($distributions -notcontains $Distribution) {
    Write-Host "$Distribution をインストールします。初期設定後にこのスクリプトを再実行してください。"
    wsl.exe --install --distribution $Distribution
    if ($LASTEXITCODE -ne 0) { throw 'WSLディストリビューションのインストールに失敗しました。' }
    return
}

$setupFile = Join-Path $PSScriptRoot 'setup-wsl-ubuntu.sh'
$linuxPath = wsl.exe --distribution $Distribution --exec wslpath -a $setupFile
if ($LASTEXITCODE -ne 0) { throw 'セットアップスクリプトのパスを変換できませんでした。' }
wsl.exe --distribution $Distribution --user root --exec bash $linuxPath
if ($LASTEXITCODE -ne 0) { throw 'WSLのセットアップに失敗しました。' }
