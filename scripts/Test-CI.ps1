#Requires -Version 7.0
$ErrorActionPreference = 'Stop'
Push-Location (Join-Path $PSScriptRoot '..')
try {
    npm run lint
    if ($LASTEXITCODE -ne 0) { throw 'npm lintに失敗しました。' }
    npm run build
    if ($LASTEXITCODE -ne 0) { throw 'npm buildに失敗しました。' }
    npm test
    if ($LASTEXITCODE -ne 0) { throw 'npm testに失敗しました。' }
    dotnet test Kizamu.Blazor.slnx --configuration Release
    if ($LASTEXITCODE -ne 0) { throw '.NETのビルド・テストに失敗しました。' }
} finally {
    Pop-Location
}
