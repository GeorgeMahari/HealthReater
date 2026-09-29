# Starts the HealthRater backend API and frontend dev server, each in its own window.
# Usage (from the project root):  powershell -ExecutionPolicy Bypass -File .\start-dev.ps1
# A server that is already listening on its port is left alone.

$root = $PSScriptRoot

function Test-Port([int]$port) {
    [bool](Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue)
}

function Start-Window([string]$title, [string]$dir, [string]$command) {
    Start-Process -FilePath "powershell.exe" -WorkingDirectory $dir `
        -ArgumentList "-NoExit", "-Command", "`$Host.UI.RawUI.WindowTitle = '$title'; $command"
}

if (Test-Port 5080) {
    Write-Host "Backend already running on http://localhost:5080"
} else {
    Write-Host "Starting backend  -> http://localhost:5080  (Swagger: /swagger)"
    Start-Window "HealthRater API (5080)" (Join-Path $root "backend\HealthRater.Api") "dotnet run"
}

if (Test-Port 5173) {
    Write-Host "Frontend already running on http://localhost:5173"
} else {
    Write-Host "Starting frontend -> http://localhost:5173"
    Start-Window "HealthRater frontend (5173)" (Join-Path $root "frontend") "npm run dev"
}

Write-Host "Close a server's window to stop it."
