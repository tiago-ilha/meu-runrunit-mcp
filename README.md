# Meu Runrun.it MCP

Servidor [MCP](https://modelcontextprotocol.io/) em C# que conecta o **Runrun.it** ao seu fluxo no Cursor: lê tarefas (com comentários) e ajuda a localizar arquivos em **qualquer repositório** que você indicar — MVC 5, ASP.NET Core, React, Node, etc.

Este repositório é só o **servidor MCP**. O código que você analisa fica em **outro projeto**, em outra pasta.

---

## Índice

1. [Visão geral](#visão-geral)
2. [Pré-requisitos](#pré-requisitos)
3. [Instalação global (sem abrir este projeto)](#instalação-global-sem-abrir-este-projeto) **recomendado**
4. [Instalação para desenvolvimento](#instalação-para-desenvolvimento)
5. [Como usar em outro projeto](#como-usar-em-outro-projeto)
6. [Fluxo de trabalho recomendado](#fluxo-de-trabalho-recomendado)
7. [Ferramentas disponíveis](#ferramentas-disponíveis)
8. [Exemplos de prompts no Cursor](#exemplos-de-prompts-no-cursor)
9. [Configuração opcional](#configuração-opcional)
10. [Solução de problemas](#solução-de-problemas)
11. [Build e desenvolvimento](#build-e-desenvolvimento)

---

## Visão geral

```text
┌─────────────┐     stdio      ┌──────────────────┐     HTTPS      ┌─────────────┐
│   Cursor    │ ◄────────────► │ meu-runrunit-mcp │ ◄────────────► │  Runrun.it  │
│  (agente)   │                │  (este repo)     │                │    API      │
└─────────────┘                └────────┬─────────┘                └─────────────┘
                                        │
                                        │ lê arquivos no disco
                                        ▼
                               ┌──────────────────┐
                               │  OUTRO PROJETO   │  ← projectRoot que você informa
                               │  (MVC, React…)   │
                               └──────────────────┘
```

O parâmetro **`projectRoot`** é o caminho absoluto da **raiz do repositório alvo** (onde está a `.sln`, `package.json`, `Web.config`, etc.). Não use a pasta do MCP.

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (ou compatível com `net10.0` do projeto)
- [Cursor](https://cursor.com/) com MCP habilitado
- Conta Runrun.it com **App-Key** e **User-Token** (Configurações → Integrações → API)
- O **outro projeto** clonado/local no disco, com caminho que você conhece (ex.: `C:\dev\cliente\portal-web`)

---

## Instalação global (sem abrir este projeto)

Você **não precisa** ter a pasta `meu-runrunit-mcp` aberta no Cursor. O MCP é registrado **uma vez** no arquivo global do usuário e fica disponível em **qualquer workspace** (MVC, React, API, etc.).

### Como o Cursor enxerga o MCP

| Arquivo | Escopo |
|---------|--------|
| `%USERPROFILE%\.cursor\mcp.json` | **Global** — vale para todos os projetos |
| `.cursor/mcp.json` na pasta do projeto | Só aquele workspace (opcional) |

Use o **global** para não repetir config em cada cliente.

### Passo 1 — Publicar o MCP em uma pasta fixa

No PowerShell, na pasta deste repositório (só na primeira vez ou após atualizar o código):

```powershell
cd C:\dev\projetos\trabalho\meu-runrunit-mcp
.\scripts\publish-global.ps1
```

Isso gera os binários em:

```text
%LOCALAPPDATA%\MeuRunrunItMCP\
  MeuRunrunItMCP.dll
  appsettings.json
  ...
```

Equivalente manual:

```bash
dotnet publish MeuRunrunItMCP.csproj -c Release -o "%LOCALAPPDATA%\MeuRunrunItMCP"
```

### Passo 2 — Configurar o Cursor (global)

Edite `%USERPROFILE%\.cursor\mcp.json`:

```json
{
  "mcpServers": {
    "meu-runrunit": {
      "command": "dotnet",
      "args": [
        "exec",
        "C:\\Users\\SEU_USUARIO\\AppData\\Local\\MeuRunrunItMCP\\MeuRunrunItMCP.dll"
      ]
    }
  }
}
```

Substitua `SEU_USUARIO` pelo seu usuário Windows. Exemplo completo com credenciais em variáveis de ambiente: [`docs/mcp.global.example.json`](docs/mcp.global.example.json).

### Passo 3 — Credenciais Runrun.it

**Opção A — User Secrets (recomendado se você mantém o repo de desenvolvimento)**

Configure uma vez no clone do MCP (o `UserSecretsId` é o mesmo após o publish):

```bash
cd C:\dev\projetos\trabalho\meu-runrunit-mcp
dotnet user-secrets set "RunrunIt:AppKey" "SUA_APP_KEY"
dotnet user-secrets set "RunrunIt:UserToken" "SEU_USER_TOKEN"
```

**Opção B — Variáveis no `mcp.json` (funciona sem abrir o repo)**

```json
"env": {
  "RunrunIt__AppKey": "sua-app-key",
  "RunrunIt__UserToken": "seu-user-token"
}
```

### Passo 4 — Usar em qualquer outro projeto

1. Abra no Cursor **apenas** o projeto do cliente (ex.: `C:\dev\acme\portal-web`).
2. No chat, chame as tools com `projectRoot` apontando para esse projeto.
3. O MCP sobe em segundo plano via `mcp.json` global — **não** precisa do workspace do MCP.

### Atualizar o MCP após mudanças no código

```powershell
cd C:\dev\projetos\trabalho\meu-runrunit-mcp
.\scripts\publish-global.ps1
```

Reinicie o Cursor (ou desligue/ligue o servidor MCP nas configurações).

### Comparação das formas de instalação

| Modo | Comando no `mcp.json` | Precisa repo aberto? | Velocidade |
|------|------------------------|----------------------|------------|
| **Publicado (recomendado)** | `dotnet exec ...\MeuRunrunItMCP.dll` | Não | Mais rápido |
| `dotnet run --project` | Caminho absoluto do `.csproj` | Não (só pasta no disco) | Compila ao iniciar |
| Workspace local | `.cursor/mcp.json` no projeto | Só se usar config local | — |

---

## Instalação para desenvolvimento

Use esta seção se você está **alterando o código** do MCP. Para só **usar** em outros projetos, prefira [Instalação global](#instalação-global-sem-abrir-este-projeto).

### 1. Clone ou mantenha este repositório

Exemplo:

```text
C:\dev\projetos\trabalho\meu-runrunit-mcp
```

### 2. Credenciais Runrun.it (User Secrets)

No terminal, na pasta do MCP:

```bash
cd C:\dev\projetos\trabalho\meu-runrunit-mcp

dotnet user-secrets set "RunrunIt:AppKey" "SUA_APP_KEY"
dotnet user-secrets set "RunrunIt:UserToken" "SEU_USER_TOKEN"
```

### 3. Registre o MCP no Cursor (modo desenvolvimento)

Em `%USERPROFILE%\.cursor\mcp.json`, apontando para o `.csproj` (caminho absoluto):

```json
{
  "mcpServers": {
    "meu-runrunit": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\dev\\projetos\\trabalho\\meu-runrunit-mcp\\MeuRunrunItMCP.csproj"
      ]
    }
  }
}
```

Para uso diário em outros projetos, prefira a [instalação global publicada](#passo-1--publicar-o-mcp-em-uma-pasta-fixa).

### 4. Reinicie o Cursor

Feche e abra o Cursor (ou recarregue a janela) para o servidor MCP subir.

### 5. Confirme que as tools aparecem

No chat, o agente deve listar ferramentas como:

- `analyze_task_against_code`
- `get_task_context`
- `search`
- `read_file`
- `validate_project_root`

---

## Como usar em outro projeto

Você **não copia** o MCP para dentro do outro projeto. Você **aponta** o MCP para a pasta dele em cada uso.

### O que é `projectRoot`

| Correto | Incorreto |
|---------|-----------|
| `C:\dev\cliente-a\portal-mvc5` (raiz do app) | `C:\dev\projetos\trabalho\meu-runrunit-mcp` |
| Pasta que contém `Controllers/`, `src/`, `.sln`, etc. | Pasta `bin\` ou `node_modules\` |

Use sempre caminho **absoluto** no Windows (`C:\...`).

### Prioridade do caminho

1. **`projectRoot` na chamada da tool** (recomendado para vários clientes/repos)
2. **`CodeAnalysis:ProjectRoot`** na config (atalho se você usa sempre o mesmo repo)
3. Se faltar os dois → erro pedindo o caminho

### Abrir o “outro projeto” no Cursor

Duas formas comuns:

**A) Workspace só do projeto alvo**  
Abra a pasta do cliente no Cursor e, no chat, informe o mesmo caminho em `projectRoot`.

**B) Workspace com MCP + projeto**  
Abra uma pasta pai ou multi-root; no prompt, passe o `projectRoot` absoluto do repo que quer analisar.

O MCP lê o disco diretamente — não depende de qual pasta está aberta no editor, desde que o caminho exista e seja legível.

### Passo a passo rápido

1. Pegue o **ID da tarefa** no Runrun.it (número na URL, ex.: `.../tasks/12345` → `12345`).
2. Defina o **caminho absoluto** do repositório alvo.
3. No Cursor, peça algo como:

```text
Use analyze_task_against_code com taskId 12345 e projectRoot C:\dev\cliente-a\portal-mvc5.
Depois diga quais arquivos devo alterar e por quê.
```

4. Se precisar de detalhe, peça `read_file` com o mesmo `projectRoot` e o caminho relativo retornado na busca.

---

## Fluxo de trabalho recomendado

### Cenário: nova tarefa em um projeto MVC 5

```text
1. validate_project_root  → confirma que o caminho existe
2. analyze_task_against_code → tarefa + comentários + lista de arquivos
3. read_file               → trechos dos arquivos mais relevantes
4. (você / agente)         → plano de implementação e código
```

### Cenário: só ler a tarefa (sem código)

```text
get_task_context com taskId 12345
```

### Cenário: outro repositório no mesmo dia

Troque apenas o `projectRoot`; credenciais Runrun.it são as mesmas:

```text
analyze_task_against_code taskId 200 projectRoot C:\dev\cliente-b\api-node
```

---

## Ferramentas disponíveis

| Ferramenta | Quando usar | Parâmetros principais |
|------------|-------------|------------------------|
| `analyze_task_against_code` | Fluxo completo (tarefa + busca no código) | `taskId`, **`projectRoot`**, `extraQuery?`, `maxResults?` |
| `get_task_context` | Só título, descrição e comentários | `taskId` |
| `search` | Busca manual por termos | **`projectRoot`**, `query?`, `taskContext?`, `maxResults?` |
| `read_file` | Ler trecho de um arquivo | **`projectRoot`**, `relativePath`, `startLine?`, `endLine?` |
| `validate_project_root` | Testar caminho antes de analisar | `projectRoot?` |

Tipos de arquivo considerados na busca (entre outros): `.cs`, `.cshtml`, `.js`, `.ts`, `.tsx`, `.vue`, `.py`, `.java`, `.go`, `.json`, `.xml`, `.config`, `.sql`, `.md`.

Pastas ignoradas: `bin`, `obj`, `node_modules`, `.git`, `dist`, `packages`, etc.

---

## Exemplos de prompts no Cursor

Substitua `TASK_ID`, caminhos e nomes pelos seus valores reais.

### ASP.NET MVC 5 (Full Framework)

```text
Valide projectRoot C:\dev\acme\MeuPortalMvc5.
Em seguida use analyze_task_against_code na tarefa TASK_ID com esse projectRoot.
Liste controllers e views impactados e um plano de alteração.
```

### ASP.NET Core

```text
analyze_task_against_code taskId TASK_ID projectRoot C:\dev\acme\MinhaApiCore
```

### Frontend React / Node

```text
analyze_task_against_code taskId TASK_ID projectRoot C:\dev\acme\frontend
extraQuery "checkout pagamento"
```

### Ler arquivo específico após a análise

```text
Use read_file com projectRoot C:\dev\acme\MeuPortalMvc5,
relativePath Areas/Vendas/Controllers/PedidoController.cs e startLine 1 endLine 120.
```

### Vários projetos na mesma sessão

```text
Primeiro analise a tarefa 100 no projeto C:\dev\projeto-a.
Depois analise a tarefa 200 no projeto C:\dev\projeto-b.
Use analyze_task_against_code em cada um com o projectRoot correto.
```

---

## Configuração opcional

Arquivo `appsettings.json` (valores vazios por padrão):

```json
{
  "RunrunIt": {
    "BaseUrl": "https://runrun.it/api/v1.0",
    "AppKey": "",
    "UserToken": ""
  },
  "CodeAnalysis": {
    "ProjectRoot": "",
    "MaxSearchResults": 25,
    "MaxFileReadLines": 200,
    "MaxTerms": 30
  }
}
```

### Repositório padrão (opcional)

Se você trabalha **sempre** no mesmo repo e não quer repetir o caminho:

```bash
dotnet user-secrets set "CodeAnalysis:ProjectRoot" "C:\dev\meu-repo-padrao"
```

No chat pode omitir `projectRoot` — o MCP usa esse padrão. Para outro projeto, passe `projectRoot` na chamada (ele tem prioridade).

### Variáveis de ambiente (alternativa)

No `mcp.json`, se preferir não usar User Secrets:

```json
{
  "mcpServers": {
    "meu-runrunit": {
      "command": "dotnet",
      "args": ["run", "--project", "C:\\...\\MeuRunrunItMCP.csproj"],
      "env": {
        "RunrunIt__AppKey": "sua-key",
        "RunrunIt__UserToken": "seu-token",
        "CodeAnalysis__ProjectRoot": "C:\\dev\\repo-opcional"
      }
    }
  }
}
```

---

## Solução de problemas

| Problema | Causa provável | O que fazer |
|----------|----------------|-------------|
| `Credenciais Runrun.it ausentes` | App-Key / User-Token não configurados | `dotnet user-secrets set` ou `env` no `mcp.json` |
| `Informe projectRoot...` | Caminho não passado e config vazia | Informe `projectRoot` absoluto no prompt |
| `ProjectRoot não encontrado` | Caminho errado ou pasta não existe | Confira o disco; use `validate_project_root` |
| `não parece um repositório de código` | Pasta vazia ou só documentação | Aponte para a raiz da solution/app |
| Nenhum arquivo na busca | Termos da tarefa muito genéricos | Use `extraQuery` com nomes de tela, controller, módulo |
| Tools não aparecem no Cursor | MCP não carregou | Verifique `mcp.json`, `dotnet build`, reinicie o Cursor |
| Erro ao compilar o MCP | SDK incompatível | Instale .NET 10 SDK |

---

## Build e desenvolvimento

```bash
cd C:\dev\projetos\trabalho\meu-runrunit-mcp
dotnet build MeuRunrunItMCP.slnx
```

Executar localmente (teste):

```bash
dotnet run --project MeuRunrunItMCP.csproj
```

## API Runrun.it

- `GET /tasks/{id}` — detalhe da tarefa (inclui `description` quando disponível)
- `GET /tasks/{id}/comments` — comentários da tarefa

Documentação oficial: https://runrun.it/api/documentation

---

## Resumo

| Pergunta | Resposta |
|----------|----------|
| Preciso abrir o projeto do MCP no Cursor? | **Não.** Use `mcp.json` global + publish. |
| Preciso instalar o MCP dentro do outro projeto? | **Não.** Só `projectRoot` no prompt. |
| Como indico o outro projeto? | Parâmetro **`projectRoot`** com caminho absoluto. |
| Posso usar em vários projetos? | **Sim.** Mude o `projectRoot` a cada tarefa/cliente. |
| O MCP analisa só MVC 5? | **Não.** Qualquer repo com arquivos-fonte suportados. |
| Onde ficam as credenciais Runrun.it? | User Secrets ou `env` no `mcp.json` global. |
