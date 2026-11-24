$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'FrontendCheck.ps1')
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$api = Join-Path $root 'src/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api'
$frontend = Join-Path $root 'src/frontends/blazor/Kizamu.Frontend.Blazor.Server'
Assert-CheckPortsAvailable -FrontendPort 5214

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
        $dll = Join-Path $item.Dir ("bin/Debug/net11.0/" + $item.Dll)
        $process = Start-LoggedCheckServer -FilePath 'dotnet' -ArgumentList "`"$dll`" --urls http://localhost:$($item.Port) --environment Development" -WorkingDirectory $item.Dir `
            -LogDirectory $logs -Name $item.Name
        $processes += $process
        $ready = Wait-HealthEndpoint -Process $process -Uri $item.Check
        if (-not $ready) {
            Get-Content (Join-Path $logs "$($item.Name).out"), (Join-Path $logs "$($item.Name).err") -ErrorAction SilentlyContinue | Write-Host
            throw "$($item.Name) を起動できませんでした。"
        }
    }
    Show-FrontendForManualCheck -Uri 'http://localhost:5214'
} finally {
    foreach ($process in $processes) {
        if (-not $process.HasExited) { $process.Kill(); $process.WaitForExit() }
        $process.Dispose()
    }
    Remove-Item -LiteralPath $logs -Recurse -Force
}
