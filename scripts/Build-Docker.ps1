param(
    [string]$Distribution = 'Ubuntu'
)

$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Invoke-DockerCompose.ps1') -Distribution $Distribution -ComposeCommand @('build') -FailureMessage 'Dockerイメージのビルドに失敗しました。'
