# Drop the previous result before launch so a failed start cannot be read as the last PASS.
# On timeout, stop only this process tree. Stopping Unity.exe by name would close the user's editor.
# Do not redirect Unity's stdout. Start-Process then leaves ExitCode empty, and a real pass becomes START_FAILED. -logFile already keeps the editor log.

$ErrorActionPreference = 'Stop'

$TimeoutMinutes = 10
$MaxListedFailures = 10
$MaxErrorLines = 8

$Repo = Split-Path -Parent $PSScriptRoot
$Logs = Join-Path $Repo 'Logs'
$ResultsPath = Join-Path $Logs 'TestResults.xml'
$LogPath = Join-Path $Logs 'TestRun.log'
$ResultsRelative = 'Logs/TestResults.xml'
$LogRelative = 'Logs/TestRun.log'
$TestAssembly = 'CardRPGFramework.Tests.EditMode'

function Exit-WithSummary {
    param(
        [int]$Code,
        [string[]]$Lines
    )
    foreach ($line in $Lines) {
        Write-Output $line
    }
    exit $Code
}

function Get-LogErrorLines {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) {
        return @()
    }
    $pattern = 'error CS|Scripts have compiler errors|Fatal Error|Exception'
    $lines = @(Get-Content -LiteralPath $Path -Encoding UTF8 -ErrorAction SilentlyContinue |
        Where-Object { $_ -match $pattern } |
        ForEach-Object { $_.Trim() } |
        Where-Object { $_ -ne '' } |
        Select-Object -First $MaxErrorLines)
    return $lines
}

function Format-StartFailed {
    param(
        [string]$Reason,
        [object]$ExitCode
    )
    $lines = @(
        'Unity EditMode Tests: START_FAILED'
        ''
        'Reason:'
        $Reason
    )
    if ($null -ne $ExitCode) {
        $lines += ''
        $lines += 'ExitCode:'
        $lines += "$ExitCode"
    }
    if (Test-Path -LiteralPath $LogPath) {
        $lines += ''
        $lines += 'Log:'
        $lines += $LogRelative
        $errorLines = @(Get-LogErrorLines -Path $LogPath)
        if ($errorLines.Count -gt 0) {
            $lines += ''
            $lines += 'Error:'
            $lines += $errorLines
        }
    }
    Exit-WithSummary -Code 1 -Lines $lines
}

function Format-ProjectOpen {
    param([string]$ReasonLine)
    Exit-WithSummary -Code 1 -Lines @(
        'Unity EditMode Tests: PROJECT_OPEN'
        ''
        'Reason:'
        $ReasonLine.Trim()
    )
}

if (-not (Test-Path -LiteralPath (Join-Path $Repo 'ProjectSettings\ProjectVersion.txt'))) {
    Format-StartFailed -Reason 'ProjectSettings/ProjectVersion.txt not found' -ExitCode $null
}

$versionText = Get-Content -LiteralPath (Join-Path $Repo 'ProjectSettings\ProjectVersion.txt') -Raw
if ($versionText -notmatch '(?m)^m_EditorVersion:\s*(\S+)\s*$') {
    Format-StartFailed -Reason 'm_EditorVersion is missing from ProjectSettings/ProjectVersion.txt' -ExitCode $null
}
$editorVersion = $Matches[1]
$unity = Join-Path ${env:ProgramFiles} "Unity\Hub\Editor\$editorVersion\Editor\Unity.exe"
if (-not (Test-Path -LiteralPath $unity)) {
    Format-StartFailed -Reason "Unity version $editorVersion not found" -ExitCode $null
}

New-Item -ItemType Directory -Force -Path $Logs | Out-Null
try {
    foreach ($path in @($ResultsPath, $LogPath)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force
        }
    }
}
catch {
    Format-StartFailed -Reason "could not remove previous results: $($_.Exception.Message)" -ExitCode $null
}

$startedAt = [DateTime]::UtcNow
$argumentList = @(
    '-batchmode'
    '-nographics'
    '-projectPath'
    $Repo
    '-runTests'
    '-testPlatform'
    'EditMode'
    '-assemblyNames'
    $TestAssembly
    '-testResults'
    $ResultsPath
    '-logFile'
    $LogPath
)
$elapsed = [System.Diagnostics.Stopwatch]::StartNew()
try {
    $process = Start-Process -FilePath $unity -ArgumentList $argumentList -WorkingDirectory $Repo -WindowStyle Hidden -PassThru
}
catch {
    Format-StartFailed -Reason "Unity start failed: $($_.Exception.Message)" -ExitCode $null
}
if ($null -eq $process) {
    Format-StartFailed -Reason 'Unity process did not start' -ExitCode $null
}

$timeoutMs = $TimeoutMinutes * 60 * 1000
$exited = $process.WaitForExit($timeoutMs)
if (-not $exited) {
    & taskkill.exe /PID $process.Id /T /F | Out-Null
    [void]$process.WaitForExit(15000)
    $elapsed.Stop()
    Exit-WithSummary -Code 1 -Lines @(
        'Unity EditMode Tests: TIMEOUT'
        ''
        'Timeout:'
        "$TimeoutMinutes minutes"
    )
}
[void]$process.WaitForExit()
$elapsed.Stop()
$exitCode = $process.ExitCode
$durationSeconds = [int][Math]::Round($elapsed.Elapsed.TotalSeconds)

$hasFreshResults = (Test-Path -LiteralPath $ResultsPath) -and ((Get-Item -LiteralPath $ResultsPath).LastWriteTimeUtc -gt $startedAt)

if ($hasFreshResults) {
    $document = New-Object System.Xml.XmlDocument
    try {
        $document.Load($ResultsPath)
    }
    catch {
        Format-StartFailed -Reason 'TestResults.xml could not be read' -ExitCode $exitCode
    }

    $run = $document.DocumentElement
    if ($null -eq $run -or $run.LocalName -ne 'test-run') {
        Format-StartFailed -Reason 'TestResults.xml has no test-run' -ExitCode $exitCode
    }

    $failedText = $run.GetAttribute('failed')
    $passedText = $run.GetAttribute('passed')
    $failedCount = 0
    $passedCount = 0
    $countsOk = [int]::TryParse($failedText, [ref]$failedCount) -and [int]::TryParse($passedText, [ref]$passedCount)
    if (-not $countsOk) {
        Format-StartFailed -Reason 'test-run is missing passed or failed' -ExitCode $exitCode
    }

    if ($failedCount -gt 0) {
        $cases = @($document.SelectNodes('//test-case[failure]'))
        $listed = [Math]::Min($MaxListedFailures, [Math]::Min($cases.Count, $failedCount))
        $lines = @(
            'Unity EditMode Tests: FAIL'
            ''
            "Failed: $failedCount"
            ''
        )
        for ($i = 0; $i -lt $listed; $i++) {
            $case = $cases[$i]
            $fullName = $case.GetAttribute('fullname')
            if ([string]::IsNullOrWhiteSpace($fullName)) {
                $fullName = $case.GetAttribute('name')
            }
            $messageNode = $case.SelectSingleNode('failure/message')
            $message = ''
            if ($null -ne $messageNode) {
                $message = $messageNode.InnerText.Trim()
            }
            if ($message.Length -gt 400) {
                $message = $message.Substring(0, 400) + '...'
            }
            $lines += "$($i + 1). $fullName"
            $lines += 'Message:'
            $lines += $message
            $lines += ''
        }
        $remaining = $failedCount - $listed
        $lines += "Remaining failures: $remaining"
        $lines += ''
        $lines += 'Result:'
        $lines += $ResultsRelative
        Exit-WithSummary -Code 1 -Lines $lines
    }

    if ($exitCode -eq 0 -and $failedCount -eq 0 -and $passedCount -gt 0) {
        Exit-WithSummary -Code 0 -Lines @(
            'Unity EditMode Tests: PASS'
            ''
            "Passed: $passedCount"
            'Failed: 0'
            "Duration: ${durationSeconds}s"
            ''
            'Result:'
            $ResultsRelative
        )
    }

    if ($passedCount -eq 0 -and $failedCount -eq 0) {
        Format-StartFailed -Reason 'no tests ran' -ExitCode $exitCode
    }

    Format-StartFailed -Reason "exit code $exitCode with no failed tests" -ExitCode $exitCode
}

if (Test-Path -LiteralPath $LogPath) {
    $occupied = @(Get-Content -LiteralPath $LogPath -Encoding UTF8 -ErrorAction SilentlyContinue |
        Where-Object { $_ -match 'another Unity instance|cannot open the same project' } |
        Select-Object -First 1)
    if ($occupied.Count -gt 0) {
        Format-ProjectOpen -ReasonLine $occupied[0]
    }
}

Format-StartFailed -Reason 'Unity exited without a new test result' -ExitCode $exitCode
