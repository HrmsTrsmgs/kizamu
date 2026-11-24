$ErrorActionPreference = 'Stop'

function Copy-ReactCheckFixture {
    param([System.IO.DirectoryInfo]$Root)

    $scripts = $Root.CreateSubdirectory('scripts')
    Copy-Item (Join-Path $sourceScripts 'Check-React.ps1') $scripts.FullName
    @'
. (Join-Path $sourceScripts 'FrontendCheck.ps1')
function Assert-CheckPortsAvailable { param([int]$FrontendPort) }
'@ | Set-Content (Join-Path $scripts.FullName 'FrontendCheck.ps1')

    $Root.CreateSubdirectory('src/backends/aspnetcore/Kizamu.Backend.AspNetCore.Api') | Out-Null
    $Root.CreateSubdirectory('src/frontends/react') | Out-Null
    $vite = $Root.CreateSubdirectory('node_modules/vite/bin')
    $typescript = $Root.CreateSubdirectory('node_modules/typescript/bin')
    New-Item -ItemType File -Path (Join-Path $vite.FullName 'vite.js'), (Join-Path $typescript.FullName 'tsc') | Out-Null

    Join-Path $scripts.FullName 'Check-React.ps1'
}

function dotnet { & cmd.exe /d /c exit 0 }
function node { & cmd.exe /d /c exit 0 }

function Invoke-WebRequest {
    param([string]$Uri, [switch]$UseBasicParsing, [int]$TimeoutSec)

    if ($Uri -eq 'http://localhost:5213/api/health') { @{ StatusCode = 200 } }
}

function Start-Process {
    param(
        [string]$FilePath, [string]$ArgumentList, [string]$WorkingDirectory,
        [string]$RedirectStandardOutput, [string]$RedirectStandardError,
        [string]$WindowStyle, [switch]$PassThru
    )

    $fixtureState.CheckLogs = Split-Path $RedirectStandardError -Parent
    $PSBoundParameters.FilePath = (Get-Process -Id $PID).Path
    switch ($FilePath) {
        'dotnet' {
            $PSBoundParameters.ArgumentList = '-NoProfile -NonInteractive -Command "Start-Sleep -Seconds 120"'
            $process = Microsoft.PowerShell.Management\Start-Process @PSBoundParameters
            $fixtureState.ApiChild = Get-Process -Id $process.Id
            $process
        }
        'node' {
            $PSBoundParameters.ArgumentList = "-NoProfile -NonInteractive -Command `"[Console]::Error.WriteLine('$startupError'); exit $exitCode`""
            $process = Microsoft.PowerShell.Management\Start-Process @PSBoundParameters -Wait
            $fixtureState.ReactExitedWithDiagnostic = $process.ExitCode -eq $exitCode -and
                ([IO.File]::ReadAllText($RedirectStandardError).TrimEnd() -ceq $startupError)
            $process
        }
        default { throw "Unexpected command: $FilePath" }
    }
}

$sourceScripts = Join-Path $PSScriptRoot '../../scripts'
$fixture = [IO.Directory]::CreateTempSubdirectory('kizamu-react-check-test-')
$startupError = 'The frontend startup configuration is invalid.'
$exitCode = 37
$fixtureState = @{
    ApiChild = $null
    CheckLogs = $null
    ReactExitedWithDiagnostic = $false
}
$beforeCleanup = [System.Collections.Generic.List[string]]::new()
try {
    $check = Copy-ReactCheckFixture -Root $fixture
    $failure = $null
    try {
        & $check *>&1 | ForEach-Object {
            if ($fixtureState.ApiChild -and -not $fixtureState.ApiChild.HasExited -and (Test-Path -LiteralPath $fixtureState.CheckLogs)) {
                $beforeCleanup.Add([string]$_)
            }
        }
    } catch {
        $failure = $_
    }

    if (-not $failure) { throw 'React の起動失敗が呼び出し元に通知されません。' }
    if (-not $fixtureState.ApiChild) { throw "代役の API を起動できませんでした。呼び出し元のエラー: $($failure.Exception.Message)" }
    if (-not $fixtureState.ReactExitedWithDiagnostic) { throw '代役の React が指定の診断と終了コードを返しませんでした。' }
    if (-not $fixtureState.ApiChild.HasExited) { throw '先に起動した API が停止していません。' }
    if (-not $fixtureState.CheckLogs -or (Test-Path -LiteralPath $fixtureState.CheckLogs)) { throw '一時ログが片付けられていません。' }

    $diagnostics = $beforeCleanup -join [Environment]::NewLine
    if ($diagnostics -notlike "*$startupError*") {
        throw "後始末前に React の起動エラーの要点が表示されません。呼び出し元のエラー: $($failure.Exception.Message)"
    }
    if ($diagnostics -notmatch '\bReact\b') { throw '後始末前に失敗したプロセス名が表示されません。' }
    if ($diagnostics -notmatch "\b$exitCode\b") { throw '後始末前に子プロセスの終了コードが表示されません。' }
    Write-Output 'React の起動に失敗すると、原因を表示してから API と一時ログを片付けます。'
} finally {
    if ($fixtureState.ApiChild) {
        if (-not $fixtureState.ApiChild.HasExited) {
            $fixtureState.ApiChild.Kill()
            $fixtureState.ApiChild.WaitForExit()
        }
        $fixtureState.ApiChild.Dispose()
    }
    if ($fixtureState.CheckLogs -and (Test-Path -LiteralPath $fixtureState.CheckLogs)) {
        [IO.Directory]::Delete($fixtureState.CheckLogs, $true)
    }
    $fixture.Delete($true)
}
