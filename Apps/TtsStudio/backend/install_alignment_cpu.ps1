# Optional genuine WhisperX force alignment (separate CPU environment).
# First explicit forced-alignment invocation may download language model weights.
$ErrorActionPreference = 'Stop'
$backend = Split-Path -Parent $MyInvocation.MyCommand.Path
$venv = Join-Path $backend '.venv-align'
$python = Join-Path $venv 'Scripts\python.exe'
if (-not (Test-Path $python)) {
    py -3.11 -m venv $venv
    if ($LASTEXITCODE -ne 0) { throw 'Python 3.11 is required; please install it.' }
}
& $python -m pip install --disable-pip-version-check --upgrade pip
if ($LASTEXITCODE -ne 0) { throw 'pip upgrade failed' }
& $python -m pip install --disable-pip-version-check whisperx
if ($LASTEXITCODE -ne 0) { throw 'WhisperX installation failed' }
& $python -c 'import whisperx; print("WhisperX alignment environment ready")'
if ($LASTEXITCODE -ne 0) { throw 'WhisperX import check failed' }
Write-Host 'Alignment environment is installed. Language model loaded on demand.'
