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

function Stop-MeuRunrunItMcpProcesses {
    $stopped = 0

    Get-Process -Name "MeuRunrunItMCP" -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Host "Encerrando MeuRunrunItMCP (PID $($_.Id))..." -ForegroundColor DarkYellow
        Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
        $stopped++
    }

    Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -and $_.CommandLine -like '*MeuRunrunItMCP*' } |
        ForEach-Object {
            Write-Host "Encerrando dotnet do MCP (PID $($_.ProcessId))..." -ForegroundColor DarkYellow
            Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
            $stopped++
        }

    if ($stopped -gt 0) {
        Start-Sleep -Milliseconds 800
    }
}

function Clear-PublishOutputDirectory {
    param(
        [string]$Path,
        [int]$MaxAttempts = 4
    )

    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
        return
    }

    for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
        try {
            Get-ChildItem -Path $Path -Force -ErrorAction Stop |
                Remove-Item -Recurse -Force -ErrorAction Stop
            return
        }
        catch {
            if ($attempt -eq 1) {
                Stop-MeuRunrunItMcpProcesses
            }
            elseif ($attempt -eq $MaxAttempts) {
                throw @"
Nao foi possivel limpar $Path (arquivo em uso).
Desligue o servidor MCP no Cursor (Settings > MCP) e execute o script novamente.
"@
            }
            Start-Sleep -Seconds 1
        }
    }
}

Write-Host ""
Write-Host "Publicando MeuRunrunItMCP em Release..." -ForegroundColor Cyan

$stagingPath = Join-Path $env:TEMP "MeuRunrunItMCP-publish-$([Guid]::NewGuid().ToString('N'))"
try {
    dotnet publish $ProjectFile -c Release -o $stagingPath

    Stop-MeuRunrunItMcpProcesses
    Clear-PublishOutputDirectory -Path $OutputPath

    Write-Host "Copiando binarios para pasta global..." -ForegroundColor DarkGray
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    Copy-Item -Path (Join-Path $stagingPath '*') -Destination $OutputPath -Recurse -Force
}
finally {
    if (Test-Path $stagingPath) {
        Remove-Item $stagingPath -Recurse -Force -ErrorAction SilentlyContinue
    }
}

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
