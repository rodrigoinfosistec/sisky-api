# Sisky API

API RESTful desenvolvida em **ASP.NET Core 10** com **C#**, seguindo boas práticas de arquitetura, segurança e organização de código.

---

## 🛠️ Stack

| Tecnologia | Descrição |
|---|---|
| ASP.NET Core 10 | Framework principal |
| PostgreSQL 16 | Banco de dados relacional |
| Redis 7 | Cache e blacklist de tokens |
| Entity Framework Core 10 | ORM (Code First) |
| FluentValidation | Validação de dados |
| BCrypt | Hash de senhas |
| JWT Bearer | Autenticação stateless |
| Docker + Compose | Orquestração de containers |

---

## 📁 Estrutura do Projeto

---

## 🔐 Autenticação

A API usa **JWT Bearer** com suporte a:

- Login com ou sem "lembrar-me"
- Logout com blacklist no Redis (token invalidado imediatamente)
- Refresh token com rotação (cada token só pode ser usado uma vez)
- Proteção de rotas com `[Authorize]`

### Fluxo

---

## 🚀 Como rodar localmente

### Pré-requisitos

- Docker Desktop com integração WSL ativada

### 1. Clone o repositório

```bash
git clone https://github.com/seu-usuario/sisky-api.git
cd sisky-api
```

### 2. Configure as variáveis de ambiente

Cria o arquivo `src/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=sisky;Username=sisky;Password=sisky",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Key": "sua-chave-secreta-minimo-32-caracteres"
  }
}
```

> Gera uma chave segura com: `openssl rand -base64 32`

### 3. Sobe os containers

```bash
docker compose up -d
```

A API estará disponível em `http://localhost:5169`

---

## 📋 Endpoints

### Auth
| Método | Rota | Descrição | Auth |
|---|---|---|---|
| POST | `/api/auth/login` | Login | ❌ |
| POST | `/api/auth/logout` | Logout | ✅ |
| POST | `/api/auth/refresh` | Renova o JWT | ❌ |

### Users
| Método | Rota | Descrição | Auth |
|---|---|---|---|
| GET | `/api/user` | Lista todos os usuários | ✅ |
| GET | `/api/user/{id}` | Busca usuário por ID | ✅ |
| GET | `/api/user/me` | Usuário autenticado | ✅ |
| POST | `/api/user` | Cria usuário | ✅ |
| PUT | `/api/user/{id}` | Atualiza usuário | ✅ |
| PATCH | `/api/user/{id}/change-password` | Altera senha | ✅ |
| DELETE | `/api/user/{id}` | Remove usuário | ✅ |

---

## 🗄️ Banco de Dados

### Migrations

```bash
# Criar migration
dotnet ef migrations add NomeDaMigration

# Aplicar migrations
dotnet ef database update

# Recriar banco do zero
dotnet ef database drop --force && dotnet ef database update
```

---

## 🐳 Docker

```bash
# Subir tudo
docker compose up -d

# Subir e rebuildar
docker compose up --build -d

# Derrubar
docker compose down

# Derrubar e apagar volumes
docker compose down -v

# Ver logs da API
docker compose logs -f api
```