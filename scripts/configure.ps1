# Configura credenciais Runrun.it (User Secrets) de forma interativa.
# Uso: .\scripts\configure.ps1
param(
    [switch]$SkipProjectRoot,
    [switch]$ForceNew
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ProjectFile = Join-Path $ProjectRoot "MeuRunrunItMCP.csproj"

. (Join-Path $PSScriptRoot "RunrunItSecrets.ps1")

if ($ForceNew) {
    Invoke-RunrunItSecretsPrompt -ProjectFile $ProjectFile -SkipProjectRoot:$SkipProjectRoot
} else {
    Ensure-RunrunItSecrets -ProjectFile $ProjectFile -SkipProjectRoot:$SkipProjectRoot
}

Write-Host ""
Write-Host "Secrets atuais:" -ForegroundColor Green
dotnet user-secrets list --project $ProjectFile

Write-Host ""
Write-Host "Proximos passos:" -ForegroundColor Cyan
Write-Host "  1. .\scripts\publish-global.ps1"
Write-Host "  2. Configure %USERPROFILE%\.cursor\mcp.json (sem bloco env)"
Write-Host "  3. Reload Window no Cursor"
Write-Host ""
