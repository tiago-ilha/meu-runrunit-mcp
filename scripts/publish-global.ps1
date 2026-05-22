# Publica o MCP em uma pasta fixa para uso global no Cursor (sem abrir este repo).
param(
    [string]$OutputPath = "$env:LOCALAPPDATA\MeuRunrunItMCP"
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ProjectFile = Join-Path $ProjectRoot "MeuRunrunItMCP.csproj"

Write-Host "Publicando MeuRunrunItMCP em Release..."
dotnet publish $ProjectFile -c Release -o $OutputPath

$dll = Join-Path $OutputPath "MeuRunrunItMCP.dll"
if (-not (Test-Path $dll)) {
    throw "Publicacao falhou: $dll nao encontrado."
}

Write-Host ""
Write-Host "Publicado com sucesso em:"
Write-Host "  $OutputPath"
Write-Host ""
Write-Host "Adicione ao arquivo %USERPROFILE%\.cursor\mcp.json :"
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
Write-Host ""
Write-Host "Credenciais Runrun.it: configure com dotnet user-secrets na pasta do repo de desenvolvimento,"
Write-Host "ou use env no mcp.json (veja README)."
