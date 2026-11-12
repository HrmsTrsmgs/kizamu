$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Wait-HealthEndpoint.ps1')
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$api = Join-Path $root 'src/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api'
$frontend = Join-Path $root 'src/frontends/react'
$ports = [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners().Port
if (($ports -contains 5213) -or ($ports -contains 5173)) {
    throw 'ポート5213または5173が使用中です。既存のサーバーを停止してから実行してください。'
}

$vite = Join-Path $root 'node_modules/vite/bin/vite.js'
$tsc = Join-Path $root 'node_modules/typescript/bin/tsc'
if (-not (Test-Path -LiteralPath $vite) -or -not (Test-Path -LiteralPath $tsc)) {
    throw 'npm依存関係が見つかりません。リポジトリルートで npm ci を実行してください。'
}

& dotnet build $api --nologo
if ($LASTEXITCODE -ne 0) { throw '業務APIのビルドに失敗しました。' }
& node $tsc --project (Join-Path $root 'src/domains/typescript/tsconfig.json')
if ($LASTEXITCODE -ne 0) { throw '共有Domainのビルドに失敗しました。' }

$processes = @()
$logs = Join-Path ([System.IO.Path]::GetTempPath()) ("kizamu-react-check-" + [guid]::NewGuid())
New-Item -ItemType Directory -Path $logs | Out-Null
try {
    $dll = Join-Path $api 'bin/Debug/net10.0/Kizamu.Backend.AspNetCore.Api.dll'
    foreach ($item in @(
        @{ File = 'dotnet'; Args = "`"$dll`" --urls http://localhost:5213 --environment Development"; Dir = $api; Check = 'http://localhost:5213/api/health'; Name = 'API' },
        @{ File = 'node'; Args = "`"$vite`" --strictPort"; Dir = $frontend; Check = 'http://localhost:5173/api/health'; Name = 'React' }
    )) {
        $process = Start-Process -FilePath $item.File -ArgumentList $item.Args -WorkingDirectory $item.Dir -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $logs "$($item.Name).out") -RedirectStandardError (Join-Path $logs "$($item.Name).err")
        $processes += $process
        $ready = Wait-HealthEndpoint -Process $process -Uri $item.Check
        if (-not $ready) { throw "$($item.Name) を起動できませんでした。ポートと実行環境を確認してください。" }
    }
    Write-Host 'ブラウザーが開かない場合は http://localhost:5173 を開いてください。'
    Write-Host '記録はメモリ上に保存され、スクリプトの終了時に失われます。'
    Start-Process -FilePath 'explorer.exe' -ArgumentList 'http://localhost:5173' -WindowStyle Hidden
    Read-Host '画面と記録操作の確認を終えたらEnterを押してください' | Out-Null
} finally {
    foreach ($process in $processes) {
        if (-not $process.HasExited) {
            & taskkill.exe /PID $process.Id /T /F | Out-Null
            $process.WaitForExit()
        }
        $process.Dispose()
    }
    Remove-Item -LiteralPath $logs -Recurse -Force
}
