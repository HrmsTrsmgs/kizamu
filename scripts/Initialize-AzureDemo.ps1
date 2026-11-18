#Requires -Version 7.0
param(
    [string]$ConfigPath = (Join-Path $PSScriptRoot '../infra/azure/demo.json'),
    [switch]$Preview
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'azure/Common.ps1')
$config = Read-AzureConfig $ConfigPath

function Set-Role([string]$PrincipalId, [string]$Role, [string]$Scope) {
    $roleId = Invoke-Az role definition list --name $Role --query '[0].name' -o tsv
    $assignments = Invoke-Az role assignment list --scope $Scope --fill-principal-name false --fill-role-definition-name false -o json | ConvertFrom-Json
    $existing = @($assignments | Where-Object { $_.principalId -eq $PrincipalId -and ($_.roleDefinitionId.Split('/')[-1]) -eq $roleId -and $_.scope -eq $Scope })
    if ($existing.Count -eq 0) {
        Invoke-Az role assignment create --assignee-object-id $PrincipalId --assignee-principal-type ServicePrincipal --role $Role --scope $Scope --output none
    }
}

function Invoke-GitHubBody([string]$Route, [hashtable]$Body, [string]$Method) {
    $file = Join-Path ([IO.Path]::GetTempPath()) ([IO.Path]::GetRandomFileName() + '.json')
    try {
        $Body | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $file -Encoding utf8
        Invoke-Gh api --method $Method $Route --input $file | Out-Null
    } finally {
        if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file -Force }
    }
}

# 実際のリポジトリ設定を読み、名前だけの旧形式とIDを含む形式の両方に対応する。
$oidc = Invoke-Gh api "repos/$($config.githubRepository)/actions/oidc/customization/sub" | ConvertFrom-Json
if (-not $oidc.use_default) { throw 'カスタムOIDC subjectはこのセットアップの対象外です。現在の信頼条件を確認してください。' }
$prefix = $oidc.sub_claim_prefix
if (-not $prefix) {
    if ($oidc.use_immutable_subject) { throw 'OIDC subjectのprefixを取得できませんでした。' }
    $prefix = "repo:$($config.githubRepository)"
}
$subject = "$prefix`:environment:$($config.githubEnvironment.Replace(':', '%3A'))"
$foundation = Join-Path $PSScriptRoot '../infra/azure/foundation.bicep'

$environmentRoute = "repos/$($config.githubRepository)/environments/$([Uri]::EscapeDataString($config.githubEnvironment))"
$existingJson = & gh api $environmentRoute 2>$null
$environmentStatus = $LASTEXITCODE
$createEnvironment = $false
if ($environmentStatus -ne 0) {
    $failure = $existingJson | ConvertFrom-Json
    if ($failure.status -ne 404) { throw 'GitHub Environmentの状態を取得できませんでした。' }
    $createEnvironment = $true
    $policies = @{ branch_policies = @() }
} else {
    $environment = $existingJson | ConvertFrom-Json
    if (-not $environment.deployment_branch_policy.custom_branch_policies) {
        throw '既存GitHub Environmentの保護設定を維持するため停止しました。main専用のカスタムブランチ規則を確認してください。'
    }
    $policies = Invoke-Gh api "$environmentRoute/deployment-branch-policies" | ConvertFrom-Json
    if (@($policies.branch_policies | Where-Object { $_.name -ne 'main' -or $_.type -ne 'branch' }).Count -gt 0) {
        throw '既存Environmentにmain以外の許可規則があります。信頼範囲を確認してください。'
    }
}

if ($Preview) {
    Invoke-AzureTemplate -Config $config -Template $foundation -Name 'kizamu-foundation' -Preview
    Write-Host "初回セットアップの信頼条件: $subject"
    Write-Host '権限: pull用IDにレジストリのAcrPull、CI用IDに対象グループのContributorとレジストリのAcrPush。'
    Write-Host 'GitHub Environmentはmainからのデプロイに限定し、Azureの識別子3件を変数へ設定します。'
    return
}

if ($createEnvironment) {
    Invoke-GitHubBody $environmentRoute @{ deployment_branch_policy = @{ protected_branches = $false; custom_branch_policies = $true } } 'PUT'
}
if (@($policies.branch_policies | Where-Object { $_.name -eq 'main' -and $_.type -eq 'branch' }).Count -eq 0) {
    Invoke-GitHubBody "$environmentRoute/deployment-branch-policies" @{ name = 'main'; type = 'branch' } 'POST'
}

foreach ($provider in @('Microsoft.App', 'Microsoft.ContainerRegistry', 'Microsoft.ManagedIdentity', 'Microsoft.OperationalInsights')) {
    $state = Invoke-Az provider show --namespace $provider --query registrationState -o tsv
    if ($state -ne 'Registered') { Invoke-Az provider register --namespace $provider --wait --output none }
}
Invoke-Az group create --name $config.resourceGroup --location $config.location --output none
Invoke-AzureTemplate -Config $config -Template $foundation -Name 'kizamu-foundation'
$registryId = Invoke-Az acr show --name $config.registryName --query id -o tsv
$pullPrincipal = Invoke-Az identity show --resource-group $config.resourceGroup --name $config.pullIdentityName --query principalId -o tsv
Set-Role $pullPrincipal 'AcrPull' $registryId

$identity = Invoke-Az identity create --resource-group $config.resourceGroup --name $config.deploymentIdentityName --location $config.location -o json | ConvertFrom-Json
$groupId = Invoke-Az group show --name $config.resourceGroup --query id -o tsv
Set-Role $identity.principalId 'Contributor' $groupId
Set-Role $identity.principalId 'AcrPush' $registryId
Invoke-Az identity federated-credential create --resource-group $config.resourceGroup --identity-name $config.deploymentIdentityName --name 'github-actions' --issuer 'https://token.actions.githubusercontent.com' --subject $subject --audiences 'api://AzureADTokenExchange' --output none

$account = Invoke-Az account show --query '{subscription:id,tenant:tenantId}' -o json | ConvertFrom-Json
foreach ($entry in @(
    @{ Name = 'AZURE_CLIENT_ID'; Value = $identity.clientId },
    @{ Name = 'AZURE_TENANT_ID'; Value = $account.tenant },
    @{ Name = 'AZURE_SUBSCRIPTION_ID'; Value = $account.subscription }
)) {
    Invoke-Gh variable set $entry.Name --repo $config.githubRepository --env $config.githubEnvironment --body $entry.Value
}
Write-Host 'AzureとGitHubの初回セットアップが完了しました。'
