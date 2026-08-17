# InventiCore — Checklist de Processos (SOP)

> **Standard Operating Procedure**
> Este checklist deve ser consultado e seguido obrigatoriamente ANTES e DEPOIS de toda nova implementação ou correção.
> Última atualização: 2026-08-16

---

## Checklist Pré-Implementação

- [ ] **Consultar `Antigravity.md`**: verificar se a nova feature/fix respeita todas as decisões arquiteturais e regras de negócio documentadas.
- [ ] **Verificar isolamento de Tenant**: toda query/command que acessa dados deve filtrar por `TenantId`.
- [ ] **Verificar camada correta**: o código está sendo adicionado na camada certa da Clean Architecture?
  - Lógica de negócio → Domain
  - Orquestração (Commands/Queries) → Application
  - Persistência/Infraestrutura → Infrastructure
  - HTTP/Apresentação → Api
- [ ] **Verificar se já existe interface**: antes de criar uma implementação, verificar se a interface já está definida no Domain.

---

## Checklist de Implementação

### Phase 3: Integração e Segurança
- [x] RabbitMQ publisher (`IMessagePublisher`).
- [x] Background Worker (`StockLowDiscordAlertWorker`).
- [x] Autenticação JWT (`AddJwtBearer`).
- [x] `ICurrentUserService` extraindo `TenantId`.
- [x] Refatoração de Handlers (Segurança e Isolamento).

### Phase 4: CI/CD e Deployment
- [x] Workflow de CI/CD (GitHub Actions).

### Phase 5: Frontend e Consumo
- [x] Scaffold projeto Blazor WebAssembly (`InventiCore.Web`).
- [x] Injeção de `HttpClient` e Autenticação JWT (`CustomAuthStateProvider`).
- [x] Tela de Login e consumo de endpoint de produtos restritos via API (`[Authorize]`).

### Phase 6: Inteligência Artificial (MCP)
- [x] Criar servidor MCP nativo `InventiCore.Mcp`.
- [x] Expor `analyze_low_stock` via STDIO.
- [x] Expor `execute_stock_transfer` nativamente, validando Tenant Security Context via args.
- [x] Validar integração do MCP no Host (resolvido STDOUT corruption).

### Phase 7: UI & Dashboards
- [x] Setup `Radzen.Blazor`.
- [x] Rota de sumário Backend Otimizada via CQRS.
- [x] Construir Componente de `Dashboard` (Cards e Gráfico) consolidando `StockItems`.

### Phase 8: Gestão Operacional e CRUDs
- [x] Construir `Depositos.razor` (RadzenDataGrid com Create/Update/Delete in-line).
- [x] Atualizar `Produtos.razor` com paginação, sort e CRUD visual avançado.
- [x] Construir `Movimentacoes.razor` (Central de Operações: Entrada, Saída, Transferência).
- [x] Integrar tratamento de erros e notificações globais `RadzenNotification` (ex: interceptar 409 Conflict).
- [x] Atualizar Menu de Navegação.

### Evolução Autônoma (Passos 2 a 4)
- [x] **Refatoração UI/UX**: Migração das edições In-line de `Depositos` e `Produtos` para formulários flutuantes `RadzenDialog`, aderindo ao padrão de mercado B2B SaaS.
- [x] **Feature (Extrato)**: Nova funcionalidade de "Histórico de Movimentações", expondo *StockMovements* de forma cronológica via CQRS e renderizando em *RadzenDataGrid* com Badges visuais (Entrada/Saída/Transferência).
- [x] **Self-Healing**: Identificação de gargalos de build por bloqueio de arquivo no pipeline (Stop-Process forçado via PID) resolvendo a falha MSB3026.

### Phase 9: Qualidade e Cobertura de Testes
- [x] Criação do projeto `InventiCore.UnitTests` (xUnit).
- [x] Configuração das ferramentas de qualidade: `Moq` e `FluentAssertions`.
- [x] Testes de Domínio: Adição de métodos de regra de negócio (`AddQuantity`, `RemoveQuantity`) na entidade `StockItem` e validação com `InsufficientStockException`.
- [x] Testes de Aplicação: Cobertura do Handler `TransferStockCommandHandler` validando fluxos de sucesso e isolamento de Tenants.

### Phase 10: Observabilidade e Logs Estruturados (Telemetry)
- [x] Setup do `Serilog.AspNetCore`, `Serilog.Sinks.Console`, `Serilog.Sinks.Async`, `Serilog.Sinks.File` e `Serilog.Formatting.Compact`.
- [x] Configuração do `Serilog` via `Program.cs` para gerar logs assíncronos no console e em arquivo estruturado (`CompactJsonFormatter`).
- [x] Implementação de enriquecimento contextual `EnrichDiagnosticContext` injetando o `TenantId` do `ICurrentUserService` no evento de log.
- [x] Configuração da infraestrutura de logs do `InventiCore.Mcp` direcionada a arquivo para evitar poluição do `STDOUT` no protocolo MCP.

### Phase 11: Mensageria AvanÃ§ada e Vitrine do PortfÃ³lio
- [x] InstalaÃ§Ã£o do ecossistema `MassTransit` (`Abstractions`, `RabbitMQ`) substituindo as conexÃµes brutas legadas e implementando `IPublishEndpoint` para eventos de domÃ­nio.
- [x] RefatoraÃ§Ã£o dos Commands (`RemoveStockCommand`, `TransferStockCommand`) para emitirem o `StockLowEvent` pelo pipeline do MassTransit.
- [x] CriaÃ§Ã£o do Worker local `StockLowEventConsumer` no backend para interceptar mensagens no RabbitMQ, validar estoque, e realizar o envio HTTP assÃ­ncrono para Webhooks do Discord (`appsettings.json`).
- [x] CriaÃ§Ã£o da vitrine corporativa no `README.md` (Design SÃªnior, Badges e diagramaÃ§Ã£o em Mermaid detalhando a integraÃ§Ã£o entre CQRS, MCP e RabbitMQ).

### Phase 12: ResiliÃªncia e ProteÃ§Ã£o de Rede (Polly)
- [x] InstalaÃ§Ã£o do pacote `Microsoft.Extensions.Http.Polly` no projeto da API (`InventiCore.Api`).
- [x] ConfiguraÃ§Ã£o de polÃ­ticas de **Retry** (3 tentativas com *exponential backoff*) no `HttpClient` do `StockLowEventConsumer` para contornar falhas transientes e instabilidades no Discord (5xx e timeouts).
- [x] ImplementaÃ§Ã£o do patrÃ£o **Circuit Breaker** (abre circuito por 30s apÃ³s 3 falhas consecutivas) para evitar *resource exhaustion* do servidor em chamadas fadadas ao fracasso.

- [ ] **Criar/atualizar DTOs**: nunca expor entidades de domínio diretamente na API.
- [ ] **Criar Validator (se Command)**: todo Command deve ter um FluentValidation Validator correspondente.
- [ ] **Usar CancellationToken**: todos os métodos assíncronos devem propagar `CancellationToken`.
- [ ] **Tratar exceções no domínio**: usar exceções tipadas (`InsufficientStockException`, etc.) ao invés de exceções genéricas.
- [ ] **Registrar no DI**: se criou uma nova interface/implementação, registrar em `DependencyInjection.cs` da camada correta.

---

## Checklist Pós-Implementação

- [ ] **Build sem erros**: `dotnet build InventiCore.sln` deve compilar sem erros ou warnings relevantes.
- [ ] **Rodar testes** (quando existirem): `dotnet test` deve passar com sucesso.
- [ ] **Atualizar `Antigravity.md`**: se houve nova decisão arquitetural, regra de negócio ou mudança de stack, documentar.
- [ ] **Atualizar `Processos.md`**: se descobriu um novo processo necessário, adicionar ao checklist.
- [ ] **Commit seguindo Conventional Commits**:
  - `feat:` para novas funcionalidades
  - `fix:` para correções de bugs
  - `chore:` para manutenção (Docker, configs, deps)
  - `docs:` para documentação
  - `refactor:` para refatorações sem mudança de comportamento
- [ ] **Push para remote**: `git push origin main`

---

## Fluxo Resumido

```
1. Consultar Antigravity.md (regras e decisões)
       ↓
2. Implementar na camada correta
       ↓
3. Criar DTOs + Validators + Registrar no DI
       ↓
4. dotnet build (zero erros)
       ↓
5. dotnet test (se aplicável)
       ↓
6. Atualizar documentação (Antigravity.md / Processos.md)
       ↓
7. git add → git commit (Conventional Commits) → git push
```
