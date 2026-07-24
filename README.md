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
| Hangfire | Filas e jobs em background |
| Cloudflare R2 | Storage de arquivos |
| Resend | Envio de e-mails |
| Sentry | Monitoramento de erros |
| Docker + Compose | Orquestração de containers |

---

## 📁 Estrutura do Projeto

src/
├── Authorization/ → Policies, handlers e atributos de autorização
├── Constants/ → Constantes (AuditActions, TicketStatus, TicketPriority, StorageFolders)
├── Controllers/ → Endpoints da API
├── Data/
│ ├── Migrations/ → Histórico de migrations do banco
│ └── Seeders/ → Dados iniciais (permissões, admin, tenant, settings)
├── DTOs/ → Objetos de transferência de dados
├── HealthChecks/ → Health checks customizados (Redis)
├── Middlewares/ → CORS dinâmico, blacklist de tokens, resolução de tenant
├── Models/ → Entidades do banco
├── Services/ → Lógica de negócio
├── Templates/ → Templates HTML de e-mail
└── Validators/ → Validações FluentValidation

---

## 🔐 Autenticação

A API usa **JWT Bearer** com suporte a:

- Login com ou sem "lembrar-me"
- Logout com blacklist no Redis (token invalidado imediatamente)
- Refresh token com rotação (cada token só pode ser usado uma vez)
- Proteção de rotas com `[Authorize]`
- Super Admin com claim `is_super_admin`

### Headers obrigatórios

Authorization: Bearer {token}
X-Tenant-Subdomain: {subdomain}

---

## 🚀 Como rodar localmente

### Pré-requisitos

- Docker Desktop com integração WSL ativada

### 1. Clone o repositório

```bash
git clone https://github.com/rodrigoinfosistec/sisky-api.git
cd sisky-api
```

### 2. Configure as variáveis de ambiente

Copia o arquivo de exemplo:

```bash
cp docker-compose.example.yml docker-compose.yml
```

Preenche as variáveis no `docker-compose.yml`.

Cria o arquivo `src/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=sisky;Username=sisky;Password=sisky",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Key": "sua-chave-secreta-minimo-32-caracteres"
  },
  "Admin": {
    "Email": "admin@email.com",
    "Password": "suasenha"
  }
}
```

> Gera uma chave JWT segura com: `openssl rand -base64 32`

### 3. Sobe os containers

```bash
docker compose up --build -d
```

A API estará disponível em `http://localhost:5169`

---

## ⚙️ Variáveis de Ambiente

| Variável | Descrição |
|---|---|
| `ConnectionStrings__DefaultConnection` | String de conexão PostgreSQL |
| `ConnectionStrings__Redis` | String de conexão Redis |
| `Jwt__Key` | Chave secreta JWT |
| `Jwt__Issuer` | Issuer do JWT |
| `Jwt__Audience` | Audience do JWT |
| `Jwt__ExpiresInHours` | Expiração do token em horas |
| `App__Domain` | Domínio principal (ex: sisky.com.br) |
| `App__FrontendUrl` | URL do frontend |
| `Admin__Email` | E-mail do super admin (seeder) |
| `Admin__Password` | Senha do super admin (seeder) |
| `Admin__SupportEmail` | E-mail padrão de suporte |
| `Mail__ApiKey` | Chave da API Resend |
| `Mail__FromAddress` | Endereço de envio |
| `Mail__FromName` | Nome do remetente |
| `Storage__AccountId` | Account ID Cloudflare R2 |
| `Storage__AccessKeyId` | Access Key ID R2 |
| `Storage__SecretAccessKey` | Secret Access Key R2 |
| `Storage__BucketName` | Nome do bucket R2 |
| `Storage__PublicUrl` | URL pública do storage |
| `Hangfire__DashboardUser` | Usuário do dashboard Hangfire |
| `Hangfire__DashboardPassword` | Senha do dashboard Hangfire |
| `Sentry__Dsn` | DSN do Sentry |

---

## 📋 Endpoints

### Auth — `/api/auth`

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| POST | `/api/auth/login` | ❌ | Login do usuário |
| POST | `/api/auth/refresh` | ❌ | Renovar token |
| POST | `/api/auth/logout` | ✅ | Logout |
| POST | `/api/auth/forgot-password` | ❌ | Solicitar redefinição de senha |
| POST | `/api/auth/reset-password` | ❌ | Redefinir senha |
| GET | `/api/auth/sessions` | ✅ | Listar sessões ativas |
| DELETE | `/api/auth/sessions/{tokenSuffix}` | ✅ | Revogar sessão |
| DELETE | `/api/auth/sessions` | ✅ | Revogar todas as sessões |
| POST | `/api/auth/switch-company` | ✅ | Trocar empresa ativa |

---

### User — `/api/user`

| Método | Rota | Permissão | Descrição |
|---|---|---|---|
| GET | `/api/user` | `users.view` | Listar usuários |
| GET | `/api/user/me` | ✅ | Perfil do usuário logado |
| GET | `/api/user/{id}` | `users.view` | Detalhes do usuário |
| GET | `/api/user/{id}/details` | `users.view` | Detalhes completos |
| GET | `/api/user/{id}/companies` | `users.view` | Empresas do usuário |
| POST | `/api/user` | `users.create` | Criar usuário |
| PUT | `/api/user/{id}` | `users.edit` | Atualizar usuário |
| DELETE | `/api/user/{id}` | `users.delete` | Excluir usuário |
| PATCH | `/api/user/{id}/toggle-active` | `users.edit` | Ativar/inativar usuário |
| POST | `/api/user/{id}/avatar` | ✅ | Atualizar avatar |
| PATCH | `/api/user/{id}/change-password` | ✅ | Alterar senha |
| POST | `/api/user/{id}/companies` | `users.edit` | Adicionar empresa ao usuário |
| DELETE | `/api/user/{id}/companies/{companyId}` | `users.edit` | Remover empresa do usuário |
| PATCH | `/api/user/{id}/companies/{companyId}/default` | `users.edit` | Definir empresa padrão |
| POST | `/api/user/{id}/companies/{companyId}/roles` | `users.edit` | Adicionar role |
| DELETE | `/api/user/{id}/companies/{companyId}/roles/{roleId}` | `users.edit` | Remover role |

---

### Role — `/api/role`

| Método | Rota | Permissão | Descrição |
|---|---|---|---|
| GET | `/api/role` | `users.view` | Listar perfis de acesso |
| GET | `/api/role/{id}` | `users.view` | Permissões do perfil |
| POST | `/api/role` | `users.create` | Criar perfil |
| PUT | `/api/role/{id}` | `users.edit` | Atualizar perfil |
| DELETE | `/api/role/{id}` | `users.delete` | Excluir perfil |
| POST | `/api/role/{id}/permissions` | `users.edit` | Atualizar permissões |

---

### Audit — `/api/audit`

| Método | Rota | Permissão | Descrição |
|---|---|---|---|
| GET | `/api/audit` | `audit.view` | Listar logs de auditoria |

**Parâmetros de filtro:** `page`, `perPage`, `search`, `action`, `entity`, `from`, `to`

---

### Dashboard — `/api/dashboard`

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| GET | `/api/dashboard/metrics` | ✅ | Métricas do tenant |

---

### Tenant — `/api/tenant`

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| GET | `/api/tenant/resolve` | ❌ | Resolver tenant pelo subdomínio |

---

### Ticket — `/api/ticket`

| Método | Rota | Auth | Descrição |
|---|---|---|---|
| GET | `/api/ticket` | ✅ | Listar tickets da empresa |
| GET | `/api/ticket/{id}` | ✅ | Detalhes do ticket |
| POST | `/api/ticket` | ✅ | Abrir ticket |
| POST | `/api/ticket/{id}/messages` | ✅ | Enviar mensagem |
| PATCH | `/api/ticket/{id}/status` | ✅ | Atualizar status |

**Prioridades:** `low` `medium` `high` `urgent`

**Status:** `open` `in_progress` `resolved` `closed`

---

### Admin — `/api/admin` 🔐

> Requer `is_super_admin = true` no token JWT.

| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/admin/dashboard` | Métricas globais |
| GET | `/api/admin/tenants` | Listar todos os tenants |
| GET | `/api/admin/tenants/{id}` | Detalhes do tenant |
| POST | `/api/admin/tenants` | Criar tenant |
| PUT | `/api/admin/tenants/{id}` | Atualizar tenant |
| DELETE | `/api/admin/tenants/{id}` | Excluir tenant |
| PATCH | `/api/admin/tenants/{id}/toggle-active` | Ativar/inativar tenant |
| GET | `/api/admin/audit` | Logs de todos os tenants |
| GET | `/api/admin/tickets` | Listar todos os tickets |
| GET | `/api/admin/tickets/{id}` | Detalhes do ticket |
| POST | `/api/admin/tickets/{id}/messages` | Responder ticket |
| PATCH | `/api/admin/tickets/{id}/status` | Atualizar status do ticket |
| GET | `/api/admin/settings` | Listar configurações |
| PUT | `/api/admin/settings` | Atualizar configurações |

**Configurações disponíveis:**

| Chave | Descrição |
|---|---|
| `support_email` | E-mail que recebe notificações de tickets |
| `system_name` | Nome do sistema |

---

## 🔑 Módulos e Permissões

| Módulo | Slug | Permissões disponíveis |
|---|---|---|
| Usuários | `users` | `view`, `create`, `edit`, `delete` |
| Financeiro | `financeiro` | `view`, `create`, `edit`, `delete` |
| RH | `rh` | `view`, `create`, `edit`, `delete` |
| CRM | `crm` | `view`, `create`, `edit`, `delete` |
| Auditoria | `audit` | `view` |

---

## 📧 Notificações por E-mail

| Evento | Destinatário |
|---|---|
| Recuperação de senha | Usuário |
| Ticket aberto | Usuário + Admin |
| Resposta do admin | Usuário |
| Resposta do tenant | Admin |
| Status do ticket alterado | Usuário |

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

---

## 🛠️ Ferramentas

| Ferramenta | URL | Acesso |
|---|---|---|
| Scalar (documentação) | `/scalar` | Apenas Development |
| Hangfire (filas) | `/hangfire` | Basic Auth |
| Health Check | `/health` | Público |

