# Funcoes compartilhadas para User Secrets do Runrun.it (App-Key / User-Token).
# Uso: . "$PSScriptRoot\RunrunItSecrets.ps1"

function Get-RunrunItSecretsStatus {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectFile
    )

    $listOutput = dotnet user-secrets list --project $ProjectFile 2>$null
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($listOutput)) {
        return [pscustomobject]@{
            HasAppKey = $false
            HasUserToken = $false
            IsComplete = $false
            RawList = @()
        }
    }

    $lines = $listOutput -split "`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    $hasAppKey = $false
    $hasUserToken = $false

    foreach ($line in $lines) {
        if ($line -match '^\s*RunrunIt:AppKey\s*=') { $hasAppKey = $true }
        if ($line -match '^\s*RunrunIt:UserToken\s*=') { $hasUserToken = $true }
    }

    return [pscustomobject]@{
        HasAppKey = $hasAppKey
        HasUserToken = $hasUserToken
        IsComplete = ($hasAppKey -and $hasUserToken)
        RawList = $lines
    }
}

function Show-RunrunItSecretsSummary {
    param(
        [Parameter(Mandatory = $true)]
        $Status
    )

    Write-Host "Secrets Runrun.it encontrados:" -ForegroundColor Cyan
    if ($Status.HasAppKey) {
        Write-Host "  - RunrunIt:AppKey (configurado)"
    } else {
        Write-Host "  - RunrunIt:AppKey (ausente)" -ForegroundColor Yellow
    }

    if ($Status.HasUserToken) {
        Write-Host "  - RunrunIt:UserToken (configurado)"
    } else {
        Write-Host "  - RunrunIt:UserToken (ausente)" -ForegroundColor Yellow
    }
}

function Set-RunrunItSecret {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectFile,
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [string]$Value
    )

    if ([string]::IsNullOrWhiteSpace($Value)) { return }
    dotnet user-secrets set $Name $Value --project $ProjectFile | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Falha ao gravar secret: $Name"
    }
    Write-Host "  OK: $Name" -ForegroundColor Green
}

function Invoke-RunrunItSecretsPrompt {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectFile,
        [switch]$SkipProjectRoot
    )

    Write-Host ""
    Write-Host "=== Credenciais Runrun.it (User Secrets) ===" -ForegroundColor Cyan
    Write-Host "Armazenadas em %APPDATA%\Microsoft\UserSecrets\ (persistem entre publishes)."
    Write-Host "Obtenha em: Runrun.it -> Configuracoes -> Integracoes -> API"
    Write-Host ""

    $appKey = Read-Host "App-Key"
    if ([string]::IsNullOrWhiteSpace($appKey)) {
        throw "App-Key obrigatorio."
    }

    $userToken = Read-Host "User-Token" -AsSecureString
    $userTokenPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($userToken))

    if ([string]::IsNullOrWhiteSpace($userTokenPlain)) {
        throw "User-Token obrigatorio."
    }

    Write-Host ""
    Write-Host "Gravando secrets..." -ForegroundColor Cyan
    Set-RunrunItSecret -ProjectFile $ProjectFile -Name "RunrunIt:AppKey" -Value $appKey.Trim()
    Set-RunrunItSecret -ProjectFile $ProjectFile -Name "RunrunIt:UserToken" -Value $userTokenPlain.Trim()

    if (-not $SkipProjectRoot) {
        Write-Host ""
        $setRoot = Read-Host "Definir pasta padrao (CodeAnalysis:ProjectRoot)? [s/N]"
        if ($setRoot -match '^[sS]') {
            $defaultRoot = Read-Host "Caminho absoluto da raiz do repositorio (Enter para pular)"
            Set-RunrunItSecret -ProjectFile $ProjectFile -Name "CodeAnalysis:ProjectRoot" -Value $defaultRoot.Trim()
        }
    }
}

function Ensure-RunrunItSecrets {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectFile,
        [switch]$SkipProjectRoot,
        [switch]$ForceNew
    )

    if (-not (Test-Path $ProjectFile)) {
        throw "Projeto nao encontrado: $ProjectFile"
    }

    $status = Get-RunrunItSecretsStatus -ProjectFile $ProjectFile

    if ($ForceNew) {
        Write-Host ""
        Write-Host "Reconfigurando credenciais Runrun.it..." -ForegroundColor Cyan
        Invoke-RunrunItSecretsPrompt -ProjectFile $ProjectFile -SkipProjectRoot:$SkipProjectRoot
        return
    }

    if (-not $status.IsComplete) {
        Write-Host ""
        Write-Host "Credenciais Runrun.it nao encontradas (ou incompletas)." -ForegroundColor Yellow
        Invoke-RunrunItSecretsPrompt -ProjectFile $ProjectFile -SkipProjectRoot:$SkipProjectRoot
        return
    }

    Write-Host ""
    Show-RunrunItSecretsSummary -Status $status
    Write-Host ""
    Write-Host "As credenciais permanecem as mesmas apos republicar a DLL (User Secrets nao ficam na pasta publish)."
    Write-Host ""
    $choice = Read-Host "Usar secrets existentes [S] ou informar novas credenciais [N]? (padrao: S)"

    if ($choice -match '^[nN]') {
        Invoke-RunrunItSecretsPrompt -ProjectFile $ProjectFile -SkipProjectRoot:$SkipProjectRoot
    } else {
        Write-Host "Mantendo secrets existentes." -ForegroundColor Green
    }
}
