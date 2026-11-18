#Requires -Version 7.0
param(
    [Parameter(Mandatory)][ValidatePattern('^[A-Za-z0-9_][A-Za-z0-9_.-]{0,127}$')][string]$ImageTag,
    [string]$ConfigPath = (Join-Path $PSScriptRoot '../infra/azure/demo.json'),
    [string]$Distribution = 'Ubuntu'
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'azure/Common.ps1')
$config = Read-AzureConfig $ConfigPath
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$dockerRoot = $root
$registry = Invoke-Az acr show --name $config.registryName --query loginServer -o tsv
$authDir = Join-Path ([IO.Path]::GetTempPath()) ('kizamu-acr-' + [guid]::NewGuid().ToString('N'))
$parent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar)
if ([IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($authDir)) -ne $parent) { throw '認証用一時パスが不正です。' }
[void][IO.Directory]::CreateDirectory($authDir)
$dockerAuthDir = $authDir

function Invoke-Docker {
    if ($IsWindows) { & wsl.exe -d $Distribution -- docker --config $dockerAuthDir @args }
    else { & docker --config $dockerAuthDir @args }
    if ($LASTEXITCODE -ne 0) { throw "Dockerが失敗しました: $($args[0])" }
}

try {
    if ($IsWindows) {
        $dockerRoot = & wsl.exe -d $Distribution -- wslpath -a ($root.Replace('\', '/'))
        if ($LASTEXITCODE -ne 0) { throw 'WSLのビルドパスを取得できませんでした。' }
        $dockerAuthDir = & wsl.exe -d $Distribution -- wslpath -a ($authDir.Replace('\', '/'))
        if ($LASTEXITCODE -ne 0) { throw 'WSLの認証パスを取得できませんでした。' }
    }
    $token = Invoke-Az acr login --name $config.registryName --expose-token --query accessToken -o tsv
    if ($IsWindows) {
        $token | & wsl.exe -d $Distribution -- docker --config $dockerAuthDir login $registry --username 00000000-0000-0000-0000-000000000000 --password-stdin
    } else {
        $token | & docker --config $dockerAuthDir login $registry --username 00000000-0000-0000-0000-000000000000 --password-stdin
    }
    if ($LASTEXITCODE -ne 0) { throw 'レジストリへのサインインに失敗しました。' }
    $token = $null
    $images = @(
        @{ Repository = 'kizamu-api'; Dockerfile = 'src/backends/aspnetcore/Dockerfile' },
        @{ Repository = 'kizamu-node-api'; Dockerfile = 'src/backends/node/Dockerfile' },
        @{ Repository = 'kizamu-web'; Dockerfile = 'src/frontends/blazor/Dockerfile' },
        @{ Repository = 'kizamu-react'; Dockerfile = 'src/frontends/react/Dockerfile' }
    )
    foreach ($image in $images) {
        $target = "$registry/$($image.Repository):$ImageTag"
        Invoke-Docker build --file "$dockerRoot/$($image.Dockerfile)" --tag $target $dockerRoot
        Invoke-Docker push $target
    }
} finally {
    $token = $null
    if (Test-Path -LiteralPath $authDir) { Remove-Item -LiteralPath $authDir -Recurse -Force }
}
