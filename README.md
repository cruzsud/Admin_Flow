# AdminFlow

AdminFlow é uma plataforma administrativa desenvolvida em C# e ASP.NET Core para praticar engenharia de software com um domínio de negócio realista.

O primeiro módulo, `AdminFlow.Budget`, modela gestão orçamentária, solicitações de despesa, aprovação, persistência e comunicação assíncrona. O projeto evolui de forma incremental: compreender as decisões arquiteturais é tão importante quanto implementar funcionalidades.

## Estado atual

A Fase 9 — Confiabilidade do RabbitMQ está concluída. O consumidor possui acknowledgement manual, retry limitado, DLQ e idempotência persistida no PostgreSQL.

Atualmente o projeto possui:

- centros de custo;
- orçamentos anuais;
- valores alocado, comprometido e disponível;
- solicitações de despesa pendentes;
- aprovação e rejeição;
- proteção contra saldo insuficiente e autoaprovação;
- persistência com PostgreSQL e Entity Framework Core;
- controle de concorrência otimista com `xmin`;
- logging estruturado com Serilog;
- publicação e consumo do evento `ExpenseApproved` com RabbitMQ;
- testes unitários, de Application e de integração.

Ainda não existem endpoints HTTP de negócio para criar ou decidir solicitações. A API expõe apenas recursos técnicos, como health check e Swagger. A próxima fase planejada introduz observabilidade com OpenTelemetry.

## Domínio

O fluxo principal é:

```text
CostCenter
    ↓
Budget
    ↓
ExpenseRequest
    ↓
Pending ──→ Approved
    └─────→ Rejected
```

O modelo orçamentário utiliza:

```text
Available = Allocated - Committed
```

- `Allocated`: valor autorizado para o orçamento.
- `Committed`: valor reservado por solicitações aprovadas.
- `Available`: valor que ainda pode ser comprometido.
- `Spent`: pagamento efetivo; ainda não faz parte do MVP.

Criar uma solicitação pendente não altera o orçamento. Somente uma aprovação aumenta `Committed`.

## Arquitetura

```text
src/
  AdminFlow.Budget.Api
  AdminFlow.Budget.Application
  AdminFlow.Budget.Domain
  AdminFlow.Budget.Infrastructure

tests/
  AdminFlow.Budget.UnitTests
  AdminFlow.Budget.IntegrationTests
```

Responsabilidades:

- `Domain`: entidades, estados, cálculos e regras de negócio.
- `Application`: coordenação dos casos de uso e contratos externos.
- `Infrastructure`: Entity Framework Core, PostgreSQL, RabbitMQ e implementações técnicas.
- `Api`: host ASP.NET Core, configuração, Swagger, health check e injeção de dependência.

Direção principal das dependências:

```text
API → Application → Domain
  └→ Infrastructure → Application / Domain
```

O `Domain` não depende de ASP.NET Core, Entity Framework Core, PostgreSQL, Serilog ou RabbitMQ.

## Tecnologias

- .NET 10 e C#;
- ASP.NET Core Web API;
- Entity Framework Core;
- PostgreSQL 17;
- Docker Compose;
- Swagger / OpenAPI;
- Serilog;
- RabbitMQ 4.1;
- RabbitMQ.Client 7;
- xUnit.

## Pré-requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0);
- Docker Desktop ou outro ambiente compatível com Docker Compose;
- PowerShell para executar os exemplos abaixo.

O SDK esperado está definido em `global.json`.

## Configuração local

Crie o arquivo local de variáveis a partir do exemplo:

```powershell
Copy-Item .env.example .env
```

Preencha senhas locais em `.env`:

```dotenv
ADMINFLOW_POSTGRES_PASSWORD=escolha-uma-senha-local
ADMINFLOW_POSTGRES_PORT=15432
ADMINFLOW_RABBITMQ_PASSWORD=escolha-outra-senha-local
ADMINFLOW_RABBITMQ_PORT=5672
ADMINFLOW_RABBITMQ_MANAGEMENT_PORT=15672
```

O arquivo `.env` não deve ser versionado.

Suba a infraestrutura:

```powershell
docker compose up -d
docker compose ps
```

Serviços locais:

| Serviço | Endereço |
|---|---|
| PostgreSQL | `localhost:15432` |
| RabbitMQ AMQP | `localhost:5672` |
| RabbitMQ Management | `http://localhost:15672` |

O usuário local dos dois serviços é `adminflow`.

## Banco de dados

Configure a connection string no terminal:

```powershell
$env:ConnectionStrings__BudgetDatabase="Host=localhost;Port=15432;Database=adminflow_budget;Username=adminflow;Password=escolha-uma-senha-local"
```

Aplique as migrations:

```powershell
dotnet ef database update `
  --project src/AdminFlow.Budget.Infrastructure `
  --startup-project src/AdminFlow.Budget.Infrastructure `
  --context BudgetDbContext
```

## Executando a API

Para executar sem mensageria, mantenha `RabbitMq:Enabled` desabilitado e rode:

```powershell
dotnet run --project src/AdminFlow.Budget.Api
```

Para habilitar RabbitMQ:

```powershell
$env:RabbitMq__Enabled="true"
$env:RabbitMq__UserName="adminflow"
$env:RabbitMq__Password="escolha-outra-senha-local"
# Opcionais: padrões de 3 retries e 5000 ms
$env:RabbitMq__MaxRetryAttempts="3"
$env:RabbitMq__RetryDelayMilliseconds="5000"

dotnet run --project src/AdminFlow.Budget.Api
```

Endereços de desenvolvimento:

- API HTTP: `http://localhost:5202`;
- API HTTPS: `https://localhost:7290`;
- health check: `https://localhost:7290/health`;
- Swagger: `https://localhost:7290/swagger`.

As portas podem ser alteradas em `launchSettings.json`.

## RabbitMQ

Uma aprovação persistida produz o evento:

```text
ExpenseApprovedIntegrationEvent
    ↓
exchange: adminflow.budget
routing key: expense.approved
    ↓
queue: adminflow.budget.expense-approved
    ↓
ExpenseApprovedConsumer
```

O evento transporta identificadores, valor, moeda e instante da aprovação. Ele não transporta descrição, motivo de rejeição ou credenciais.

A implementação usa confirmação manual: mensagens válidas recebem `Ack` somente depois do processamento. Falhas transitórias passam por uma fila de retry com intervalo e limite configuráveis; mensagens inválidas ou que esgotam as tentativas seguem para uma dead-letter queue.

O `EventId` de cada mensagem concluída é armazenado em `processed_integration_events`. Uma entrega duplicada é confirmada sem executar novamente o handler. Ainda não existem publisher confirms ou Outbox transacional.

## Compilação e testes

Compilar toda a Solution:

```powershell
dotnet build AdminFlow.sln
```

Executar testes sem infraestrutura externa:

```powershell
dotnet test AdminFlow.sln
```

Os testes que dependem de PostgreSQL ou RabbitMQ serão ignorados quando suas variáveis não estiverem configuradas.

Para executar a suíte completa:

```powershell
$env:ADMINFLOW_TEST_DB_CONNECTION_STRING="Host=localhost;Port=15432;Database=adminflow_budget;Username=adminflow;Password=escolha-uma-senha-local"
$env:ADMINFLOW_TEST_RABBITMQ_PASSWORD="escolha-outra-senha-local"

dotnet test AdminFlow.sln
```

Na conclusão da Fase 9, o resultado validado foi:

- 53 testes unitários e de Application;
- 32 testes de integração;
- 85 testes aprovados;
- 0 falhas;
- build com 0 erros e 0 avisos.

## Segurança e configuração

- Não versione `.env`, senhas ou connection strings completas.
- Utilize variáveis de ambiente ou user secrets para credenciais.
- Logs não devem conter tokens, senhas ou headers de autorização.
- Os identificadores de solicitante e decisor ainda não representam usuários autenticados.
- Autenticação e autorização estão planejadas para uma fase posterior.

## Documentação

- [Arquitetura](docs/architecture.md)
- [Domínio](docs/domain.md)
- [Status do projeto](docs/project-status.md)
- [Decisões arquiteturais](docs/decisions)

## Roadmap resumido

- [x] Fundação ASP.NET Core;
- [x] CostCenter;
- [x] PostgreSQL e Entity Framework Core;
- [x] Budget;
- [x] ExpenseRequest;
- [x] fluxo de aprovação;
- [x] logging estruturado;
- [x] fundamentos de RabbitMQ;
- [x] confiabilidade do RabbitMQ — acknowledgement manual, retry, DLQ e idempotência;
- [ ] OpenTelemetry;
- [ ] autenticação e autorização;
- [ ] AdminFlow.People;
- [ ] integração entre os sistemas.

## Licença

Nenhuma licença foi definida até o momento.
