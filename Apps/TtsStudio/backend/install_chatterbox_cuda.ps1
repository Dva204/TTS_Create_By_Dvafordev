# Install Chatterbox Multilingual with the tested CUDA 12.4 build.
# Safe to rerun. Does not modify the legacy project, VieNeu, models or voice data.
$ErrorActionPreference = 'Stop'
$backend = Split-Path -Parent $MyInvocation.MyCommand.Path
$venv = Join-Path $backend '.venv'
$python = Join-Path $venv 'Scripts\python.exe'
if (-not (Test-Path -LiteralPath $python)) {
    py -3.13 -m venv $venv
    if ($LASTEXITCODE -ne 0) { throw 'Unable to create Chatterbox Python environment.' }
}
& $python -m pip install --disable-pip-version-check --index-url 'https://download.pytorch.org/whl/cu124' 'torch==2.6.0+cu124' 'torchaudio==2.6.0+cu124'
if ($LASTEXITCODE -ne 0) { throw 'CUDA PyTorch installation failed.' }
& $python -m pip install --disable-pip-version-check -r (Join-Path $backend 'requirements.txt')
if ($LASTEXITCODE -ne 0) { throw 'Chatterbox dependency installation failed.' }
& $python -c "import torch,torchaudio; from chatterbox.mtl_tts import ChatterboxMultilingualTTS; print('Chatterbox import OK; CUDA:',torch.cuda.is_available(),'Torch:',torch.__version__)"
if ($LASTEXITCODE -ne 0) { throw 'Chatterbox validation failed.' }
