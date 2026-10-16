# Run in PowerShell from any directory. VieNeu v3 Turbo ONNX/CPU.
# This isolated environment does NOT modify the existing Chatterbox/Edge venv.
$ErrorActionPreference = 'Stop'
$backend = Split-Path -Parent $MyInvocation.MyCommand.Path
$venv = Join-Path $backend '.venv-vieneu'
$python = Join-Path $venv 'Scripts\python.exe'
if (-not (Test-Path -LiteralPath $python)) {
    py -3.13 -m venv $venv
    if ($LASTEXITCODE -ne 0) { throw 'Python 3.13 environment creation failed.' }
}
& $python -m pip install --disable-pip-version-check vieneu
if ($LASTEXITCODE -ne 0) { throw 'VieNeu install failed. See pip output.' }
& $python -c "from importlib.metadata import version; print('VieNeu installed:',version('vieneu'))"
if ($LASTEXITCODE -ne 0) { throw 'VieNeu package verification failed.' }
Write-Host 'VieNeu installed. Restart TTS Create By DvaForDev and select vieneu_v3_turbo.'
Write-Host 'The model may be downloaded on the first generation; this can take time.'
