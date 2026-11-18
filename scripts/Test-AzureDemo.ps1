#Requires -Version 7.0
param(
    [string]$ConfigPath = (Join-Path $PSScriptRoot '../infra/azure/demo.json')
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'azure/Common.ps1')
$config = Read-AzureConfig $ConfigPath
foreach ($key in @('blazorAspnet', 'blazorNode', 'reactAspnet', 'reactNode')) {
    $name = $config.apps.$key.name
    $hostName = Invoke-Az containerapp show --resource-group $config.resourceGroup --name $name --query properties.configuration.ingress.fqdn -o tsv
    $base = "https://$hostName"
    $failure = $null
    for ($attempt = 0; $attempt -lt 6; $attempt++) {
        try {
            $page = Invoke-WebRequest -Uri "$base/" -TimeoutSec 60
            if ($page.StatusCode -ne 200 -or $page.Content -notmatch 'Kizamu') { throw '画面の応答が不正です。' }
            $health = Invoke-RestMethod -Uri "$base/api/health" -TimeoutSec 60
            if ($health.status -ne 'ok') { throw 'APIの応答が不正です。' }
            $failure = $null
            break
        } catch {
            $failure = $_
            if ($attempt -lt 5) { Start-Sleep -Seconds 5 }
        }
    }
    if ($failure) { throw "$name の公開経路を確認できませんでした: $failure" }
    Write-Host "$name OK $base"
}
