param(
    [string]$Distribution = 'Ubuntu'
)

$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Invoke-DockerCompose.ps1') -Distribution $Distribution -ComposeCommand @('down') -FailureMessage 'Dockerコンテナーの停止に失敗しました。'
