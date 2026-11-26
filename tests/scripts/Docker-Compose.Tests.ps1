$ErrorActionPreference = 'Stop'

if (-not $IsWindows) {
    throw 'このテストはWindowsのPowerShell 7で実行してください。'
}

$calls = [System.Collections.Generic.List[object]]::new()
function wsl.exe {
    $calls.Add(@($args))
    $global:LASTEXITCODE = 0
    if ($args -contains 'wslpath') {
        '/mnt/c/kizamu/compose.yaml'
    }
}

foreach ($case in @(
    @{ Script = 'Start-Docker.ps1'; Command = 'up -d' },
    @{ Script = 'Build-Docker.ps1'; Command = 'build' },
    @{ Script = 'Stop-Docker.ps1'; Command = 'down' }
)) {
    $scriptPath = Join-Path $PSScriptRoot "../../scripts/$($case.Script)"
    if (-not (Test-Path $scriptPath)) {
        throw "$($case.Script) が存在しません。"
    }
    $calls.Clear()
    & $scriptPath -Distribution 'Ubuntu-Test'

    if ($calls.Count -ne 2) {
        throw "WSLの呼び出し回数が期待と異なります: $($calls.Count)"
    }
    if (($calls[0] -join ' ') -notmatch '^--distribution Ubuntu-Test --exec wslpath -a .+compose\.yaml$') {
        throw "Composeパスの変換呼び出しが期待と異なります: $($calls[0] -join ' ')"
    }
    $expected = "--distribution Ubuntu-Test --user root --exec docker compose -f /mnt/c/kizamu/compose.yaml $($case.Command)"
    if (($calls[1] -join ' ') -ne $expected) {
        throw "Compose呼び出しが期待と異なります: $($calls[1] -join ' ')"
    }
}
