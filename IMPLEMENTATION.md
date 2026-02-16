# Order Service API - Implementação Completa

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-blue)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-Ready-blue)](https://www.docker.com/)
[![Tests](https://img.shields.io/badge/Tests-17%20Passing-green)](https://nunit.org/)

> **API REST para gestão de Pedidos** com validação de estoque, autenticação JWT, Minimal API e arquitetura limpa.

## 📋 Índice

- [Visão Geral](#visão-geral)
- [Tecnologias Utilizadas](#tecnologias-utilizadas)
- [Arquitetura](#arquitetura)
- [Como Executar](#como-executar)
- [Endpoints da API](#endpoints-da-api)
- [Testes](#testes)
- [Decisões Técnicas](#decisões-técnicas)

---

## 🎯 Visão Geral

Este projeto implementa uma API REST completa para gestão de pedidos com:
- ✅ Clean Architecture (Domain, Application, Infrastructure, API) em `src/`
- ✅ **Minimal API** (seguindo instruções específicas)
- ✅ CQRS com MediatR
- ✅ Validação de estoque em duas fases
- ✅ Operações idempotentes (Confirm/Cancel)
- ✅ Autenticação JWT
- ✅ **Correlation ID** para rastreamento de requisições
- ✅ PostgreSQL com EF Core
- ✅ Migrations automáticas
- ✅ Docker Compose pronto para uso
- ✅ 17 testes unitários passando com **NUnit**
- ✅ Swagger/OpenAPI completo

---

## 🛠 Tecnologias Utilizadas

- **.NET 8.0** - Framework principal
- **ASP.NET Core 8.0 Minimal API** - Web API leve e eficiente
- **Entity Framework Core 8.0** - ORM
- **PostgreSQL 16** - Banco de dados
- **MediatR** - CQRS pattern
- **JWT Bearer** - Autenticação
- **NUnit** - Framework de testes (conforme especificação)
- **FluentAssertions** - Assertions nos testes
- **Swagger/OpenAPI** - Documentação da API
- **Docker & Docker Compose** - Containerização

---

## 🏗 Arquitetura

```
OrderService/
├── OrderService.Domain/          # Camada de Domínio (entities, value objects, enums)
├── OrderService.Application/     # Camada de Aplicação (commands, queries, DTOs)
├── OrderService.Infrastructure/  # Camada de Infraestrutura (EF Core, repositories)
├── OrderService.API/             # Camada de API (controllers, auth)
└── OrderService.Tests/           # Testes unitários e de integração
```

### Princípios Aplicados
- **Clean Architecture** - Separação clara de responsabilidades
- **SOLID** - Princípios de design orientado a objetos
- **DDD** - Entities, Value Objects, Aggregates
- **CQRS** - Separação de comandos e queries
- **Repository Pattern** - Abstração de acesso a dados

---

## 🚀 Como Executar

### Pré-requisitos
- [Docker](https://www.docker.com/get-started) (versão 20.10+)
- [Docker Compose](https://docs.docker.com/compose/install/) (versão 2.0+)

### Opção 1: Docker Compose (Recomendado)

```bash
# Clone o repositório
git clone https://github.com/MsbowElite/nstech-challange.git
cd nstech-challange

# Inicie a aplicação
docker compose up

# Aguarde até ver a mensagem "Now listening on: http://[::]:8080"
```

A API estará disponível em:
- **Swagger UI**: http://localhost:8080/swagger
- **API Base URL**: http://localhost:8080

### Opção 2: Execução Local (sem Docker)

```bash
# Pré-requisitos adicionais:
# - .NET 8 SDK: https://dotnet.microsoft.com/download
# - PostgreSQL 16+: https://www.postgresql.org/download/

# 1. Configure o banco de dados local
# Edite OrderService.API/appsettings.json com sua connection string

# 2. Restaure as dependências
dotnet restore

# 3. Execute as migrations
cd OrderService.Infrastructure
dotnet ef database update --startup-project ../OrderService.API

# 4. Execute a API
cd ../OrderService.API
dotnet run

# A API estará em http://localhost:5000
# Com Docker: http://localhost:8080
```

---

## 📚 Endpoints da API

### Autenticação

#### POST /auth/token
Gera um token JWT para autenticação.

**Request:**
```json
{
  "username": "usuario",
  "password": "senha"
}
```

**Response:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "tokenType": "bearer",
  "expiresIn": 3600
}
```

> 💡 Para demo, qualquer usuário/senha é aceito. Use o token no header: `Authorization: Bearer {token}`

---

### Orders

Todos os endpoints de orders requerem autenticação JWT.

#### POST /orders
Cria um novo pedido.

**Request:**
```json
{
  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "currency": "BRL",
  "items": [
    {
      "productId": "produto-id-aqui",
      "quantity": 2
    }
  ]
}
```

**Response:** `201 Created`
```json
{
  "id": "order-id",
  "customerId": "customer-id",
  "status": "Placed",
  "currency": "BRL",
  "total": 199.98,
  "items": [...],
  "createdAt": "2024-01-01T00:00:00Z",
  "updatedAt": null
}
```

---

#### POST /orders/{id}/confirm
Confirma um pedido (idempotente).

**Headers:** `Idempotency-Key: unique-key` (opcional)

**Response:** `200 OK`

---

#### POST /orders/{id}/cancel
Cancela um pedido (idempotente).

**Headers:** `Idempotency-Key: unique-key` (opcional)

**Response:** `200 OK`

---

#### GET /orders/{id}
Consulta um pedido específico.

**Response:** `200 OK`

---

#### GET /orders
Lista pedidos com paginação e filtros.

**Query Parameters:**
- `customerId` (opcional)
- `status` (opcional): Placed, Confirmed, Canceled
- `from` (opcional): data inicial (ISO 8601)
- `to` (opcional): data final (ISO 8601)
- `page` (padrão: 1)
- `pageSize` (padrão: 10)

**Response:** `200 OK`
```json
{
  "items": [...],
  "page": 1,
  "pageSize": 10,
  "totalCount": 100,
  "totalPages": 10
}
```

---

## 🧪 Testes

### Executar todos os testes

```bash
dotnet test
```

### Executar com detalhes

```bash
dotnet test --verbosity normal
```

### Cobertura de Testes

- ✅ **Domain Layer**: 17 testes
  - Order entity (7 testes)
  - Product entity (6 testes)
  - OrderItem value object (4 testes)

**Próximos passos de testes:**
- Application layer (command/query handlers)
- Integration tests (API endpoints)
- Load tests (performance)

---

## 📖 Decisões Técnicas

Consulte [docs/decisions.md](docs/decisions.md) para detalhes completos sobre:

### Principais Decisões

1. **Gerenciamento de Estoque em Duas Fases**
   - Validação na criação do pedido
   - Reserva na confirmação
   - Permite melhor UX e evita overselling

2. **Idempotência**
   - Implementada com chave única no banco
   - Previne duplicação em confirm/cancel
   - Header `Idempotency-Key` opcional

3. **Autenticação Simplificada**
   - JWT básico para demo
   - Produção: integrar com OAuth2/Azure AD

4. **Clean Architecture**
   - Domain independente de frameworks
   - Application contém regras de negócio
   - Infrastructure isola detalhes técnicos

---

## 📁 Estrutura do Banco de Dados

### Tabelas

#### Orders
- Id (PK, GUID)
- CustomerId (GUID)
- Status (enum)
- Currency (string)
- CreatedAt (datetime)
- UpdatedAt (datetime nullable)

#### OrderItems
- Id (PK, int, auto-increment)
- OrderId (FK)
- ProductId (GUID)
- UnitPrice (decimal)
- Quantity (int)

#### Products
- Id (PK, GUID)
- Name (string)
- UnitPrice (decimal)
- AvailableQuantity (int)
- CreatedAt (datetime)
- UpdatedAt (datetime nullable)

#### IdempotencyRecords
- Key (PK, string)
- CreatedAt (datetime)

---

## 🔍 Exemplos de Uso

### 1. Obter Token de Autenticação

```bash
curl -X POST http://localhost:8080/auth/token \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin"}'
```

### 2. Listar Produtos Disponíveis

Primeiro, obtenha o token. Depois, acesse o banco diretamente ou crie endpoints de produtos.

Os produtos são seedados automaticamente:
- Laptop Dell XPS 15 ($1299.99, qty: 50)
- Mouse Logitech MX Master 3 ($99.99, qty: 200)
- Keyboard Mechanical RGB ($149.99, qty: 100)
- ... e mais 5 produtos

### 3. Criar um Pedido

```bash
curl -X POST http://localhost:8080/orders \
  -H "Authorization: Bearer {seu-token}" \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "currency": "USD",
    "items": [
      {
        "productId": "{product-id-from-database}",
        "quantity": 2
      }
    ]
  }'
```

### 4. Confirmar Pedido

```bash
curl -X POST http://localhost:8080/orders/{order-id}/confirm \
  -H "Authorization: Bearer {seu-token}" \
  -H "Idempotency-Key: confirm-123"
```

### 5. Consultar Pedidos do Cliente

```bash
curl -X GET "http://localhost:8080/orders?customerId={customer-id}&page=1&pageSize=10" \
  -H "Authorization: Bearer {seu-token}"
```

---

## 🎯 Checklist de Requisitos

- ✅ API roda local e via Docker
- ✅ Migrations aplicadas automaticamente
- ✅ Endpoints MUST implementados
- ✅ JWT + autorização básica funcionando
- ✅ `dotnet test` passando
- ✅ README com passo a passo
- ✅ Clean Architecture implementada
- ✅ SOLID e boas práticas
- ✅ Testes de unidade
- ✅ Idempotência em confirm/cancel
- ✅ Paginação e filtros
- ✅ Swagger/OpenAPI
- ✅ Async/await end-to-end

---

## 📝 Licença

Este projeto foi desenvolvido como parte de um teste técnico.

---

## 👨‍💻 Autor

Desenvolvido para avaliação técnica - Senior .NET Developer Position

**Contato:**
- Repository: https://github.com/MsbowElite/nstech-challange
- Technical Decisions: [docs/decisions.md](docs/decisions.md)

---

## 🚧 Próximos Passos (Produção)

Para transformar este projeto em production-ready:

1. ✅ **Logging estruturado** - Serilog, Application Insights
2. ✅ **Monitoring** - Health checks, métricas, APM
3. ✅ **Caching** - Redis para dados frequentes
4. ✅ **Rate Limiting** - Proteção contra abuso
5. ✅ **CI/CD Pipeline** - Azure DevOps, GitHub Actions
6. ✅ **Secrets Management** - Azure Key Vault
7. ✅ **API Gateway** - Azure API Management
8. ✅ **Events** - Domain events, Event Store
9. ✅ **Message Queue** - RabbitMQ, Azure Service Bus
10. ✅ **Observability** - OpenTelemetry, Jaeger

---

**Obrigado por avaliar este projeto! 🚀**
