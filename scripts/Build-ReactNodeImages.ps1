param(
    [string]$Distribution = 'Ubuntu'
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$dockerRoot = $root
if ($IsWindows) {
    $dockerRoot = wsl.exe -d $Distribution -- wslpath -a ($root.Replace('\', '/'))
    if ($LASTEXITCODE -ne 0 -or -not $dockerRoot) { throw 'WSL内のパスを取得できませんでした。' }
}

function Invoke-Docker {
    if ($IsWindows) {
        & wsl.exe -d $Distribution -- docker @args
    } else {
        & docker @args
    }
}

Invoke-Docker build -f "$dockerRoot/src/backends/node/Dockerfile" -t kizamu-node-api:local $dockerRoot
if ($LASTEXITCODE -ne 0) { throw 'Node APIイメージのビルドに失敗しました。' }

Invoke-Docker build -f "$dockerRoot/src/frontends/react/Dockerfile" -t kizamu-react:local $dockerRoot
if ($LASTEXITCODE -ne 0) { throw 'Reactイメージのビルドに失敗しました。' }

Write-Host 'kizamu-node-api:local と kizamu-react:local をビルドしました。'
