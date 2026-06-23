#requires -version 5.1
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [switch]$SelfContained
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$Version = '1.0.0'
$ReleaseTag = "v$Version"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$projectPath = Join-Path $repoRoot 'src\ADAccessReporter\ADAccessReporter.csproj'
$releaseDir = Join-Path $repoRoot 'release'
$publishDir = Join-Path $repoRoot 'artifacts\publish'
$assetDir = Join-Path $repoRoot 'assets'
$exeFileName = "ADAccessReporter-$ReleaseTag-win-x64.exe"
$zipFileName = "ADAccessReporter-$ReleaseTag-portable.zip"
$exePath = Join-Path $releaseDir $exeFileName
$zipPath = Join-Path $releaseDir $zipFileName

New-Item -ItemType Directory -Force -Path $releaseDir, $publishDir, $assetDir | Out-Null

if (-not (Test-Path -LiteralPath (Join-Path $assetDir 'ad-access-reporter.ico'))) {
    & (Join-Path $PSScriptRoot 'New-VisualAssets.ps1') | Out-Host
}

Get-ChildItem -LiteralPath $releaseDir -File -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -match '^ADAccessReporter-v[\d.]+-(?:win-x64|portable)\.(?:exe|zip)$' -or $_.Name -in @('checksums.txt', 'latest.json', 'README.txt') } |
    Remove-Item -Force

Remove-Item -LiteralPath $publishDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $publishDir | Out-Null

$selfContainedValue = if ($SelfContained) { 'true' } else { 'false' }
$publishArgs = @(
    'publish', $projectPath,
    '--configuration', $Configuration,
    '--runtime', 'win-x64',
    '--self-contained', $selfContainedValue,
    '-p:PublishSingleFile=true',
    '-p:DebugType=none',
    '-p:DebugSymbols=false',
    '--output', $publishDir
)

if ($SelfContained) {
    $publishArgs += '-p:EnableCompressionInSingleFile=true'
}

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

$publishedExe = Join-Path $publishDir 'ADAccessReporter.exe'
if (-not (Test-Path -LiteralPath $publishedExe)) {
    throw "Publish did not create $publishedExe"
}

Copy-Item -LiteralPath $publishedExe -Destination $exePath -Force
Copy-Item -LiteralPath (Join-Path $repoRoot 'README.md') -Destination (Join-Path $releaseDir 'README.md') -Force
Copy-Item -LiteralPath (Join-Path $repoRoot 'LICENSE') -Destination (Join-Path $releaseDir 'LICENSE') -Force

$runtimeNote = if ($SelfContained) {
    'This build is self-contained and does not require a separate .NET runtime.'
}
else {
    'This build requires the .NET 8 Desktop Runtime on the target PC.'
}

$readmeText = @(
    "AD Access Reporter $ReleaseTag portable release",
    '',
    "Run $exeFileName on a Windows domain-connected machine.",
    $runtimeNote,
    '',
    'Use the AD Groups tab for membership comparison.',
    'Use the Folder Rights tab for local, file, or UNC NTFS permission reports.',
    ''
)
Set-Content -LiteralPath (Join-Path $releaseDir 'README.txt') -Value $readmeText -Encoding ASCII

$latestManifest = [ordered]@{
    version = $Version
    tagName = $ReleaseTag
    windowsExe = $exeFileName
    portableZip = $zipFileName
    selfContained = [bool]$SelfContained
    requiresDotNetDesktopRuntime = -not [bool]$SelfContained
    releaseUrl = "https://github.com/eMacTh3Creator/ADAccessReporter/releases/tag/$ReleaseTag"
}
($latestManifest | ConvertTo-Json -Depth 3) | Set-Content -LiteralPath (Join-Path $releaseDir 'latest.json') -Encoding UTF8

Remove-Item -LiteralPath $zipPath -Force -ErrorAction SilentlyContinue
$zipSources = @(
    $exePath,
    (Join-Path $releaseDir 'README.md'),
    (Join-Path $releaseDir 'README.txt'),
    (Join-Path $releaseDir 'LICENSE'),
    (Join-Path $releaseDir 'latest.json')
)
$compressed = $false
for ($attempt = 1; $attempt -le 5 -and -not $compressed; $attempt++) {
    try {
        Start-Sleep -Milliseconds (300 * $attempt)
        Compress-Archive -Path $zipSources -DestinationPath $zipPath -Force
        $compressed = $true
    }
    catch {
        if ($attempt -eq 5) {
            throw
        }

        Write-Warning "Zip attempt $attempt failed: $($_.Exception.Message)"
    }
}

$hashRows = Get-ChildItem -LiteralPath $releaseDir -File |
    Where-Object { $_.Name -ne 'checksums.txt' } |
    Sort-Object Name |
    ForEach-Object {
        $hash = Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256
        '{0}  {1}' -f $hash.Hash, $_.Name
    }
Set-Content -LiteralPath (Join-Path $releaseDir 'checksums.txt') -Value $hashRows -Encoding ASCII

Write-Host "Release built: $exePath"
Write-Host "Portable zip: $zipPath"
