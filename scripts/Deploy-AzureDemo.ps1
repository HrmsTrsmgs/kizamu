#Requires -Version 7.0
param(
    [ValidatePattern('^[A-Za-z0-9_][A-Za-z0-9_.-]{0,127}$')][string]$ImageTag,
    [string]$ConfigPath = (Join-Path $PSScriptRoot '../infra/azure/demo.json'),
    [string]$Distribution = 'Ubuntu',
    [switch]$UseExistingImages,
    [switch]$Preview
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'azure/Common.ps1')
$config = Read-AzureConfig $ConfigPath
$infra = Join-Path $PSScriptRoot '../infra/azure'
if (-not $Preview -and -not $ImageTag) { throw 'デプロイにはImageTagを指定してください。' }

if ($UseExistingImages) {
    if (-not $ImageTag) { throw '既存イメージの指定にはImageTagが必要です。' }
    foreach ($repository in @('kizamu-api', 'kizamu-node-api', 'kizamu-web', 'kizamu-react')) {
        Invoke-Az acr repository show --name $config.registryName --image "${repository}:$ImageTag" --output none
    }
}

Invoke-AzureTemplate -Config $config -Template (Join-Path $infra 'foundation.bicep') -Name 'kizamu-foundation' -Preview:$Preview

if ($ImageTag) {
    $registry = Invoke-Az acr show --name $config.registryName --query loginServer -o tsv
    $images = @{
        aspnetApi = "$registry/kizamu-api:$ImageTag"
        nodeApi = "$registry/kizamu-node-api:$ImageTag"
        blazor = "$registry/kizamu-web:$ImageTag"
        react = "$registry/kizamu-react:$ImageTag"
    }
} else {
    # プレビューでは現在のイメージを使い、構成だけの差を確認する。
    $images = @{}
    foreach ($pair in @(@('aspnetApi', 'aspnetApi'), @('nodeApi', 'nodeApi'), @('blazor', 'blazorAspnet'), @('react', 'reactAspnet'))) {
        $images[$pair[0]] = Invoke-Az containerapp show --resource-group $config.resourceGroup --name $config.apps.($pair[1]).name --query 'properties.template.containers[0].image' -o tsv
    }
}
if (-not $Preview -and -not $UseExistingImages) {
    & (Join-Path $PSScriptRoot 'Publish-AzureImages.ps1') -ConfigPath $ConfigPath -ImageTag $ImageTag -Distribution $Distribution
}
Invoke-AzureTemplate -Config $config -Template (Join-Path $infra 'apps.bicep') -Name 'kizamu-apps' -AdditionalParameters @{ images = $images } -Preview:$Preview
if (-not $Preview) { & (Join-Path $PSScriptRoot 'Test-AzureDemo.ps1') -ConfigPath $ConfigPath }
