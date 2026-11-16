$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$isWindowsHost = [System.Runtime.InteropServices.RuntimeInformation]::IsOSPlatform([System.Runtime.InteropServices.OSPlatform]::Windows)
$dockerRoot = $root
if ($isWindowsHost) {
    $dockerRoot = wsl.exe -d Ubuntu -- wslpath -a ($root.Replace('\', '/'))
    if ($LASTEXITCODE -ne 0 -or -not $dockerRoot) { throw 'WSL Ubuntuのパスを取得できませんでした。' }
}

function Invoke-Docker {
    if ($isWindowsHost) {
        & wsl.exe -d Ubuntu -- docker @args
    } else {
        & docker @args
    }
}

$id = [guid]::NewGuid().ToString('N')
$network = "kizamu-test-$id"
$backendImage = "kizamu-test-backend:$id"
$frontendImage = "kizamu-test-frontend:$id"
$backend = "kizamu-test-backend-$id"
$frontend = "kizamu-test-frontend-$id"
$networkCreated = $false
$backendImageBuilt = $false
$frontendImageBuilt = $false
$backendCreated = $false
$frontendCreated = $false

try {
    Invoke-Docker build -f "$dockerRoot/src/backends/node/Dockerfile" -t $backendImage $dockerRoot
    if ($LASTEXITCODE -ne 0) { throw 'Node APIイメージのビルドに失敗しました。' }
    $backendImageBuilt = $true

    Invoke-Docker build -f "$dockerRoot/src/frontends/react/Dockerfile" -t $frontendImage $dockerRoot
    if ($LASTEXITCODE -ne 0) { throw 'Reactイメージのビルドに失敗しました。' }
    $frontendImageBuilt = $true

    Invoke-Docker network create $network | Out-Null
    if ($LASTEXITCODE -ne 0) { throw '検証用ネットワークを作成できませんでした。' }
    $networkCreated = $true

    Invoke-Docker create --name $backend --network $network --network-alias backend -e HOST=0.0.0.0 -e PORT=5213 $backendImage | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Node APIコンテナを作成できませんでした。' }
    $backendCreated = $true
    Invoke-Docker start $backend | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Node APIコンテナを起動できませんでした。' }

    Invoke-Docker create --name $frontend --network $network -p '127.0.0.1::3000' -e PORT=3000 -e API_BASE_URL=http://backend:5213 $frontendImage | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Reactコンテナを作成できませんでした。' }
    $frontendCreated = $true
    Invoke-Docker start $frontend | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Reactコンテナを起動できませんでした。' }

    $binding = Invoke-Docker port $frontend 3000/tcp
    if ($LASTEXITCODE -ne 0 -or $binding -notmatch '^127\.0\.0\.1:(\d+)$') { throw "Reactの公開ポートを取得できませんでした: $binding" }
    $url = "http://127.0.0.1:$($Matches[1])"
    $health = $null
    for ($attempt = 0; $attempt -lt 60; $attempt++) {
        try {
            $health = Invoke-WebRequest -Uri "$url/api/health" -UseBasicParsing -TimeoutSec 1
            break
        } catch {
            Start-Sleep -Milliseconds 500
        }
    }
    if ($null -eq $health) { throw 'ReactからNode APIへの中継が応答しませんでした。' }
    if ($health.StatusCode -ne 200 -or $health.Headers['Content-Type'] -notmatch 'application/json' -or ($health.Content | ConvertFrom-Json).status -ne 'ok') {
        throw '中継された正常性確認の応答が不正です。'
    }

    $page = Invoke-WebRequest -Uri "$url/" -UseBasicParsing -TimeoutSec 5
    if ($page.StatusCode -ne 200 -or $page.Headers['Content-Type'] -notmatch 'text/html' -or $page.Content -notmatch '<title>Kizamu</title>') {
        throw 'Reactの画面が配信されませんでした。'
    }
    if ($page.Content -notmatch '<script[^>]+src="([^"]+)"') { throw '画面にJavaScriptの参照がありません。' }
    $script = Invoke-WebRequest -Uri ([uri]::new([uri]"$url/", $Matches[1])) -UseBasicParsing -TimeoutSec 5
    if ($script.StatusCode -ne 200 -or $script.Headers['Content-Type'] -notmatch 'javascript' -or -not $script.Content) {
        throw 'ReactのJavaScriptが配信されませんでした。'
    }
    Write-Host 'Reactの画面とNode APIへの中継を確認しました。'
} catch {
    if ($frontendCreated) { Invoke-Docker logs $frontend }
    if ($backendCreated) { Invoke-Docker logs $backend }
    throw
} finally {
    if ($frontendCreated) { Invoke-Docker rm -f $frontend | Out-Null }
    if ($backendCreated) { Invoke-Docker rm -f $backend | Out-Null }
    if ($networkCreated) { Invoke-Docker network rm $network | Out-Null }
    if ($frontendImageBuilt) { Invoke-Docker image rm $frontendImage | Out-Null }
    if ($backendImageBuilt) { Invoke-Docker image rm $backendImage | Out-Null }
}
