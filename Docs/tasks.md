# 🔧 EPIC 1 — Infrastructure: Persistence (EF Core + SQL Server) [P0]

## 1.1 Criar DbContext base - OK

### Atividades

- Criar `MarginIQDbContext : DbContext`
- Adicionar `DbSet<>` para cada Aggregate Root:
  - Company
  - User
  - Customer
  - Product
  - DiscountRequest
  - Approval
  - BusinessRule
  - AILearningData
  - AuditLog
- Injetar `DbContextOptions`

### Critérios de aceite

- [x] Projeto compila
- [x] DbContext inicializa com SQL Server local
- [x] Conexão configurável via appsettings.json

---

## 1.2 Configuração de entidades (Fluent API) - OK

### Atividades

- Criar uma pasta `Configurations`
- Criar uma classe por entidade implementando `IEntityTypeConfiguration<T>`
- Definir:
  - Chaves primárias
  - Índices (CompanyId, ExternalId, CreatedAt)
  - Relacionamentos (1:N, N:N se houver)
  - Required vs Optional
  - Tamanhos de campos (varchar limits)

### Atenções importantes

- Todas as tabelas devem conter `CompanyId`
- Índice composto (CompanyId, Id) para isolamento multi-tenant

### Critérios de aceite

- [x] `dotnet ef migrations add Initial`
- [x] Migration gerada sem erros
- [x] Script SQL coerente

---

## 1.3 Value Objects (Money + Owned Types) - OK

### Money

#### Atividades

- Criar `ValueConverter<Money, decimal>`
- Configurar precisão (decimal(18,4))
- Garantir imutabilidade

### DiscountRequestItem

#### Atividades

- Mapear como Owned Entity
- Configurar tabela filha (DiscountRequestItems)
- Relacionar com ProductId

### Critérios de aceite

- [x] Dados persistidos corretamente
- [x] Sem tabelas órfãs
- [x] Queries carregam corretamente os itens

---

## 1.4 Implementação dos Repositórios - OK

### Atividades

- Criar `BaseRepository<T>`
- Implementar os 9 repositórios:
  - CompanyRepository
  - UserRepository
  - CustomerRepository
  - ProductRepository
  - DiscountRequestRepository
  - ApprovalRepository
  - BusinessRuleRepository
  - AILearningDataRepository
  - AuditLogRepository
- Implementar:
  - Add
  - GetById
  - Find
  - List
  - Update

### Regras

- Todos os métodos filtram por `CompanyId`
- Nada de DbContext vazando para Application

### Critérios de aceite

- [x] Use Cases executam CRUD real
- [x] Multi-tenant isolado por query
- [x] Nenhum acesso direto ao EF fora da Infrastructure


---

# 🔐 EPIC 2 — Authentication (JWT) [P0]

## 2.1 Hash de senha - OK

### Atividades

- Escolher bcrypt ou Argon2
- Criar `IPasswordHasher`
- Implementar:
  - Hash
  - Verify

### Critérios de aceite

- [x] Senhas nunca persistidas em texto puro
- [x] Hash validado corretamente

---

## 2.2 Geração de JWT

### Atividades

- Criar `AuthenticationService`
- Criar Access Token com:
  - UserId
  - CompanyId
  - Role
  - Expiração curta (ex: 15 min)
- Criar Refresh Token persistido no banco

### Critérios de aceite

- [ ] Login retorna tokens válidos
- [ ] Token contém claims necessárias
- [ ] Refresh token funciona

---

## 2.3 Middleware de autenticação

### Atividades

- Configurar `AddAuthentication().AddJwtBearer()`
- Criar middleware para:
  - Extrair CompanyId do token
  - Setar TenantContext

### Critérios de aceite

- [ ] Endpoints protegidos
- [ ] Token inválido → 401
- [ ] Token válido → acesso liberado

---

# 🛂 EPIC 3 — Authorization & Multi-Tenant [P0]

## 3.1 RBAC (Role-Based Access Control)

### Atividades

- Criar `AuthorizationService`
- Implementar:
  - CanCreateDiscount
  - CanApproveDiscount
  - CanOverrideAI
- Baseado em Role + contexto

### Critérios de aceite

- [ ] Salesperson não aprova
- [ ] Manager aprova
- [ ] Admin configura AI

---

## 3.2 Filtro de isolamento multi-tenant

### Atividades

- Criar `TenantFilter`
- Validar CompanyId em toda request
- Bloquear acesso cruzado

### Critérios de aceite

- [ ] Usuário de uma empresa não acessa dados de outra
- [ ] Teste manual com dois CompanyIds

---

# 🌐 EPIC 4 — API Controllers Core [P0]

## 4.1 AuthController

### Endpoints

- `POST /auth/login`
- `POST /auth/refresh`
- `POST /auth/logout`

### Critérios de aceite

- [ ] Login funcional
- [ ] Tokens renováveis
- [ ] Logout invalida refresh token

---

## 4.2 DiscountRequestsController

### Endpoints

- `POST /discount-requests`
- `GET /discount-requests/{id}`
- `GET /discount-requests`

### Fluxo

Criação chama:
- BusinessRules
- AI Recommendation
- AutoApproval

### Critérios de aceite

- [ ] Request criada
- [ ] AuditLog gravado
- [ ] Auto-approval funcionando

---

## 4.3 ApprovalsController

### Endpoints

- `POST /approvals/{id}/approve`
- `POST /approvals/{id}/reject`

### Critérios de aceite

- [ ] Manager decide
- [ ] SLA registrado
- [ ] AI learning data gravada

---

# ⚠️ EPIC 5 — Middleware & Observabilidade [P0]

## 5.1 Global Exception Handler

### Atividades

- Capturar exceções
- Mapear para HTTP correto
- Log estruturado

### Critérios de aceite

- [ ] Nenhuma exception vaza stacktrace
- [ ] Erros padronizados

---

## 5.2 Logging e Performance

### Atividades

- Integrar Serilog
- Logar:
  - RequestId
  - CompanyId
  - Tempo de resposta
- Integrar métricas já existentes

### Critérios de aceite

- [ ] Logs legíveis
- [ ] Tempo médio mensurado

---

# 🤖 EPIC 6 — AI Service (MVP) [P1]

## 6.1 AIService baseado em regras

### Atividades

- Implementar `IAIService`
- Algoritmo simples:
  - Média histórica
  - Limite de margem
  - Peso por cliente

### Critérios de aceite

- [ ] Recomendação coerente
- [ ] Risk Score calculado
- [ ] Explainability funcionando

---

## 6.2 Persistência de aprendizado

### Atividades

- Registrar decisões humanas
- Criar dataset incremental

### Critérios de aceite

- [ ] Dados armazenados
- [ ] Base pronta para ML futuro

---

# 📧 EPIC 7 — Notifications [P1]

## 7.1 Email Adapter

### Atividades

- Implementar SendGrid ou SMTP
- Criar templates Razor/Liquid
- Criar fila simples (in-memory)

### Critérios de aceite

- [ ] Email enviado em eventos-chave
- [ ] Falha não quebra fluxo principal

---

# 🧪 EPIC 8 — Testing [P0/P1]

## 8.1 Unit Tests (Domínio + Application)

### Atividades

- Testar:
  - MarginCalculationService
  - RiskScoreCalculationService
  - AutoApprovalService
- Mockar repositórios

### Critérios de aceite

- [ ] 70% do core coberto
- [ ] Testes rápidos

---

## 8.2 Integration Tests

### Atividades

- Subir SQL Server via Testcontainers
- Testar repositórios reais
- Testar fluxo end-to-end

### Critérios de aceite

- [ ] Criar → aprovar → auditar
- [ ] Multi-tenant validado

---

# 🚀 EPIC 9 — MVP Definition (Go/No-Go)

## Checklist final

- [ ] Login funciona
- [ ] Criar desconto
- [ ] AI decide
- [ ] Manager aprova
- [ ] Audit log completo
- [ ] Email enviado
- [ ] Banco persistente
- [ ] API documentada