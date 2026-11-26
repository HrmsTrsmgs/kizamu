$ErrorActionPreference = 'Stop'

if (-not $IsWindows) {
    throw 'このテストはWindowsのPowerShell 7で実行してください。'
}

$scriptPath = Join-Path $PSScriptRoot '../../scripts/Start-Docker.ps1'
if (-not (Test-Path $scriptPath)) {
    throw 'Start-Docker.ps1 が存在しません。'
}

$calls = [System.Collections.Generic.List[object]]::new()
function wsl.exe {
    $calls.Add(@($args))
    $global:LASTEXITCODE = 0
    if ($args -contains 'wslpath') {
        '/mnt/c/kizamu/compose.yaml'
    }
}

& $scriptPath -Distribution 'Ubuntu-Test'

if ($calls.Count -ne 2) {
    throw "WSLの呼び出し回数が期待と異なります: $($calls.Count)"
}
if (($calls[0] -join ' ') -notmatch '^--distribution Ubuntu-Test --exec wslpath -a .+compose\.yaml$') {
    throw "Composeパスの変換呼び出しが期待と異なります: $($calls[0] -join ' ')"
}
if (($calls[1] -join ' ') -ne '--distribution Ubuntu-Test --user root --exec docker compose -f /mnt/c/kizamu/compose.yaml up -d') {
    throw "Compose起動呼び出しが期待と異なります: $($calls[1] -join ' ')"
}
