# Local setup, separate from the Python/PyTorch TTS backend.
# Usage: powershell -ExecutionPolicy Bypass -File .\whiteboard_tools\setup_renderer.ps1
[CmdletBinding()]
param(
    [string]$Destination = 'D:\TestTTS\_whiteboard_engine_lab',
    [string]$PythonVersion = '3.13'
)
$ErrorActionPreference = 'Stop'
$repository = 'https://github.com/gnipbao/whiteboard-video-engine.git'
# Tested upstream revision. Pinning prevents unexpected CLI changes.
$revision = '86848ea27077c1902950bf4e7e37e02d4bfbd1a3'
if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw 'Git is missing from PATH.'
}
if (-not (Get-Command py -ErrorAction SilentlyContinue)) {
    throw 'Python launcher py.exe is missing.'
}
if (-not (Get-Command ffmpeg -ErrorAction SilentlyContinue)) {
    throw 'FFmpeg is missing from PATH.'
}
if (-not (Test-Path -LiteralPath (Join-Path $Destination '.git'))) {
    if (Test-Path -LiteralPath $Destination) {
        throw "Destination exists and is not an engine repository: $Destination"
    }
    git clone $repository $Destination
    if ($LASTEXITCODE -ne 0) { throw 'Cannot clone whiteboard renderer.' }
}
$actualRemote = (git -C $Destination remote get-url origin).Trim()
if ($LASTEXITCODE -ne 0 -or $actualRemote -ne $repository) {
    throw "Existing engine checkout has unexpected remote: $actualRemote"
}
git -C $Destination fetch origin $revision
if ($LASTEXITCODE -ne 0) { throw 'Cannot fetch pinned engine revision.' }
git -C $Destination -c advice.detachedHead=false checkout --detach $revision
if ($LASTEXITCODE -ne 0) { throw 'Cannot select pinned revision. Check local changes.' }
$venv = Join-Path $Destination '.venv'
if (-not (Test-Path -LiteralPath (Join-Path $venv 'Scripts\python.exe'))) {
    py "-$PythonVersion" -m venv $venv
    if ($LASTEXITCODE -ne 0) { throw "Cannot create Python $PythonVersion environment." }
}
$python = Join-Path $venv 'Scripts\python.exe'
& $python -m pip install -e $Destination
if ($LASTEXITCODE -ne 0) { throw 'Engine installation failed.' }
Write-Host 'Renderer installed:' -ForegroundColor Green
Write-Host (Join-Path $venv 'Scripts\whiteboard.exe')
Write-Host 'Optional PyTorch / AI models were NOT installed.'

