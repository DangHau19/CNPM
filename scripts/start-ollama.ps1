param(
    [switch]$DockerAccess,
    [switch]$Restart,
    [string]$ModelsPath = 'D:\Tools\OllamaTrial\models',
    [string]$OllamaExe = 'D:\Tools\OllamaTrial\runtime\ollama.exe'
)
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $OllamaExe)) {
    $command = Get-Command ollama -ErrorAction SilentlyContinue
    if (!$command) { throw 'Install Ollama, or pass -OllamaExe with the path to ollama.exe.' }
    $OllamaExe = $command.Source
}
$listener = Get-NetTCPConnection -LocalPort 11434 -State Listen -ErrorAction SilentlyContinue
if ($listener -and $Restart) {
    $owners = @($listener.OwningProcess | Select-Object -Unique)
    if ($owners.Count -ne 1) { throw 'Multiple processes own port 11434. Stop the old server manually.' }
    $existing = Get-Process -Id $owners[0] -ErrorAction Stop
    $expected = (Resolve-Path -LiteralPath $OllamaExe).Path
    if ($existing.Path -ne $expected) { throw 'Port 11434 belongs to another installation. Stop that server manually.' }
    Write-Output "Restarting Ollama PID $($existing.Id)"
    Stop-Process -Id $existing.Id -ErrorAction Stop
    $existing.WaitForExit()
    $listener = $null
}
if ($listener) {
    if ($DockerAccess -and !($listener | Where-Object { $_.LocalAddress -in @('0.0.0.0', '::') })) {
        throw 'Ollama listens on localhost only. Wait for current translations to finish, then use -DockerAccess -Restart.'
    }
    Write-Output 'Ollama is already running on port 11434.'
    return
}
New-Item -ItemType Directory -Path $ModelsPath -Force | Out-Null
$env:OLLAMA_MODELS = $ModelsPath
$env:OLLAMA_HOST = if ($DockerAccess) { '0.0.0.0:11434' } else { '127.0.0.1:11434' }
$env:OLLAMA_NO_CLOUD = '1'
$env:OLLAMA_NUM_PARALLEL = '1'
$env:OLLAMA_MAX_LOADED_MODELS = '1'
Write-Output "Models: $ModelsPath"
Write-Output "Listening on $env:OLLAMA_HOST. Keep this terminal open; Ctrl+C stops the server."
& $OllamaExe serve
