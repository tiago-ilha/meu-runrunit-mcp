# Configura credenciais Runrun.it (User Secrets) de forma interativa.
# Uso: .\scripts\configure.ps1
param(
    [switch]$SkipProjectRoot
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$ProjectFile = Join-Path $ProjectRoot "MeuRunrunItMCP.csproj"

if (-not (Test-Path $ProjectFile)) {
    throw "Projeto nao encontrado: $ProjectFile"
}

function Set-Secret([string]$Name, [string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) { return }
    dotnet user-secrets set $Name $Value --project $ProjectFile | Out-Null
    Write-Host "  OK: $Name"
}

Write-Host ""
Write-Host "=== Configuracao Meu Runrun.it MCP ===" -ForegroundColor Cyan
Write-Host ""
Write-Host "As credenciais ficam em User Secrets (fora do Git)."
Write-Host "Obtenha App-Key e User-Token em: Runrun.it -> Configuracoes -> Integracoes -> API"
Write-Host ""

$appKey = Read-Host "App-Key"
if ([string]::IsNullOrWhiteSpace($appKey)) {
    Write-Host "App-Key obrigatorio. Cancelado." -ForegroundColor Yellow
    exit 1
}

$userToken = Read-Host "User-Token" -AsSecureString
$userTokenPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($userToken))

if ([string]::IsNullOrWhiteSpace($userTokenPlain)) {
    Write-Host "User-Token obrigatorio. Cancelado." -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Gravando secrets..." -ForegroundColor Cyan
Set-Secret "RunrunIt:AppKey" $appKey.Trim()
Set-Secret "RunrunIt:UserToken" $userTokenPlain.Trim()

if (-not $SkipProjectRoot) {
    Write-Host ""
    $setRoot = Read-Host "Definir pasta padrao do codigo (CodeAnalysis:ProjectRoot)? [s/N]"
    if ($setRoot -match '^[sS]') {
        $defaultRoot = Read-Host "Caminho absoluto da raiz do repositorio (Enter para pular)"
        Set-Secret "CodeAnalysis:ProjectRoot" $defaultRoot.Trim()
    }
}

Write-Host ""
Write-Host "Secrets configurados:" -ForegroundColor Green
dotnet user-secrets list --project $ProjectFile

Write-Host ""
Write-Host "Proximos passos:" -ForegroundColor Cyan
Write-Host "  1. .\scripts\publish-global.ps1   (se ainda nao publicou)"
Write-Host "  2. Configure %USERPROFILE%\.cursor\mcp.json (sem bloco env)"
Write-Host "  3. Reload Window no Cursor"
Write-Host ""
