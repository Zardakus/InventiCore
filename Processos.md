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
