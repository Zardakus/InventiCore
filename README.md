<div align="center">
  <img src="https://cdn-icons-png.flaticon.com/512/3613/3613344.png" alt="Logo" width="80" height="80">
  <h1 align="center">InventiCore SaaS</h1>
  <p align="center">
    O sistema de gestÃ£o de estoques distribuÃ­dos corporativo (B2B) definitivo. ConstruÃ­do com as melhores prÃ¡ticas de engenharia para aguentar escala, auditoria e automaÃ§Ã£o via InteligÃªncia Artificial.
  </p>
  
  [![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
  [![Blazor WebAssembly](https://img.shields.io/badge/Blazor-WASM-512BD4?logo=blazor)](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor)
  [![Clean Architecture](https://img.shields.io/badge/Architecture-Clean-success)](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
  [![MediatR](https://img.shields.io/badge/CQRS-MediatR-blue)](#)
  [![Entity Framework Core](https://img.shields.io/badge/ORM-EF%20Core-blue)](#)
  [![PostgreSQL](https://img.shields.io/badge/Database-PostgreSQL-336791?logo=postgresql)](#)
  [![RabbitMQ](https://img.shields.io/badge/Message_Broker-RabbitMQ-FF6600?logo=rabbitmq)](#)
  [![MassTransit](https://img.shields.io/badge/Event_Driven-MassTransit-lightgrey)](#)
  [![Serilog](https://img.shields.io/badge/Observability-Serilog-yellow)](#)
  [![Docker](https://img.shields.io/badge/Containers-Docker-2496ED?logo=docker)](#)
</div>

---

## âš™ï¸  Arquitetura (VisÃ£o Geral)

A plataforma InventiCore implementa Clean Architecture rigorosa e separaÃ§Ã£o de responsabilidades em CQRS. Ela conta com Multi-Tenancy (Isolamento nÃ­vel de Banco de Dados) garantindo a seguranÃ§a para aplicaÃ§Ãµes SaaS (Software as a Service) B2B.

O diferencial estratÃ©gico Ã© a interface acoplada de **Model Context Protocol (MCP)**, permitindo que Agentes de InteligÃªncia Artificial controlem o domÃ­nio de estoques atravÃ©s do servidor MCP como "Trabalhadores AutÃ´nomos".

```mermaid
graph TD
    %% Core Services
    subgraph Frontend
        BLZ[Blazor WebAssembly App]
    end

    subgraph Backend
        API[ASP.NET Core API]
    end

    subgraph Infrastructure
        PG[(PostgreSQL)]
        RMQ{{RabbitMQ}}
    end

    subgraph Observability
        LOG[Serilog JSON Files]
    end

    subgraph Agents
        MCP[InventiCore.Mcp Server]
        AI[Agent LLM / Roo / Cline]
    end
    
    subgraph Integrations
        DC[Discord Webhooks]
    end

    %% ConexÃµes
    BLZ -- "JWT HTTP" --> API
    API -- "EF Core" --> PG
    API -- "Publish Event (MassTransit)" --> RMQ
    RMQ -- "Consume Event" --> API
    API -- "Async Log" --> LOG
    API -- "Send Alert" --> DC
    
    AI -- "JSON-RPC (STDIO)" --> MCP
    MCP -- "Commands/Queries (MediatR)" --> PG
    MCP -- "Publish Event" --> RMQ
```

## âœ¨ Funcionalidades Principais

* **Isolamento de Tenants:** SeparaÃ§Ã£o total dos dados por cliente atravÃ©s da arquitetura JWT Auth e interceptadores em todo o nÃ­vel de RepositÃ³rio EF Core.
* **Model Context Protocol:** Ferramentas corporativas expostas nativamente via STDIO (como `analyze_low_stock` e `execute_stock_transfer`) permitindo integraÃ§Ã£o *Plug & Play* com agentes e LLMs.
* **Audit Trail ImutÃ¡vel:** Todo processo de alteraÃ§Ã£o de estoque gera `StockMovement`, garantindo visÃ£o histÃ³rica e de conformidade.
* **Dashboards Reativos:** Resumos consolidados B2B com biblioteca `Radzen.Blazor`.
* **Mensageria Orientada a Eventos:** `MassTransit` ouvindo alertas crÃ­ticos como `StockLowEvent` para notificaÃ§Ãµes em tempo real.
* **Telemetria Centralizada:** Geração estruturada de JSON no Log, possibilitando consumo direto no Datadog/ELK, atrelado explicitamente ao `TenantId`.

## ðŸš€ Como rodar localmente

### 1. Requisitos
- [Docker](https://www.docker.com/) e Docker Compose
- [.NET 8 SDK](https://dotnet.microsoft.com/download) ou superior
- [Node.js](https://nodejs.org/en/) (opcional, para tools de desenvolvimento)

### 2. Subir a Infraestrutura (Postgres + RabbitMQ)
Abra a raiz do projeto e execute:
```bash
docker-compose up -d
```
Isso iniciarÃ¡ o PostgreSQL na porta `5433` e o RabbitMQ (com painel de gerenciamento) na `5672`/`15672`.

### 3. Subir a API
```bash
dotnet run --project src/InventiCore.Api/InventiCore.Api.csproj
```
A API ficarÃ¡ disponÃ­vel em: `http://localhost:5030`

### 4. Subir o Frontend (Blazor WASM)
Em outro terminal:
```bash
dotnet run --project src/InventiCore.Web/InventiCore.Web.csproj
```

> **Dica:** O usuÃ¡rio patrÃ£o para testes localmente pode ser invocado com login de `admin` e senha `admin` (conforme simulado em nosso ambiente local).

### 5. Ativar as NotificaÃ§Ãµes (Discord)
Adicione o endereÃ§o gerado no seu servidor do Discord no arquivo `appsettings.json` da `InventiCore.Api`:
```json
"Discord": {
  "WebhookUrl": "https://discord.com/api/webhooks/..."
}
```
---
<div align="center">
  <i>ConstruÃ­do por InteligÃªncia Artificial (Antigravity Agent) guiado por Engenharia Humana SÃªnior.</i>
</div>
