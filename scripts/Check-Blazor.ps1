$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Wait-HealthEndpoint.ps1')
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$api = Join-Path $root 'src/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api'
$frontend = Join-Path $root 'src/frontends/blazor/Kizamu.Frontend.Blazor.Server'
$ports = [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners().Port
if (($ports -contains 5213) -or ($ports -contains 5214)) {
    throw 'ポート5213または5214が使用中です。既存のサーバーを停止してから実行してください。'
}

foreach ($project in @($api, $frontend)) {
    & dotnet build $project --nologo
    if ($LASTEXITCODE -ne 0) { throw "ビルドに失敗しました: $project" }
}

$processes = @()
$logs = Join-Path ([System.IO.Path]::GetTempPath()) ("kizamu-check-" + [guid]::NewGuid())
New-Item -ItemType Directory -Path $logs | Out-Null
try {
    foreach ($item in @(
        @{ Dir = $api; Dll = 'Kizamu.Backend.AspNetCore.Api.dll'; Port = 5213; Check = 'http://localhost:5213/api/health'; Name = 'api' },
        @{ Dir = $frontend; Dll = 'Kizamu.Frontend.Blazor.Server.dll'; Port = 5214; Check = 'http://localhost:5214/api/health'; Name = 'frontend' }
    )) {
        $dll = Join-Path $item.Dir ("bin/Debug/net10.0/" + $item.Dll)
        $process = Start-Process -FilePath 'dotnet' -ArgumentList "`"$dll`" --urls http://localhost:$($item.Port) --environment Development" -WorkingDirectory $item.Dir -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $logs "$($item.Name).out") -RedirectStandardError (Join-Path $logs "$($item.Name).err")
        $processes += $process
        $ready = Wait-HealthEndpoint -Process $process -Uri $item.Check
        if (-not $ready) {
            Get-Content (Join-Path $logs "$($item.Name).out"), (Join-Path $logs "$($item.Name).err") -ErrorAction SilentlyContinue | Write-Host
            throw "$($item.Name) を起動できませんでした。"
        }
    }
    Write-Host 'ブラウザーが開かない場合は http://localhost:5214 を開いてください。'
    Write-Host '記録はメモリ上に保存され、スクリプトの終了時に失われます。'
    Start-Process -FilePath 'explorer.exe' -ArgumentList 'http://localhost:5214' -WindowStyle Hidden
    Read-Host '画面と記録操作の確認を終えたらEnterを押してください' | Out-Null
} finally {
    foreach ($process in $processes) {
        if (-not $process.HasExited) { $process.Kill(); $process.WaitForExit() }
        $process.Dispose()
    }
    Remove-Item -LiteralPath $logs -Recurse -Force
}
