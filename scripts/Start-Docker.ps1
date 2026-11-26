param(
    [string]$Distribution = 'Ubuntu'
)

$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Invoke-DockerCompose.ps1') -Distribution $Distribution -ComposeCommand @('up', '-d') -FailureMessage 'Dockerコンテナーの起動に失敗しました。'
