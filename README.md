# 🎯 CareWork API - Advanced Business Development with .NET

API RESTful completa em .NET 8 para a plataforma de bem-estar emocional CareWork, desenvolvida seguindo todas as boas práticas e requisitos da disciplina Advanced Business Development with .NET.

## 📋 Sobre o Projeto

**CareWork** é uma plataforma de bem-estar emocional para ambientes de trabalho híbridos. Esta API fornece endpoints completos para gerenciamento de check-ins emocionais, análise de tendências, relatórios detalhados e recomendações personalizadas de dicas de bem-estar.

### 🎯 Funcionalidades Principais

- ✅ **Check-ins Emocionais**: Registro diário de humor, stress e qualidade do sono
- ✅ **Análises e Insights**: Tendências, sequências (streaks) e comparações de períodos
- ✅ **Relatórios Detalhados**: Relatórios semanais e mensais com análises completas
- ✅ **Dicas Personalizadas**: Recomendações inteligentes baseadas no estado do usuário
- ✅ **Gestão de Perfil**: Atualização de perfil, senha e exclusão de conta
- ✅ **Tips Pré-cadastradas**: 20 dicas de bem-estar categorizadas (Stress, Sleep, Mood, Wellness)

## 🚀 Tecnologias Utilizadas

- **.NET 8** - Framework principal
- **Entity Framework Core 8** - ORM para acesso a dados (Code First)
- **SQLite** - Banco de dados para desenvolvimento (macOS)
- **SQL Server** - Banco de dados para produção
- **JWT** - Autenticação e autorização
- **BCrypt** - Hash de senhas
- **Serilog** - Logging estruturado
- **OpenTelemetry** - Tracing distribuído
- **Swagger/OpenAPI** - Documentação interativa da API
- **xUnit** - Framework de testes (111 testes, 100% passando)
- **AutoMapper** - Mapeamento de objetos
- **FluentAssertions** - Assertions mais legíveis nos testes

## 📁 Estrutura do Projeto

```
CareWork-DotNet/
├── CareWork.API/                    # Projeto principal da API
│   ├── Controllers/
│   │   └── V1/                     # Controllers versionados
│   │       ├── AuthController.cs   # Autenticação (register, login, profile, password, account)
│   │       ├── CheckinsController.cs # Check-ins (CRUD completo)
│   │       ├── TipsController.cs   # Dicas de bem-estar (CRUD)
│   │       ├── ReportsController.cs # Relatórios (weekly, monthly)
│   │       └── InsightsController.cs # Análises (trends, streak, compare, recommended-tips)
│   ├── Models/
│   │   └── DTOs/                   # Data Transfer Objects
│   ├── Services/                   # Lógica de negócio
│   │   ├── AuthService.cs
│   │   ├── CheckinService.cs
│   │   ├── TipService.cs
│   │   └── InsightsService.cs
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs
│   ├── Mapping/
│   │   └── MappingProfile.cs
│   └── Program.cs
├── CareWork.Infrastructure/        # Camada de infraestrutura
│   ├── Data/
│   │   ├── CareWorkDbContext.cs   # DbContext do EF Core
│   │   ├── DbSeeder.cs            # Seed automático de 20 tips iniciais
│   │   └── Configurations/         # Configurações do EF Core (Fluent API)
│   │       ├── CheckinConfiguration.cs
│   │       ├── TipConfiguration.cs
│   │       └── UserConfiguration.cs
│   ├── Models/                    # Modelos de domínio (entidades)
│   │   ├── User.cs                # Usuário com autenticação
│   │   ├── Checkin.cs             # Check-in com Notes e Tags
│   │   └── Tip.cs                 # Dica de bem-estar
│   └── Migrations/                # Migrations do EF Core (Code First)
└── CareWork.Tests/                 # Projeto de testes
    ├── IntegrationTests/          # 111 testes de integração
    └── UnitTests/                  # Testes unitários dos Services
```

## 🔌 Endpoints da API

### Base URL
```
http://localhost:8080/api/v1
```

### 📚 Documentação Completa
Todos os endpoints estão documentados no Swagger UI (`http://localhost:8080/swagger`). Abaixo estão os principais endpoints e exemplos de uso.

### 🔐 Autenticação

#### POST `/api/v1/auth/register`
Registra um novo usuário e retorna token JWT.

**Request:**
```json
{
  "email": "user@example.com",
  "password": "password123",
  "name": "João Silva"
}
```

**Validações:**
- `email`: Email válido, obrigatório
- `password`: Mínimo 6 caracteres, obrigatório
- `name`: Mínimo 2 caracteres, máximo 200, apenas letras e espaços

#### POST `/api/v1/auth/login`
Realiza login e retorna token JWT.

#### PUT `/api/v1/auth/profile` 🔒
Atualiza nome e email do perfil do usuário autenticado.

#### PUT `/api/v1/auth/password` 🔒
Atualiza senha do usuário (requer senha atual).

#### DELETE `/api/v1/auth/account` 🔒
Deleta conta do usuário permanentemente (requer confirmação com senha).

### 📝 Check-ins

Todos os endpoints requerem autenticação JWT.

#### GET `/api/v1/checkins?page=1&pageSize=10`
Lista check-ins do usuário com paginação e HATEOAS.

#### GET `/api/v1/checkins/{id}`
Busca check-in específico por ID.

#### POST `/api/v1/checkins`
Cria novo check-in com notas e tags opcionais.

**Request:**
```json
{
  "mood": 4,
  "stress": 2,
  "sleep": 5,
  "notes": "Dia produtivo, me senti bem",
  "tags": ["trabalho", "produtivo"]
}
```

**Validações:**
- `mood`, `stress`, `sleep`: Valores entre 1 e 5
- `notes`: Máximo 1000 caracteres (opcional)
- `tags`: Lista de strings (opcional)

#### PUT `/api/v1/checkins/{id}`
Atualiza check-in existente.

#### DELETE `/api/v1/checkins/{id}`
Deleta check-in.

### 💡 Tips (Dicas de Bem-estar)

**Importante:** As tips são pré-cadastradas no sistema (20 tips iniciais). Usuários apenas visualizam e recebem recomendações.

#### GET `/api/v1/tips?page=1&pageSize=10&category=Stress`
Lista dicas com paginação, filtro por categoria e HATEOAS.

**Categorias disponíveis:**
- `Stress` - Gerenciamento de stress
- `Sleep` - Qualidade do sono
- `Mood` - Melhoria do humor
- `Wellness` - Bem-estar geral

#### GET `/api/v1/tips/{id}`
Busca dica específica por ID.

#### POST `/api/v1/tips` 🔒
Cria nova dica (para administração futura).

#### PUT `/api/v1/tips/{id}` 🔒
Atualiza dica existente.

#### DELETE `/api/v1/tips/{id}` 🔒
Deleta dica.

### 📊 Relatórios

#### GET `/api/v1/reports/weekly?weekStart=2024-11-04`
Gera relatório semanal completo com:
- Médias de mood, stress e sleep
- Dados diários da semana
- Melhor e pior dia da semana

**Parâmetros:**
- `weekStart`: Data de início da semana (YYYY-MM-DD)

**Response 200 OK:**
```json
{
  "success": true,
  "data": {
    "userId": "guid",
    "weekStart": "2024-11-04T00:00:00Z",
    "weekEnd": "2024-11-11T23:59:59Z",
    "averages": {
      "mood": 4.2,
      "stress": 2.1,
      "sleep": 4.5
    },
    "dailyData": [
      {
        "date": "2024-11-04",
        "mood": 4,
        "stress": 2,
        "sleep": 5
      }
    ]
  }
}
```

#### GET `/api/v1/reports/monthly?year=2024&month=11`
Gera relatório mensal completo com:
- Resumo semanal do mês
- Médias mensais
- Melhor e pior dia do mês
- Total de check-ins
- Frequência de check-ins (%)

**Parâmetros:**
- `year`: Ano (ex: 2024)
- `month`: Mês (1-12)

**Response 200 OK:**
```json
{
  "success": true,
  "data": {
    "userId": "guid",
    "year": 2024,
    "month": 11,
    "averages": {
      "mood": 4.0,
      "stress": 2.5,
      "sleep": 4.2
    },
    "weeklySummaries": [...],
    "bestWorstDays": {
      "bestDay": {...},
      "worstDay": {...}
    },
    "totalCheckins": 30,
    "checkinFrequency": 85.5
  }
}
```

### 🔍 Insights e Análises

#### GET `/api/v1/insights/trends?period=week`
Análise de tendências dos últimos 7 dias, mês ou ano.

**Parâmetros:**
- `period`: `week`, `month` ou `year` (padrão: `week`)

**Retorna:**
- Tendências de mood, stress e sleep (improving/declining/stable)
- Médias e percentuais de mudança
- Insights e alerts personalizados

#### GET `/api/v1/insights/streak`
Calcula sequência de check-ins consecutivos:
- Sequência atual
- Maior sequência já alcançada
- Status (ativo/inativo)

#### GET `/api/v1/insights/compare?start1=...&end1=...&start2=...&end2=...`
Compara dois períodos de check-ins:
- Médias de cada período
- Mudanças percentuais
- Tendência geral (better/worse/similar)

#### GET `/api/v1/insights/recommended-tips`
Recomenda até 5 dicas personalizadas baseadas em análise inteligente:

**Lógica de Recomendação:**
- **Sleep/Mood**: Recomenda se média ≤ 3.0 OU (média ≤ 3.5 E tendência "declining")
- **Stress**: Recomenda se média ≥ 3.5 OU (tendência "improving" E média ≥ 3.0)
- **Wellness**: Recomendado quando tudo está em bons níveis
- **Priorização**: 1 categoria = 5 tips, 2 categorias = 3+3 tips, 3+ = 2+2+2 tips

**Exemplos:**
- Sleep piorando (3.45 declining) → Recomenda 5 tips de Sleep
- Stress baixo (2.2 declining) → NÃO recomenda Stress
- Múltiplos problemas → Recomenda mix de categorias
- Tudo bem → Recomenda Wellness

**Response 200 OK:**
```json
{
  "success": true,
  "data": [
    {
      "id": "guid",
      "title": "Técnicas de Respiração Profunda",
      "description": "Pratique respiração profunda...",
      "icon": "breath",
      "color": "#FF5722",
      "category": "Sleep",
      "createdAt": "2024-11-14T13:50:18Z"
    }
  ]
}
```

### 🏥 Health Check

#### GET `/health`
Endpoint de health check para monitoramento.

**Response:**
```json
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy"
  }
}
```

## 🔐 Autenticação

A API utiliza **JWT (JSON Web Tokens)** para autenticação.

### Como usar no Swagger:

1. Faça login em `POST /api/v1/auth/login`
2. Copie o `token` da resposta
3. Clique no botão **"Authorize"** (canto superior direito)
4. Cole o token no formato: `Bearer {seu_token}` ou apenas `{seu_token}`
5. Clique em **"Authorize"** e depois **"Close"**

### Como usar em requisições HTTP:

```
Authorization: Bearer {seu_token_aqui}
```

## 📦 Instalação e Configuração

### Pré-requisitos

- **.NET 8 SDK** ([Download](https://dotnet.microsoft.com/download))
- **Visual Studio 2022**, **VS Code** ou **Rider**

### Passos para Executar

1. **Clone o repositório**
   ```bash
   git clone https://github.com/seu-usuario/CareWork-DotNet.git
   cd CareWork-DotNet
   ```

2. **Restaure as dependências**
   ```bash
   dotnet restore
   ```

3. **Configure a connection string** (opcional)
   
   Por padrão, usa SQLite (`Data Source=CareWorkDB.db`).
   
   Para SQL Server, edite `CareWork.API/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CareWorkDB;Trusted_Connection=true;TrustServerCertificate=true;"
     }
   }
   ```

4. **Configure a chave JWT** (opcional)
   
   A chave padrão está em `appsettings.json`. Para produção, use uma chave segura:
   ```json
   {
     "Jwt": {
       "Key": "SuaChaveSecretaComPeloMenos32CaracteresParaHS256",
       "Issuer": "CareWork",
       "Audience": "CareWork",
       "ExpirationMinutes": "1440"
     }
   }
   ```

5. **Execute a aplicação**
   ```bash
   dotnet run --project CareWork.API
   ```

6. **Acesse o Swagger**
   
   Abra o navegador em: `http://localhost:8080/swagger`

### 🎉 Primeira Execução

Na primeira execução, o sistema:
- ✅ Cria o banco de dados automaticamente (SQLite: `CareWorkDB.db`)
- ✅ Executa migrations do Entity Framework Core
- ✅ Popula automaticamente com **20 tips pré-cadastradas** via `DbSeeder`:
  - 5 tips de **Stress** (Técnicas de Respiração, Meditação, etc.)
  - 5 tips de **Sleep** (Rotina de Sono, Ambiente Escuro, etc.)
  - 5 tips de **Mood** (Conexão Social, Hobbies, etc.)
  - 5 tips de **Wellness** (Hidratação, Metas Diárias, etc.)
- ✅ Pronto para uso imediato!

## 🧪 Testes

### Executar Todos os Testes Unitários

```bash
dotnet test
```

### Estatísticas de Testes

- **Total:** 111 testes
- **Passando:** 111 (100% ✅)
- **Cobertura:** 66.9% linhas, 68.09% branches
- **Tipos:** Integração (endpoints), Unitários (services), Validação (DTOs)

### Tipos de Testes

- ✅ **Testes de Integração:** Todos os endpoints testados
- ✅ **Testes Unitários:** Services (AuthService, CheckinService, InsightsService)
- ✅ **Testes de Validação:** DTOs e regras de negócio
- ✅ **Testes de Paginação:** HATEOAS e links
- ✅ **Testes de Autenticação:** Login, registro, atualizações

### Executar com Cobertura

```bash
dotnet test --collect:"XPlat Code Coverage" --settings:coverlet.runsettings
```

### 🧪 Teste Manual Completo de Endpoints

Para testar todos os endpoints na ordem correta (com dependências), execute:

```bash
# Certifique-se de que a API está rodando
dotnet run --project CareWork.API

# Em outro terminal, execute o script de teste
./test-all-endpoints.sh
```

O script testa:
1. ✅ Autenticação (register, login)
2. ✅ Criação de check-ins (7 check-ins variados)
3. ✅ CRUD de check-ins
4. ✅ Listagem e busca de tips
5. ✅ Insights (trends, streak, recommended-tips)
6. ✅ Relatórios (weekly, monthly)
7. ✅ Atualização de perfil e senha
8. ✅ Health check

**Ordem de Dependências:**
- Autenticação → Check-ins → Insights/Relatórios → Perfil
- Tips podem ser testadas independentemente (já pré-cadastradas)

**Validações Automáticas:**
- ✅ Verifica se trends e recommended-tips são consistentes
- ✅ Valida se check-ins foram criados corretamente
- ✅ Confirma se relatórios incluem os dados esperados
- ✅ Testa atualização de senha e novo login

## 📊 Versionamento da API

A API utiliza **versionamento por URL**: `/api/v1/` e `/api/v2/`

### Versões Disponíveis

#### **v1** - Versão Estável
- Todos os endpoints principais
- Endpoints: `/api/v1/auth`, `/api/v1/checkins`, `/api/v1/tips`, `/api/v1/insights`, `/api/v1/reports`
- Versão completa e estável
- **5 controllers**: Auth, Checkins, Tips, Insights, Reports

#### **v2** - Versão com Melhorias
- Endpoints principais com melhorias
- Endpoints: `/api/v2/auth`, `/api/v2/checkins`
- Mantém compatibilidade com V1 (mesma funcionalidade)
- Preparada para futuras expansões
- **2 controllers**: Auth, Checkins (demonstração de versionamento)

### 🔍 O que Muda Ter 2 Versões?

**Na Prática:**
- ✅ **Swagger**: Mostra 2 versões separadas com seletor no topo
- ✅ **URLs**: Você pode usar `/api/v1/` OU `/api/v2/` (ambas funcionam)
- ✅ **Logs**: Identificam qual versão foi usada (ex: "V2: Retrieved...")
- ✅ **Estrutura**: Código organizado em `Controllers/V1/` e `Controllers/V2/`

**Funcionalidade:**
- Por enquanto, V1 e V2 têm a mesma funcionalidade (compatibilidade)
- V2 pode evoluir no futuro sem quebrar V1
- Demonstra versionamento real e controle adequado de rotas

**Exemplo:**
```bash
# Ambas funcionam:
POST /api/v1/checkins → ✅ Funciona
POST /api/v2/checkins → ✅ Também funciona
```

### Estratégia

- **Versionamento por URL**: Cada versão tem seu próprio prefixo
- **Compatibilidade**: V2 mantém compatibilidade com V1
- **Swagger**: Ambas as versões documentadas no Swagger UI
- **Estrutura**: Controllers organizados em `Controllers/V1/` e `Controllers/V2/`
- **Breaking changes**: Resultam em nova versão
- **Versões antigas**: Mantidas para compatibilidade

## 🔍 Monitoramento e Observabilidade

### Health Check

Endpoint disponível em `/health` para verificação de saúde da aplicação e banco de dados.

### Logging Estruturado

- **Serilog** configurado
- Logs em console e arquivo (`logs/carework-YYYYMMDD.txt`)
- Logs estruturados com contexto (UserId, CheckinId, etc.)
- Níveis configuráveis via `appsettings.json`

### Tracing Distribuído

- **OpenTelemetry** configurado
- Instrumentação automática de requisições HTTP e ASP.NET Core
- Preparado para exportação para Application Insights ou outros sistemas

## 📝 Boas Práticas Implementadas

### REST

✅ **Paginação**: Todos os endpoints de listagem  
✅ **HATEOAS**: Links de navegação (first, last, next, previous)  
✅ **Status Codes**: Uso correto (200, 201, 204, 400, 401, 404, 500)  
✅ **Verbos HTTP**: GET, POST, PUT, DELETE  
✅ **Estrutura Padronizada**: `ApiResponseDto<T>` em todas as respostas

### Arquitetura

✅ **Separação de Camadas**: API, Infrastructure, Tests  
✅ **Dependency Injection**: Todos os serviços registrados  
✅ **Services Pattern**: Lógica de negócio nos Services  
✅ **DTOs**: Separação entre modelos de domínio e DTOs  
✅ **AutoMapper**: Mapeamento automático entre entidades e DTOs

### Segurança

✅ **JWT Authentication**: Tokens com expiração configurável  
✅ **Password Hashing**: BCrypt para hash de senhas  
✅ **Authorization**: Endpoints protegidos com `[Authorize]`  
✅ **CORS**: Configurado para permitir requisições do frontend  
✅ **Validação**: Data Annotations e ModelState validation

### Qualidade

✅ **Testes Abrangentes**: 111 testes cobrindo todos os endpoints  
✅ **Tratamento de Erros**: Middleware customizado  
✅ **Logging**: Logs estruturados para debugging  
✅ **Documentação**: Swagger/OpenAPI completo

## 🎯 Features Implementadas

### Check-ins

- ✅ CRUD completo (Create, Read, Update, Delete)
- ✅ Notas opcionais (até 1000 caracteres)
- ✅ Tags para categorização
- ✅ Paginação com HATEOAS
- ✅ Filtro por usuário (apenas próprios check-ins)

### Insights

- ✅ **Trends**: Análise de tendências (week/month/year)
  - Calcula médias e percentuais de mudança
  - Identifica tendências: improving/declining/stable
  - Gera insights e alerts personalizados
- ✅ **Streak**: Sequência de check-ins consecutivos
  - Sequência atual e maior sequência já alcançada
  - Status ativo/inativo
- ✅ **Compare**: Comparação entre dois períodos
  - Médias de cada período
  - Mudanças percentuais e tendência geral
- ✅ **Recommended Tips**: Recomendações inteligentes
  - Baseadas em médias e tendências dos últimos 7 dias
  - Lógica consistente com análise de trends
  - Priorização por urgência (piorando = alta prioridade)

### Relatórios

- ✅ **Weekly Report**: Relatório semanal completo
- ✅ **Monthly Report**: Relatório mensal com análises detalhadas

### Tips

- ✅ **20 Tips Pré-cadastradas**: Criadas automaticamente via `DbSeeder` na primeira execução
- ✅ **Categorias**: Stress (5), Sleep (5), Mood (5), Wellness (5)
- ✅ **Recomendações Inteligentes**: 
  - Baseadas em **médias** dos últimos 7 dias
  - Considera **tendências** (piorando = precisa de ajuda)
  - **Não recomenda** se está melhorando (lógica consistente com trends)
  - Priorização inteligente por quantidade de categorias problemáticas

### Autenticação

- ✅ Registro e login
- ✅ Atualização de perfil
- ✅ Mudança de senha
- ✅ Exclusão de conta (com confirmação)

## 🔍 Consistência e Qualidade

### ✅ Validações de Consistência

A API foi validada para garantir que todas as lógicas estão "conversando bem" entre si:

- ✅ **Filtro por UserId**: Consistente em todos os endpoints
- ✅ **Cálculos de médias**: Usam a mesma lógica (`checkins.Average()`)
- ✅ **Lógica de trends**: `GetTrendsAsync` e `GetRecommendedTipsAsync` usam a mesma análise
- ✅ **Períodos de data**: Inclusão correta de limites (inclui último dia)
- ✅ **Validações**: Consistentes em todos os endpoints
- ✅ **Autorização**: JWT validado em todos os endpoints protegidos

### 📊 Status dos Requisitos

**Boas Práticas REST (30 pts):** ✅ Completo
- Paginação, HATEOAS, Status Codes, Verbos HTTP

**Monitoramento e Observabilidade (15 pts):** ✅ Completo
- Health Check, Logging (Serilog), Tracing (OpenTelemetry)

**Versionamento da API (10 pts):** ✅ Completo
- `/api/v1/` implementado e documentado

**Integração e Persistência (30 pts):** ✅ Completo
- Entity Framework Core, Migrations, SQLite/SQL Server

**Testes (15 pts):** ✅ Completo
- 111 testes (100% passando), Cobertura 66.9%

## 🚀 Deploy

### Preparação para Produção

1. **Configure connection string** para SQL Server (ou outro banco de produção)
2. **Configure chave JWT segura** (mínimo 32 caracteres)
3. **Ajuste níveis de log** (produção: Information ou Warning)
4. **Configure OpenTelemetry** para exportação (Application Insights, Jaeger, etc.)
5. **Configure CORS** para domínios específicos do frontend
6. **Configure HTTPS** obrigatório em produção

### Plataformas Recomendadas

- **Azure App Service** - Integração nativa com .NET
- **AWS Elastic Beanstalk** - Suporte completo para .NET
- **Railway** - Deploy simples e rápido
- **Heroku** - Suporte para .NET Core
- **GitHub Actions** - CI/CD automático

## 🤝 Contribuindo

1. Faça um fork do projeto
2. Crie uma branch (`git checkout -b feature/AmazingFeature`)
3. Commit suas mudanças (`git commit -m 'feat: Add AmazingFeature'`)
4. Push para a branch (`git push origin feature/AmazingFeature`)
5. Abra um Pull Request

## 📄 Licença

Este projeto está sob a licença MIT.

## 👥 Autores

- Equipe CareWork

## ✅ Checklist de Entrega

### Funcionalidades Core
- [x] API funcionando com todos os endpoints (12 endpoints principais)
- [x] Autenticação JWT completa (register, login, profile, password, account)
- [x] CRUD completo de Check-ins (com Notes e Tags)
- [x] CRUD completo de Tips (20 pré-cadastradas)
- [x] Insights e análises (trends, streak, compare, recommended-tips)
- [x] Relatórios semanais e mensais

### Boas Práticas REST
- [x] Paginação implementada em todos os endpoints de listagem
- [x] HATEOAS nas respostas paginadas
- [x] Status codes adequados (200, 201, 204, 400, 401, 404, 500)
- [x] Estrutura padronizada de resposta (`ApiResponseDto<T>`)

### Monitoramento e Observabilidade
- [x] Health Check configurado (`/health`)
- [x] Logging estruturado (Serilog) - console e arquivo
- [x] Tracing distribuído (OpenTelemetry)

### Arquitetura e Qualidade
- [x] Versionamento `/api/v1/` implementado
- [x] Entity Framework com Migrations (Code First)
- [x] Separação de camadas (API, Infrastructure, Tests)
- [x] Dependency Injection configurado
- [x] AutoMapper para mapeamento de objetos

### Testes
- [x] Testes xUnit (111 testes, 100% passando)
- [x] Testes de integração (todos os endpoints)
- [x] Testes unitários (services)
- [x] Cobertura de código (66.9% linhas)

### Documentação
- [x] Swagger/OpenAPI completo e funcional
- [x] README completo e atualizado

### Features Avançadas
- [x] Tips pré-cadastradas (20 tips iniciais via DbSeeder)
- [x] Insights e recomendações inteligentes (lógica refinada)
- [x] Relatórios semanais e mensais com análises detalhadas
- [x] Lógica de recomendações baseada em médias e tendências

### Pendências (Entrega)
- [ ] Deploy em produção
  - Escolher plataforma (Azure, AWS, Railway, etc.)
  - Configurar CI/CD
  - Documentar link e credenciais
- [ ] Vídeo demonstrativo (máx 5 min)
  - Gravar vídeo mostrando funcionamento integrado
  - Publicar no YouTube
  - Adicionar link no README

## 📊 Status dos Requisitos da Disciplina

#### 1. Boas Práticas REST (30 pts) ✅
- ✅ **Paginação**: Implementada em todos os endpoints de listagem
  - Query parameters: `page` e `pageSize`
  - Resposta inclui: `page`, `pageSize`, `totalCount`, `totalPages`, `hasPreviousPage`, `hasNextPage`
- ✅ **HATEOAS**: Links de navegação implementados
  - Links: `self`, `first`, `last`, `previous`, `next`
  - Implementado em `PagedResponseDto<T>`
- ✅ **Status Codes**: Uso correto (200, 201, 204, 400, 401, 404, 500)
- ✅ **Verbos HTTP**: GET, POST, PUT, DELETE corretamente implementados

#### 2. Monitoramento e Observabilidade (15 pts) ✅
- ✅ **Health Check**: Endpoint `/health` implementado
  - Health check do banco de dados (Entity Framework)
  - Resposta: `{ "status": "Healthy", "checks": { "database": "Healthy" } }`
- ✅ **Logging**: Serilog configurado
  - Logging estruturado
  - Logs em console e arquivo (`logs/carework-YYYYMMDD.txt`)
  - Níveis configuráveis via `appsettings.json`
- ✅ **Tracing**: OpenTelemetry configurado
  - Instrumentação automática de requisições HTTP e ASP.NET Core
  - Preparado para exportação para Application Insights ou outros sistemas

#### 3. Versionamento da API (10 pts) ✅
- ✅ **Estrutura**: Versões implementadas (`/api/v1/` e `/api/v2/`)
  - Controllers organizados em `Controllers/V1/` e `Controllers/V2/`
  - Estrutura preparada para futuras versões (`/api/v3/`, etc.)
  - Exemplos: `[Route("api/v1/checkins")]` e `[Route("api/v2/checkins")]`
- ✅ **Controle Adequado**: Rotas versionadas corretamente
  - V1: 5 controllers completos (auth, checkins, tips, insights, reports)
  - V2: 2 controllers implementados (auth, checkins) demonstrando versionamento
  - Swagger documenta ambas as versões separadamente
- ✅ **Documentação**: Estratégia explicada no README
  - Seção "Versionamento da API" com versões disponíveis
  - Explicação de compatibilidade entre versões
  - Estratégia de quando criar novas versões (breaking changes)

#### 4. Integração e Persistência (30 pts) ✅
- ✅ **Entity Framework Core**: Configurado
  - SQLite para desenvolvimento (macOS)
  - SQL Server para produção (configurável)
  - Code First approach
- ✅ **Migrations**: Implementado
  - Migrations criadas e aplicadas
  - Executadas automaticamente na primeira execução

#### 5. Testes Integrados (15 pts) ✅
- ✅ **xUnit**: 111 testes implementados
  - 100% passando ✅
  - Cobertura: 66.9% linhas, 68.09% branches
- ✅ **Tipos**: Integração, Unitários, Validação

### ✅ Requisitos Opcionais

- ✅ **Autenticação JWT**: Implementado (bonus)
  - Tokens JWT com expiração configurável
  - Password hashing com BCrypt
  - Endpoints protegidos com `[Authorize]`
- ❌ **ML.NET**: Não implementado (opcional)

### ⚠️ Itens de Entrega (Pendentes)

- ⚠️ **Deploy da API**: Pendente
  - Escolher plataforma (Azure, AWS, Railway, etc.)
  - Configurar CI/CD
  - Documentar link e credenciais no README
  
- ⚠️ **Vídeo Demonstrativo**: Pendente
  - Gravar vídeo (máximo 5 minutos)
  - Demonstrar funcionamento integrado
  - Publicar no YouTube
  - Adicionar link no README

### 🎯 Melhorias Sugeridas (Opcional)

- [ ] Aumentar cobertura de testes para 80%+
- [ ] Adicionar rate limiting
- [ ] Implementar cache para melhor performance
- [ ] Adicionar ML.NET para previsões de bem-estar (se tiver tempo)

---

**Desenvolvido para a disciplina Advanced Business Development with .NET**
