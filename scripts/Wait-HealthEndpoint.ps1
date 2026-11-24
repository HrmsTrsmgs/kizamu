function Wait-HealthEndpoint {
    param(
        [System.Diagnostics.Process]$Process,
        [string]$Uri
    )

    $maxAttempts = 60
    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        if ($Process.HasExited) { return $false }
        try {
            $response = Invoke-WebRequest -Uri $Uri -UseBasicParsing -TimeoutSec 1
            if ($response.StatusCode -eq 200) { return $true }
        } catch {
            # The endpoint may still be starting.
        }
        if ($attempt -lt $maxAttempts -and -not $Process.HasExited) {
            Start-Sleep -Milliseconds 500
        }
    }
    return $false
}
