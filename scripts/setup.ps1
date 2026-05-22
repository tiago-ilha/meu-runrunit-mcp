# Setup completo: credenciais + publish global (primeira instalacao).
param(
    [switch]$SkipPublish,
    [switch]$SkipProjectRoot
)

$ErrorActionPreference = "Stop"
$ScriptsDir = $PSScriptRoot

Write-Host ""
Write-Host "=== Setup Meu Runrun.it MCP ===" -ForegroundColor Cyan
Write-Host ""

& (Join-Path $ScriptsDir "configure.ps1") -SkipProjectRoot:$SkipProjectRoot

if (-not $SkipPublish) {
    Write-Host ""
    $publish = Read-Host "Publicar para uso global no Cursor agora? [S/n]"
    if ($publish -notmatch '^[nN]') {
        & (Join-Path $ScriptsDir "publish-global.ps1") -SkipSecretsCheck
    }
}

Write-Host ""
Write-Host "Setup concluido." -ForegroundColor Green
Write-Host ""
