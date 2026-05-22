param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

$csproj = Join-Path $root 'MeuRunrunItMCP.csproj'
$packageJson = Join-Path $root 'package.json'

if (-not (Test-Path $csproj)) {
    throw "Arquivo não encontrado: $csproj"
}

$content = Get-Content $csproj -Raw
$versionTag = "<Version>$Version</Version>"

if ($content -match '<Version>[^<]+</Version>') {
    $content = $content -replace '<Version>[^<]+</Version>', $versionTag
} else {
    $content = $content -replace '(<PropertyGroup>\s*)', "`$1`r`n    $versionTag`r`n"
}

Set-Content -Path $csproj -Value $content.TrimEnd() -NoNewline -Encoding utf8

if (Test-Path $packageJson) {
    $pkgContent = Get-Content $packageJson -Raw
    $pkgContent = $pkgContent -replace '"version"\s*:\s*"[^"]+"', "`"version`": `"$Version`""
    Set-Content -Path $packageJson -Value $pkgContent.TrimEnd() -NoNewline -Encoding utf8
}

Write-Host "Versão definida para $Version em MeuRunrunItMCP.csproj e package.json"
