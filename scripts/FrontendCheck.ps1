. (Join-Path $PSScriptRoot 'Wait-HealthEndpoint.ps1')

function Assert-CheckPortsAvailable {
    param([int]$FrontendPort)

    $ports = [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners().Port
    if ($ports -contains 5213 -or $ports -contains $FrontendPort) {
        throw "ポート5213または${FrontendPort}が使用中です。既存のサーバーを停止してから実行してください。"
    }
}

function Start-LoggedCheckServer {
    param(
        [string]$FilePath,
        [string]$ArgumentList,
        [string]$WorkingDirectory,
        [string]$LogDirectory,
        [string]$Name
    )

    $startOptions = @{
        FilePath = $FilePath
        ArgumentList = $ArgumentList
        WorkingDirectory = $WorkingDirectory
        RedirectStandardOutput = Join-Path $LogDirectory "$Name.out"
        RedirectStandardError = Join-Path $LogDirectory "$Name.err"
        WindowStyle = 'Hidden'
        PassThru = $true
    }
    Start-Process @startOptions
}

function Show-FrontendForManualCheck {
    param([string]$Uri)

    Write-Host "ブラウザーが開かない場合は $Uri を開いてください。"
    Write-Host '記録はメモリ上に保存され、スクリプトの終了時に失われます。'
    Start-Process -FilePath 'explorer.exe' -ArgumentList $Uri -WindowStyle Hidden
    Read-Host '画面と記録操作の確認を終えたらEnterを押してください' | Out-Null
}
