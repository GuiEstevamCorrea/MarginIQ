# Projeto MarginIQ

**Sistema Inteligente de Aprovação e Governança de Descontos**

---

## LEVANTAMENTO DE REQUISITOS – DOCUMENTAÇÃO PARA IMPLEMENTAÇÃO

---

## 1. Objetivo Geral

Criar uma camada de governança e inteligência de descontos para times comerciais B2B, com foco em:

- **Redução de perda de margem**
- **Padronização de descontos**
- **Eliminação de gargalos de aprovação**
- **Uso de IA para recomendar, avaliar risco e autoaprovar descontos**

O sistema **não substitui ERP/CRM**, atua como **camada intermediária**, integrável com SAP, TOTVS, CRMs e planilhas.

**Arquitetura preparada para:**

- Multi-tenant
- IA com governança
- Evolução incremental do modelo

---

## 2. Arquitetura Geral (Hexagonal) - OK

```
/src
 ├── Domain
 │   ├── Entities
 │   ├── ValueObjects
 │   ├── Enums
 │   ├── Repositories (Interfaces)
 │   └── Services (Regras puras)
 │
 ├── Application
 │   ├── UseCases
 │   ├── DTOs
 │   ├── Ports (Inbound / Outbound)
 │   └── Policies
 │
 ├── Infrastructure
 │   ├── Persistence (ORM / DB)
 │   ├── Auth
 │   ├── Messaging
 │   ├── ExternalIntegrations
 │   └── IA
 │
 └── API
     ├── Controllers
     ├── Middlewares
     └── Filters
```

---

## 3. Entidades de Domínio (CORE)

### 3.1 Empresa (Tenant) -  OK

Representa o cliente do SaaS.

- **Id**
- **Nome**
- **Segmento** (ex: indústria, SaaS, distribuição)
- **Status**
- **Configurações gerais**

**Regras:**

- Todos os dados são isolados por Empresa (multi-tenant)
- IA aprende por empresa (modelo lógico separado)

---

### 3.2 Usuário - OK

- **Id**
- **Nome**
- **Email**
- **Perfil:**
  - Vendedor
  - Gestor
  - Admin
- **EmpresaId**
- **Status**

---

### 3.3 Cliente - OK

- **Id**
- **Nome**
- **Segmento**
- **Classificação** (A/B/C – opcional)
- **EmpresaId**
- **Status**

---

### 3.4 Produto - OK

- **Id**
- **Nome**
- **Categoria**
- **Preço Base**
- **Margem Base**
- **EmpresaId**
- **Status**

---

### 3.5 Solicitação de Desconto - OK

**Entidade central do sistema**

- **Id**
- **ClienteId**
- **VendedorId**
- **Itens** (produto, quantidade, preço base)
- **DescontoSolicitado** (%)
- **PreçoFinal**
- **MargemEstimada**
- **Status:**
  - Em análise
  - Aprovado
  - Reprovado
  - Ajuste solicitado
  - Autoaprovado (IA)
- **ScoreRisco** (0–100)
- **EmpresaId**
- **DataHoraCriacao**

---

### 3.6 Aprovação - OK
- **Id**
- **SolicitaçãoId**
- **AprovadorId** (ou "IA")
- **Decisão** (Aprovar / Reprovar / Ajustar)
- **Justificativa**
- **SLA** (tempo de decisão)
- **DataHora**

---

### 3.7 Regra Comercial - OK

- **Id**
- **Tipo:**
  - Margem mínima
  - Limite de desconto
  - Autoaprovação
- **Escopo:**
  - Produto
  - Categoria
  - Cliente
  - Perfil do vendedor
- **Parâmetros**
- **EmpresaId**
- **Ativa/Inativa**

---

### 3.8 Log de Auditoria - OK

- **Id**
- **Entidade**
- **EntidadeId**
- **Ação**
- **Origem** (Humano / IA)
- **Payload**
- **DataHora**

---

## 4. Regras de Negócio (Domain Services)

### 4.1 Cálculo de Margem - OK

```
margem = (preçoFinal - custoEstimado) / preçoFinal
```

---

### 4.2 Validação de Regras Comerciais - OK

- Desconto não pode ultrapassar limite por perfil
- Margem não pode ficar abaixo do mínimo configurado
- Cliente bloqueado → reprovação automática

---

### 4.3 Score de Risco - OK

Score calculado com base em:

- Histórico do cliente
- Desconto fora do padrão
- Comportamento do vendedor
- Margem resultante

**Score alto → exige aprovação humana**

---

### 4.4 Autoaprovação Inteligente - OK
Autoaprovação ocorre quando:

- Score abaixo do threshold
- Dentro dos guardrails
- Modelo de IA recomenda com confiança mínima

---

## 5. Módulo de IA (40–50% do projeto)

### 5.1 Arquitetura da IA - OK

A IA **não vive no domínio**, entra como **adapter externo**.

```
Application → Port IA → Adapter IA
```

**Fallback sempre disponível para regras fixas.**

---

### 5.2 Base de Aprendizado - OK

Dados armazenados:

- Cliente
- Produto
- Desconto
- Margem
- Decisão
- Resultado da venda (ganha/perdida)

---

### 5.3 Casos de Uso da IA

#### IA-UC-01 – Recomendar Desconto - OK

**Input:**

- Cliente
- Produto(s)
- Histórico
- Regras

**Output:**

- % desconto recomendado
- Margem esperada
- Confiança

---

#### IA-UC-02 – Calcular Score de Risco - OK

- Analisa desvio do padrão histórico
- Retorna score 0–100

---

#### IA-UC-03 – Explicabilidade - OK

Gera texto simples:

- "Desconto comum para este cliente"
- "Margem abaixo do padrão histórico"

---

#### IA-UC-04 – Aprendizado Incremental - OK
Treino periódico baseado em:

- Decisões humanas
- Resultado real da venda

---

### 5.4 Governança da IA - OK

- Ativar/desativar IA por empresa
- Ajustar autonomia
- Auditoria completa de decisões

---

## 6. Fluxos Funcionais (Use Cases)

### 6.1 Solicitar Desconto

#### UC-01 – Criar Solicitação de Desconto - OK

1. Validar usuário e empresa
2. Calcular margem
3. Aplicar regras comerciais
4. Chamar IA para:
   - recomendação
   - score
5. Decidir:
   - autoaprovar
   - enviar para aprovação

---

### 6.2 Aprovação

#### UC-02 – Aprovar / Reprovar Solicitação - OK

- Gestor decide
- Justificativa obrigatória se reprovar
- Registra SLA

---

### 6.3 Autoaprovação

#### UC-03 – Autoaprovação por IA - OK

- Sistema aprova
- Marca origem como "IA"
- Permite revisão posterior

---

### 6.4 Histórico e Auditoria

#### UC-04 – Consultar Histórico

- Todas as decisões
- Humano vs IA
- Filtros por período, cliente, vendedor

---

## 7. Integrações (Adapters)

### 7.1 ERP / CRM (futuro)

- SAP
- TOTVS
- Planilha (CSV)

**Integrações assíncronas, desacopladas.**

---

### 7.2 Notificações

- Email (obrigatório)
- WhatsApp (fase 2)

---

## 8. Requisitos Não Funcionais

### 8.1 Performance

- IA responde em até 2s
- Fallback automático

---

### 8.2 Segurança

- JWT
- Perfis
- Logs

---

### 8.3 Escalabilidade

- Multi-tenant
- IA isolada por empresa
- Histórico crescente

---

## 9. Fora do Escopo do MVP

- CPQ completo
- CRM
- Forecast
- NLP avançado

---

## 10. Resultado Esperado

Produto vendável em **60 dias**

- Setup em horas (planilha)
- Primeiro dashboard já mostrando:
  - Margem salva
  - Tempo reduzido
  - % autoaprovada

---

## 🔥 Resumo para o Desenvolvedor

- **Domínio simples e forte**
- **IA como copiloto, não caixa-preta**
- **Hexagonal bem respeitada**
- **Governança > automação cega**
- **Produto feito para gerar dinheiro, não só processo**
