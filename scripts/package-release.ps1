param(
    [string]$Version = "0.7.1",
    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"
$projectDirectory = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $projectDirectory "release"
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $projectDirectory $OutputDirectory
}

$stagingDirectory = Join-Path $OutputDirectory ("XieYiMiao-v" + $Version + "-windows")
$archivePath = $stagingDirectory + ".zip"
$outputDirectoryFull = [System.IO.Path]::GetFullPath($OutputDirectory)
$stagingDirectoryFull = [System.IO.Path]::GetFullPath($stagingDirectory)
$expectedPrefix = $outputDirectoryFull.TrimEnd([System.IO.Path]::DirectorySeparatorChar) +
    [System.IO.Path]::DirectorySeparatorChar
if (-not $stagingDirectoryFull.StartsWith(
    $expectedPrefix,
    [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to prepare a staging directory outside the output directory."
}

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
if (Test-Path -LiteralPath $stagingDirectory) {
    Remove-Item -LiteralPath $stagingDirectory -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $stagingDirectory | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $stagingDirectory "images") | Out-Null

& (Join-Path $projectDirectory "build.ps1") -OutputDirectory $stagingDirectory
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code: $LASTEXITCODE"
}

$selfTestPath = Join-Path $stagingDirectory "自动测试明细.txt"
$releaseExecutable = Join-Path $stagingDirectory "歇一喵.exe"
$quotedSelfTestPath = [char]34 + $selfTestPath + [char]34
$selfTestProcess = Start-Process -FilePath $releaseExecutable -ArgumentList @("--self-test", $quotedSelfTestPath) -Wait -PassThru
if ($selfTestProcess.ExitCode -ne 0) {
    throw "Self-test failed with exit code: $($selfTestProcess.ExitCode)"
}

Copy-Item -LiteralPath (Join-Path $projectDirectory "docs\产品说明书.md") `
    -Destination (Join-Path $stagingDirectory "产品说明书.md") -Force
Copy-Item -LiteralPath (Join-Path $projectDirectory "RELEASE_NOTES.md") `
    -Destination (Join-Path $stagingDirectory "版本说明.md") -Force
Copy-Item -LiteralPath (Join-Path $projectDirectory "docs\测试报告.txt") `
    -Destination (Join-Path $stagingDirectory "测试报告.txt") -Force

$imageMap = @{
    "docs\images\care-weather.png" = "images\care-weather.png"
    "docs\images\care-notes.png" = "images\care-notes.png"
    "docs\images\care-evening.png" = "images\care-evening.png"
    "docs\images\settings-care.png" = "images\settings-care.png"
    "docs\images\settings-pet.png" = "images\settings-pet.png"
}
foreach ($entry in $imageMap.GetEnumerator()) {
    Copy-Item -LiteralPath (Join-Path $projectDirectory $entry.Key) `
        -Destination (Join-Path $stagingDirectory $entry.Value) -Force
}

$exeHash = Get-FileHash -LiteralPath (Join-Path $stagingDirectory "歇一喵.exe") `
    -Algorithm SHA256
@(
    "歇一喵.exe",
    $exeHash.Hash
) | Set-Content -LiteralPath (Join-Path $stagingDirectory "SHA256.txt") -Encoding UTF8

if (Test-Path -LiteralPath $archivePath) {
    Remove-Item -LiteralPath $archivePath -Force
}
Compress-Archive -Path (Join-Path $stagingDirectory "*") `
    -DestinationPath $archivePath -CompressionLevel Optimal

$archiveHash = Get-FileHash -LiteralPath $archivePath -Algorithm SHA256
$archiveName = Split-Path -Leaf $archivePath
$checksumPath = Join-Path $OutputDirectory "SHA256SUMS.txt"
("{0}  {1}" -f $archiveHash.Hash, $archiveName) |
    Set-Content -LiteralPath $checksumPath -Encoding ASCII
Copy-Item -LiteralPath (Join-Path $projectDirectory "RELEASE_NOTES.md") `
    -Destination (Join-Path $OutputDirectory "RELEASE_NOTES.md") -Force
Write-Host "Release archive: $archivePath"
Write-Host "SHA256: $($archiveHash.Hash)"
Write-Host "Checksum file: $checksumPath"
