# InventiCore — Memória do Projeto (Antigravity.md)

> **Este arquivo é a memória de longo prazo da IA.**
> Deve ser mantido constantemente atualizado a cada nova demanda, decisão arquitetural ou refatoração.
> Última atualização: 2026-08-16

---

## 1. Stack Tecnológica

| Componente         | Tecnologia / Versão                     |
|--------------------|-----------------------------------------|
| Runtime            | .NET 8 (LTS) — SDK 9.0.305             |
| Linguagem          | C# 12                                  |
| Web Framework      | ASP.NET Core 8 (Minimal Hosting)        |
| ORM                | Entity Framework Core 8.0.11            |
| Banco de Dados     | PostgreSQL 16 (Docker Alpine)           |
| CQRS / Mediator    | MediatR 12.4.1                          |
| Validação          | FluentValidation 11.11.0               |
| Logging            | Serilog (Console sink)                  |
| API Docs           | Swashbuckle (Swagger/OpenAPI)           |
| Container Runtime  | Docker (docker-compose.yml)             |
| Cache (futuro)     | Redis 7 (Alpine)                        |
| Mensageria (futuro)| RabbitMQ 3 (Management Alpine)          |
| Versionamento      | Git + GitHub                            |

---

## 2. Decisões Arquiteturais

### 2.1 Clean Architecture
O projeto segue Clean Architecture com 4 camadas:

```
InventiCore.Domain         → Entidades, Enums, Exceções de Domínio, Interfaces de Repositório
InventiCore.Application    → CQRS (Commands/Queries), DTOs, Validators, Pipeline Behaviors
InventiCore.Infrastructure → EF Core, Repositórios, Persistência, Serviços Externos
InventiCore.Api            → Controllers, Middleware, Configuração do Host
```

**Regra de dependência**: Domain ← Application ← Infrastructure ← Api.
Domain não referencia nenhum outro projeto.

### 2.2 CQRS com MediatR
- Commands alteram estado (Create, Update, Delete).
- Queries apenas leem dados.
- Todos passam pelo pipeline do MediatR, permitindo cross-cutting concerns via Behaviors.

### 2.3 Repository Pattern + Unit of Work
- `IRepository<T>` genérico com operações CRUD básicas.
- Repositórios específicos (`IProductRepository`, etc.) para queries especializadas.
- `IUnitOfWork` coordena a transação atômica com `SaveChangesAsync`.

### 2.4 Mapper Manual (sem AutoMapper)
- Mapeamentos Entity ↔ DTO feitos manualmente via classes estáticas em `Common/Mappings/`.
- Decisão: manter simplicidade, controle explícito e evitar dependência extra.

### 2.5 Validação via Pipeline Behavior
- `ValidationBehavior<TRequest, TResponse>` intercepta todos os Commands/Queries.
- Usa FluentValidation para validar antes de executar o Handler.
- Validators são descobertos automaticamente via DI por assembly scanning.

### 2.6 Global Exception Handler
- Middleware centralizado que captura exceções e retorna `ProblemDetails` (RFC 7807).
- Mapeamento: `ValidationException` → 400, `KeyNotFoundException` → 404, `InsufficientStockException` → 409, outros → 500.

### 2.7 Multi-Tenancy
- Modelo: **column-level isolation** (coluna `TenantId` nas entidades).
- SKU é único por Tenant (índice composto `TenantId + Sku`).
- Cada Tenant tem seus próprios Products, Warehouses, StockItems.

### 2.8 Concorrência Otimista
- `StockItem.RowVersion` usa `xmin` do PostgreSQL como concurrency token.
- Previne conflitos de escrita simultânea em ajustes de estoque.

### 2.9 Audit Trail (StockMovement)
- Tabela append-only — NUNCA sofre UPDATE ou DELETE.
- Todo movimento de estoque (Entry, Exit, Transfer, Adjustment) gera um registro imutável.

---

## 3. Regras de Negócio Estritas

> **Estas regras NÃO podem ser violadas em nenhuma implementação.**

1. **StockMovement é imutável**: uma vez criado, não pode ser editado ou excluído.
2. **SKU é único por Tenant**: não é possível ter dois produtos com o mesmo SKU dentro do mesmo Tenant.
3. **Estoque não pode ficar negativo**: toda saída deve validar `Quantity >= solicitado`, senão lançar `InsufficientStockException`.
4. **Um Product só existe em um Warehouse via StockItem**: o par `(ProductId, WarehouseId)` é único.
5. **Soft Delete é preferido**: entidades com campo `IsActive` devem ser desativadas, não excluídas fisicamente (exceto em casos explícitos).
6. **Todo dado pertence a um Tenant**: queries devem sempre filtrar por `TenantId` para garantir isolamento.
7. **CreatedAt/UpdatedAt são gerenciados automaticamente**: via override de `SaveChangesAsync` no DbContext. Nunca setar manualmente.

### 5. Eventos e Mensageria (RabbitMQ)
- Acoplamento frouxo utilizando Eventos de Domínio (`InventiCore.Application.Common.Events`).
- A infraestrutura publica eventos no `RabbitMQ` configurado em `Topic Exchange` (`inventicore.events`).
- Workers (`BackgroundService`) consomem filas para fluxos assíncronos, como disparar Webhooks do Discord (`StockLowDiscordAlertWorker`).

### 6. Segurança e Isolamento (JWT)
- Autenticação configurada via `JwtBearer`.
- Isolamento Multi-tenant enforcing: O `TenantId` é lido do token (via `ICurrentUserService`) em vez do request body, garantindo que usuários nunca transacionem dados de Tenants que não lhes pertencem.

### 7. CI/CD e Integração Contínua
- Pipeline de integração contínua implementado no GitHub Actions (`.github/workflows/main.yml`).
- A cada *push* ou *pull request* na `main`, o código sofre *restore*, *build* em modo `Release` e *test*. Isso garante a integridade estrutural da master branch e evita que quebras sejam integradas silenciosamente.

## Roadmap & Checklists
1. **[x] Passo 1:** Estabelecer a Memória da IA.
2. **[x] Passo 2:** CRUDs Iniciais (Tenants, Warehouses, Products).
3. **[x] Passo 3:** Movimentação e Alertas RabbitMQ.
4. **[x] Passo 4:** Segurança e Isolamento JWT.
5. **[x] Passo 5:** CI/CD e Integração com Repositório Remoto.

---

## 4. Convenções de Código

- **Namespaces**: file-scoped (`namespace X;`)
- **Nullable**: habilitado em todos os projetos
- **Commits**: Conventional Commits (`feat:`, `fix:`, `chore:`, `docs:`, `refactor:`)
- **Branches**: `main` (produção), feature branches quando necessário
- **Nomes de arquivos**: PascalCase para classes, match 1:1 com o nome da classe
