$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'FrontendCheck.ps1')
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$api = Join-Path $root 'src/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api'
$frontend = Join-Path $root 'src/frontends/react'
Assert-CheckPortsAvailable -FrontendPort 5173

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
    $dll = Join-Path $api 'bin/Debug/net11.0/Kizamu.Backend.AspNetCore.Api.dll'
    foreach ($item in @(
        @{ File = 'dotnet'; Args = "`"$dll`" --urls http://localhost:5213 --environment Development"; Dir = $api; Check = 'http://localhost:5213/api/health'; Name = 'API' },
        @{ File = 'node'; Args = "`"$vite`" --strictPort"; Dir = $frontend; Check = 'http://localhost:5173/api/health'; Name = 'React' }
    )) {
        $process = Start-LoggedCheckServer -FilePath $item.File -ArgumentList $item.Args -WorkingDirectory $item.Dir `
            -LogDirectory $logs -Name $item.Name
        $processes += $process
        $ready = Wait-HealthEndpoint -Process $process -Uri $item.Check
        if (-not $ready) {
            Write-Host "$($item.Name) を起動できませんでした。"
            if ($process.HasExited) {
                Write-Host "終了コード: $($process.ExitCode)"
            }
            Get-Content -LiteralPath (Join-Path $logs "$($item.Name).err") -Tail 10 -ErrorAction SilentlyContinue | Write-Host
            throw "$($item.Name) を起動できませんでした。ポートと実行環境を確認してください。"
        }
    }
    Show-FrontendForManualCheck -Uri 'http://localhost:5173'
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
