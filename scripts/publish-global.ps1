# Publica o MCP em uma pasta fixa para uso global no Cursor (sem abrir este repo).
param(
    [string]$OutputPath = "$env:LOCALAPPDATA\MeuRunrunItMCP",
    [switch]$SkipSecretsCheck,
    [switch]$ForceNewSecrets,
    [switch]$SkipProjectRoot
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ProjectFile = Join-Path $ProjectRoot "MeuRunrunItMCP.csproj"

. (Join-Path $PSScriptRoot "RunrunItSecrets.ps1")

if (-not $SkipSecretsCheck) {
    Ensure-RunrunItSecrets -ProjectFile $ProjectFile -SkipProjectRoot:$SkipProjectRoot -ForceNew:$ForceNewSecrets
}

Write-Host ""
Write-Host "Publicando MeuRunrunItMCP em Release..." -ForegroundColor Cyan
if (Test-Path $OutputPath) {
    Write-Host "Limpando pasta de publicacao (evita DLLs antigas de publish anterior)..." -ForegroundColor DarkGray
    Remove-Item (Join-Path $OutputPath "*") -Recurse -Force
}
dotnet publish $ProjectFile -c Release -o $OutputPath

$dll = Join-Path $OutputPath "MeuRunrunItMCP.dll"
if (-not (Test-Path $dll)) {
    throw "Publicacao falhou: $dll nao encontrado."
}

Write-Host ""
Write-Host "Publicado com sucesso em:" -ForegroundColor Green
Write-Host "  $OutputPath"
Write-Host ""
Write-Host "Adicione ao arquivo %USERPROFILE%\.cursor\mcp.json :" -ForegroundColor Cyan
Write-Host ""
$dllForJson = $dll -replace '\\', '\\\\'
Write-Host @"
{
  "mcpServers": {
    "meu-runrunit": {
      "command": "dotnet",
      "args": ["exec", "$dllForJson"]
    }
  }
}
"@
Write-Host ""
Write-Host "Runtime: net8.0 com roll-forward Major (roda em maquinas com .NET 8, 9 ou 10 instalado)."
Write-Host "Credenciais: User Secrets (nao e necessario reconfigurar a cada publish, salvo se escolher novas)."
Write-Host ""
