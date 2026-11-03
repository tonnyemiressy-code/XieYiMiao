param(
    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"
$projectDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceDirectory = Join-Path $projectDirectory "src"
$buildDirectory = Join-Path $projectDirectory "bin"
$appName = [string]::Concat(
    [char]0x6B47,
    [char]0x4E00,
    [char]0x55B5
)

if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $projectDirectory "dist"
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $projectDirectory $OutputDirectory
}

New-Item -ItemType Directory -Force -Path $buildDirectory | Out-Null
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$frameworkCandidates = @(
    "C:\Windows\Microsoft.NET\Framework64\v4.0.30319",
    "C:\Windows\Microsoft.NET\Framework\v4.0.30319"
)
$framework = $frameworkCandidates |
    Where-Object { Test-Path -LiteralPath (Join-Path $_ "csc.exe") } |
    Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($framework)) {
    throw "The Windows .NET Framework compiler (csc.exe) was not found."
}

$wpf = Join-Path $framework "WPF"
$compiler = Join-Path $framework "csc.exe"
$executable = Join-Path $buildDirectory ($appName + ".exe")
$iconBuilder = Join-Path $buildDirectory "IconBuilder.exe"
$iconPath = Join-Path $buildDirectory "app.ico"

$references = @(
    (Join-Path $wpf "PresentationCore.dll"),
    (Join-Path $wpf "PresentationFramework.dll"),
    (Join-Path $wpf "WindowsBase.dll"),
    (Join-Path $framework "System.Xaml.dll"),
    (Join-Path $framework "System.Windows.Forms.dll"),
    (Join-Path $framework "System.Drawing.dll"),
    (Join-Path $framework "System.Runtime.Serialization.dll"),
    (Join-Path $framework "System.Xml.dll")
)

& $compiler @(
    "/nologo",
    "/target:exe",
    "/optimize+",
    "/codepage:65001",
    "/reference:$(Join-Path $framework 'System.Drawing.dll')",
    "/out:$iconBuilder",
    (Join-Path $projectDirectory "tools\IconBuilder.cs")
)
if ($LASTEXITCODE -ne 0) {
    throw "Icon build failed with exit code: $LASTEXITCODE"
}

& $iconBuilder $iconPath (Join-Path $projectDirectory "assets\cat-sprites-v2.png")
if ($LASTEXITCODE -ne 0) {
    throw "Icon generation failed with exit code: $LASTEXITCODE"
}

$arguments = @(
    "/nologo",
    "/target:winexe",
    "/optimize+",
    "/platform:anycpu",
    "/codepage:65001",
    "/win32manifest:$projectDirectory\app.manifest",
    "/win32icon:$iconPath",
    "/resource:$projectDirectory\assets\cat-sprites-v2.png,XieYiMiao.Assets.CatSprites",
    "/resource:$projectDirectory\assets\cat-care-sprites.png,XieYiMiao.Assets.CatCareSprites",
    "/resource:$projectDirectory\assets\cat-outfit-sprites.png,XieYiMiao.Assets.CatOutfitSprites",
    "/out:$executable"
)
$arguments += 0..7 | ForEach-Object {
    $frame = $_.ToString("00")
    "/resource:$projectDirectory\assets\cat-walk-$frame.png,XieYiMiao.Assets.CatWalk$frame"
}
$arguments += $references | ForEach-Object { "/reference:$_" }
$arguments += Get-ChildItem -LiteralPath $sourceDirectory -Filter "*.cs" |
    Sort-Object Name |
    ForEach-Object { $_.FullName }

& $compiler $arguments
if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code: $LASTEXITCODE"
}

$finalExecutable = Join-Path $OutputDirectory ($appName + ".exe")
Copy-Item -LiteralPath $executable -Destination $finalExecutable -Force

$hash = Get-FileHash -LiteralPath $finalExecutable -Algorithm SHA256
Write-Host "Build complete: $finalExecutable"
Write-Host "SHA256: $($hash.Hash)"
