$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '../../scripts/Wait-HealthEndpoint.ps1')

function Assert-HealthCheck {
    param([string]$Name, [System.Diagnostics.Process]$Process, [object[]]$Responses, [bool]$Ready, [string[]]$Expected)

    $events = [System.Collections.Generic.List[string]]::new()
    $responsesInOrder = [System.Collections.Generic.Queue[object]]::new($Responses)

    function Invoke-WebRequest {
        param([string]$Uri, [switch]$UseBasicParsing, [int]$TimeoutSec)
        $events.Add('request')
        $status = $responsesInOrder.Dequeue()
        if ($status -eq 'error') { throw 'Connection unavailable' }
        @{ StatusCode = $status }
    }

    function Start-Sleep {
        param([int]$Milliseconds)
        $events.Add("wait:$Milliseconds")
    }

    $actual = Wait-HealthEndpoint -Process $Process -Uri 'http://localhost/api/health'
    if ($actual -ne $Ready -or ($events -join ',') -cne ($Expected -join ',')) {
        throw "$Name failed. Expected ready=$Ready, events=$($Expected -join ','). Got ready=$actual, events=$($events -join ',')."
    }
    Write-Output "$Name passed."
}

$running = [System.Diagnostics.Process]::GetCurrentProcess()
$exited = Start-Process -FilePath 'cmd.exe' -ArgumentList '/c exit 0' -WindowStyle Hidden -PassThru -Wait
try {
    Assert-HealthCheck 'HTTP 200を受け取ると直ちに成功します' $running @(200) $true @('request')
    Assert-HealthCheck 'HTTP 204を受け取ると待ってから再試行します' $running @(204, 200) $true @('request', 'wait:500', 'request')
    Assert-HealthCheck '接続に失敗すると待ってから再試行します' $running @('error', 200) $true @('request', 'wait:500', 'request')
    Assert-HealthCheck 'プロセスが終了済みなら直ちに失敗します' $exited @() $false @()
    $attempts = @('request', 'wait:500') * 59 + 'request'
    Assert-HealthCheck '60回目までHTTP 200が来なければ最後の待機なしで失敗します' $running (@(204) * 60) $false $attempts
} finally {
    $exited.Dispose()
    $running.Dispose()
}
