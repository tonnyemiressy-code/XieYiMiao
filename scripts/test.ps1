param(
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$projectDirectory = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$distDirectory = Join-Path $projectDirectory "dist"
$executable = Join-Path $distDirectory "歇一喵.exe"
$report = Join-Path $distDirectory "self-test.txt"

if (-not $SkipBuild) {
    & (Join-Path $projectDirectory "build.ps1") -OutputDirectory $distDirectory
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code: $LASTEXITCODE"
    }
}

if (-not (Test-Path -LiteralPath $executable)) {
    throw "Executable not found: $executable"
}

$quotedReport = [char]34 + $report + [char]34
$selfTestProcess = Start-Process -FilePath $executable -ArgumentList @("--self-test", $quotedReport) -Wait -PassThru
if ($selfTestProcess.ExitCode -ne 0) {
    Get-Content -LiteralPath $report -Encoding UTF8
    throw "Self-test failed with exit code: $($selfTestProcess.ExitCode)"
}

Get-Content -LiteralPath $report -Encoding UTF8

$smokeProcess = Start-Process -FilePath $executable -ArgumentList "--runtime-smoke" -Wait -PassThru
if ($smokeProcess.ExitCode -ne 0) {
    throw "Runtime smoke test failed with exit code: $($smokeProcess.ExitCode)"
}

Write-Host "All tests passed."
