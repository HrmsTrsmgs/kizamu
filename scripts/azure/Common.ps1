#Requires -Version 7.0

function Invoke-Az {
    $output = & az @args
    if ($LASTEXITCODE -ne 0) { throw "Azure CLIが失敗しました: az $($args[0]) $($args[1])" }
    $output
}

function Invoke-Gh {
    $output = & gh @args
    if ($LASTEXITCODE -ne 0) { throw "GitHub CLIが失敗しました: gh $($args[0])" }
    $output
}

function Read-AzureConfig([string]$Path) {
    $config = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    foreach ($field in @('resourceGroup', 'location', 'registryName', 'registrySku', 'environmentName', 'logsName', 'pullIdentityName', 'deploymentIdentityName', 'githubRepository', 'githubEnvironment', 'apps')) {
        if (-not $config.$field) { throw "構成に $field がありません。" }
    }
    $names = @($config.apps.PSObject.Properties | ForEach-Object { $_.Value.name })
    if ($names.Count -ne 6 -or ($names | Select-Object -Unique).Count -ne 6) { throw '6サービスには異なる名前を指定してください。' }
    $config
}

function Invoke-AzureTemplate {
    param(
        $Config,
        [string]$Template,
        [string]$Name,
        [hashtable]$AdditionalParameters = @{},
        [switch]$Preview
    )
    $parameters = @{ config = @{ value = $Config } }
    foreach ($key in $AdditionalParameters.Keys) { $parameters[$key] = @{ value = $AdditionalParameters[$key] } }
    $file = Join-Path ([IO.Path]::GetTempPath()) ([IO.Path]::GetRandomFileName() + '.json')
    try {
        @{ parameters = $parameters } | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $file -Encoding utf8
        if ($Preview) {
            Invoke-Az deployment group what-if --resource-group $Config.resourceGroup --template-file $Template --parameters "@$file" --no-pretty-print --query "changes[?changeType!='Ignore'].{resource:resourceId,change:changeType,properties:delta[].path}" -o json
        } else {
            Invoke-Az deployment group create --resource-group $Config.resourceGroup --name $Name --template-file $Template --parameters "@$file" --mode Incremental --output none
        }
    } finally {
        if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file -Force }
    }
}
