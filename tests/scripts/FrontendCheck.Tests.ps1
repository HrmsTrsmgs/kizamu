$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '../../scripts/FrontendCheck.ps1')

$logs = [IO.Directory]::CreateTempSubdirectory('kizamu-logged-process-test-')
$workingDirectory = $logs.CreateSubdirectory('working')
$outputFile = Join-Path $logs.FullName 'example.out'
$errorFile = Join-Path $logs.FullName 'example.err'
$process = $null
try {
    $processOptions = @{
        FilePath = (Get-Process -Id $PID).Path
        ArgumentList = '-NoProfile -NonInteractive -Command "[Console]::WriteLine([Environment]::CurrentDirectory); [Console]::Error.WriteLine(''diagnostic''); exit 7"'
        WorkingDirectory = $workingDirectory.FullName
        LogDirectory = $logs.FullName
        Name = 'example'
    }

    $process = Start-LoggedCheckServer @processOptions
    $process.WaitForExit()

    if ($process.ExitCode -ne 7) { throw '起動したプロセスの終了コードを取得できません。' }
    if (([IO.File]::ReadAllText($outputFile).TrimEnd()) -ne $workingDirectory.FullName) { throw '子プロセスの作業ディレクトリが標準出力に記録されていません。' }
    if (([IO.File]::ReadAllText($errorFile).TrimEnd()) -cne 'diagnostic') { throw '標準エラーの内容が一致しません。' }
    Write-Output '指定した作業ディレクトリで起動し、終了コードと名前別の標準出力・標準エラーを取得できます。'
} finally {
    if ($process) { $process.Dispose() }
    [IO.File]::Delete($outputFile)
    [IO.File]::Delete($errorFile)
    $workingDirectory.Delete()
    $logs.Delete()
}
