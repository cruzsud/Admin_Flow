# Arquitetura planejada do AdminFlow.Budget

## Contexto

O AdminFlow.Budget é uma única aplicação ASP.NET Core organizada por responsabilidades. A separação em projetos serve para tornar dependências explícitas e manter regras de negócio independentes de HTTP e persistência; ela não representa microserviços.

A fundação foi criada na Fase 1 com .NET 10, uma API mínima, OpenAPI/Swagger e health check. A Fase 2 adicionou `CostCenter` ao Domain e a Fase 3 introduziu sua persistência. A Fase 4 adicionou o modelo mínimo de `Budget` e sua persistência, ainda sem casos de uso ou endpoints de negócio.

## Estrutura proposta

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

### AdminFlow.Budget.Domain

Contém entidades, estados, invariantes, erros de domínio e comportamento financeiro. Não conhece ASP.NET Core, Entity Framework Core, PostgreSQL, Serilog ou RabbitMQ.

Implementação atual: `CostCenters/CostCenter.cs`, entidade independente com identidade, código e nome. Suas invariantes locais são testadas diretamente por `UnitTests`.

### AdminFlow.Budget.Application

Expõe e coordena casos de uso. Carrega entidades, chama comportamento de domínio e solicita persistência por limites específicos quando necessários. Não contém detalhes HTTP e não deve absorver regras que pertencem ao domínio.

### AdminFlow.Budget.Infrastructure

Implementa EF Core, PostgreSQL, mapeamentos e migrations. Futuramente conterá outras integrações externas. Não decide regras de domínio, como se uma despesa pode ser aprovada.

### AdminFlow.Budget.Api

É o ponto de entrada HTTP e composition root. Valida o formato das requisições, chama os casos de uso, converte resultados em respostas HTTP e registra as dependências concretas. Controllers ou endpoints devem permanecer finos.

### Testes

- `UnitTests`: começa protegendo invariantes e transições do domínio; pode incluir casos de uso isolados.
- `IntegrationTests`: verifica persistência no PostgreSQL e o pipeline HTTP da fundação.

Separar os dois projetos de teste desde o início é aceitável porque eles terão velocidades e dependências diferentes. Projetos adicionais não são justificados agora.

## Fundação ASP.NET Core implementada

- `AdminFlow.sln` agrupa os seis projetos.
- `global.json` seleciona o SDK .NET 10.0.301, aceitando patches mais recentes da mesma linha.
- A API usa Minimal API apenas como host inicial; a escolha entre Minimal APIs e controllers para endpoints de negócio continua aberta.
- A injeção de dependência nativa registra health checks e geração de OpenAPI.
- `GET /health` existe em todos os ambientes e retorna apenas o estado técnico básico.
- Swagger JSON e Swagger UI existem somente em `Development`.
- Redirecionamento HTTPS está ativo; HSTS é aplicado fora de `Development`.
- Não há banco, Docker, autenticação, logging estruturado ou endpoint de negócio.

### Dependências externas atuais

- `Swashbuckle.AspNetCore`: gera o documento OpenAPI e a interface Swagger.
- `Microsoft.AspNetCore.Mvc.Testing`: hospeda a API em memória nos testes HTTP.
- xUnit, test SDK e coverlet: infraestrutura dos projetos de teste criada pelos templates oficiais.

Domain e Application continuam sem pacotes externos. Infrastructure depende de EF Core Design e do provider Npgsql; a API usa Swashbuckle. A auditoria do NuGet ao final da Fase 3 não encontrou pacotes vulneráveis nas fontes consultadas.

## Persistência implementada na Fase 3

```text
Api
  -> AddInfrastructure(connectionString)
  -> container de injeção de dependência
  -> BudgetDbContext
  -> provider Npgsql
  -> PostgreSQL
```

- `BudgetDbContext` representa a sessão do EF Core e expõe `CostCenters` e `Budgets`.
- `CostCenterConfiguration` adapta a entidade à tabela `cost_centers`, sem anotações de persistência no Domain.
- `InitialCreate` cria tabela, chave primária e índice único de código.
- `BudgetConfiguration` adapta a entidade à tabela `budgets`, com `numeric(18,2)`, checks financeiros, chave estrangeira restritiva e unicidade por centro/exercício.
- `AddBudgets` cria a segunda tabela e seu relacionamento sem alterar `cost_centers`.
- `DesignTimeBudgetDbContextFactory` permite que `dotnet ef` crie o contexto sem depender do host HTTP.
- `DependencyInjection.AddInfrastructure` é o ponto único de registro da persistência.
- PostgreSQL 17 é disponibilizado localmente pelo `compose.yaml` na porta `15432`, evitando colisão com uma instalação local em `5432`.
- A senha não é versionada. Compose exige `ADMINFLOW_POSTGRES_PASSWORD`; aplicação e migrations recebem a connection string completa por variável de ambiente.

### Decisão sobre Repository

Nenhum repository genérico ou wrapper de Unit of Work foi criado. Neste estágio, `DbContext` já oferece tracking, consulta e confirmação de alterações. Uma abstração específica só será introduzida quando um caso de uso da Application precisar de um limite que evite dependência direta de EF Core.

### Testes de persistência

Os testes usam PostgreSQL real iniciado pelo Compose, pois um provider em memória não reproduziria tipos `uuid`, `numeric(18,2)`, migrations, chaves estrangeiras, checks ou índices únicos. Testcontainers foi adiado: o Compose já fornece a infraestrutura necessária com menos dependências. Os testes PostgreSQL são habilitados pela variável `ADMINFLOW_TEST_DB_CONNECTION_STRING`; sem ela, são explicitamente ignorados. As classes de persistência compartilham uma collection xUnit não paralela para não disputar a limpeza das mesmas tabelas.

## Dependências

```text
Domain                         (nenhuma referência interna)
Application  ----------------> Domain
Infrastructure --------------> Application, Domain
Api -----------> Application, Infrastructure (somente composição)
UnitTests --------------------> Domain, Application quando necessário
IntegrationTests ------------> Api, Infrastructure
```

O sentido das referências de compilação não é uma sequência de execução. `Api` referencia `Infrastructure` para registrar implementações concretas, enquanto as regras continuam apontando para dentro, em direção ao domínio.

### Vantagens

- Regras financeiras testáveis sem servidor ou banco.
- Limites didáticos e local previsível para cada responsabilidade.
- Troca/evolução da persistência sem contaminar o domínio.
- Estrutura suficiente para o crescimento planejado.

### Custos e limites

- Quatro projetos geram mais arquivos e referências que uma API única.
- Mapeamentos entre HTTP, aplicação e domínio podem acrescentar código.
- Interfaces sem necessidade concreta seriam excesso; não serão criadas automaticamente.
- Não serão usados CQRS, MediatR, repositórios genéricos, `UnitOfWork` customizado ou classes base genéricas.

## API REST mínima proposta

| Método e rota | Finalidade | Sucesso | Erros principais |
|---|---|---|---|
| `POST /api/cost-centers` | criar centro de custo | `201 Created` com `Location` | `400` dados inválidos; `409` código duplicado |
| `GET /api/cost-centers/{id}` | consultar centro | `200 OK` | `404` não encontrado |
| `POST /api/budgets` | criar orçamento anual | `201 Created` com `Location` | `400` dados inválidos; `404` centro inexistente; `409` orçamento duplicado |
| `GET /api/budgets/{id}` | consultar valores e saldo | `200 OK` | `404` não encontrado |
| `POST /api/expense-requests` | criar solicitação pendente | `201 Created` com `Location` | `400` dados inválidos; `404` orçamento inexistente |
| `GET /api/expense-requests/{id}` | consultar estado e decisão | `200 OK` | `404` não encontrada |
| `GET /api/expense-requests` | listar e filtrar solicitações | `200 OK` | `400` filtro/paginação inválido |
| `POST /api/expense-requests/{id}/approve` | executar a ação de aprovação | `200 OK` com estado e saldo atualizados | `400` decisor inválido; `404` não encontrada; `409` estado inválido, saldo insuficiente ou concorrência |
| `POST /api/expense-requests/{id}/reject` | executar a ação de rejeição | `200 OK` com decisão | `400` decisor/motivo inválido; `404` não encontrada; `409` estado inválido |

Rotas de ação (`approve` e `reject`) são adequadas porque expressam transições de negócio, em vez de permitir um `PATCH` genérico no status. O corpo não define o novo estado arbitrariamente.

Autenticação não faz parte do MVP inicial. Os identificadores de solicitante/decisor nos contratos serão provisórios e nunca devem ser confundidos com identidade autenticada.

Os contratos HTTP serão modelos explícitos, diferentes das entidades. Clientes não poderão enviar `Status`, `Committed`, `ApprovedAt` ou outros campos controlados pelo servidor. Erros não deverão expor stack traces, SQL, connection strings ou caminhos internos.

## Fluxo conceitual: aprovar ExpenseRequest

```text
POST /api/expense-requests/{id}/approve
                    |
                    v
API -> Application -> Domain -> Infrastructure -> PostgreSQL
 ^          |           |             |
 |          |           |             +-- carrega e persiste na mesma transação
 |          |           +-- valida estado/saldo e altera estado/compromisso
 |          +-- coordena carregamento, decisão e persistência
 +-- traduz HTTP, entrada, resultado e erros
```

1. **API:** recebe id e decisor, valida o contrato e chama o caso de uso.
2. **Application:** obtém solicitação e orçamento, coordena a operação e delimita a unidade lógica.
3. **Domain:** exige estado pendente e saldo suficiente; aprova e compromete o valor.
4. **Infrastructure:** usa EF Core para carregar e persistir as mudanças em transação, com proteção de concorrência.
5. **PostgreSQL:** aplica constraints e confirma atomicamente a alteração.
6. **API:** devolve `200`, ou mapeia ausência para `404`, validação para `400` e conflito de estado/saldo/concorrência para `409`.

PostgreSQL e a persistência de `CostCenter`, `Budget` e `ExpenseRequest` já existem. Na Fase 6, o fluxo de aprovação passou a ser implementado por `ExpenseApprovalService`, que carrega as entidades por `IExpenseApprovalStore`, executa o comportamento do Domain e grava as alterações em uma única chamada.

## Persistência de ExpenseRequest

```text
Domain: ExpenseRequest
          |
          v
Infrastructure: ExpenseRequestConfiguration
          |
          v
PostgreSQL: expense_requests -> budgets
```

`BudgetDbContext` expõe `ExpenseRequests` para inclusão e consulta. O mapeamento mantém a entidade de domínio independente do EF Core, persiste o status como inteiro, cria índice por `budget_id` e impede no banco solicitações associadas a orçamento inexistente. Casos de uso e endpoints continuam adiados; não foi criada uma abstração de repository porque o `DbContext` já atende à persistência desta fase.

## Aprovação, atomicidade e concorrência

```text
ExpenseApprovalService (Application)
       |
       v
IExpenseApprovalStore (contrato)
       |
       v
BudgetDbContext (Infrastructure)
       |
       +-- ExpenseRequest.Approve/Reject
       +-- Budget.Commit
       |
       v
SaveChanges -> transação PostgreSQL
```

O próprio `BudgetDbContext` implementa o contrato específico; não existe repository genérico nem wrapper genérico de Unit of Work. O EF Core envolve as atualizações de uma aprovação em transação, garantindo que solicitação e orçamento sejam confirmados ou revertidos juntos.

O campo interno `xmin` do PostgreSQL é usado como token de concorrência otimista do orçamento. Ele não aparece na entidade de domínio nem é criado pela migration. Se outra operação alterar o orçamento desde sua leitura, o `UPDATE` não encontra a versão esperada e o EF Core lança `DbUpdateConcurrencyException`, revertendo a transação. A futura API deverá traduzir esse conflito para uma resposta apropriada, sem expor detalhes do banco.

## Logging estruturado

```text
Domain
  sem dependência de logging

Application
  ExpenseApprovalService -> ILogger<ExpenseApprovalService>
                                  |
                                  v
API / composição
  Serilog -> Console
  SerilogRequestLogging -> eventos HTTP
```

A Application depende apenas de `Microsoft.Extensions.Logging.Abstractions`. O Serilog é configurado na API como implementação concreta e lê níveis e sink do `appsettings.json`. Isso preserva o Domain e evita acoplar casos de uso diretamente ao Serilog.

Eventos de aprovação e rejeição são emitidos em `Information` somente depois de `SaveChanges`. Cada evento contém `ExpenseRequestId`, `BudgetId`, `DecisionMakerId`, `Amount`, `Action` e `OccurredAt` como propriedades separadas. Descrição, motivo de rejeição, credenciais, connection strings, tokens e cabeçalhos de autorização não são registrados.

O middleware de requisição gera um evento por chamada HTTP com método, caminho, status e duração. Corpo, query string e headers não são enriquecidos nesta fase para reduzir risco de exposição. Persistência em arquivo ou serviço externo também foi adiada; o sink inicial é apenas o console.

## Roadmap revisado

| Fase | Conteúdo | Observação |
|---|---|---|
| 0 | Planejamento | domínio, MVP, arquitetura e contratos conceituais |
| 1 | Fundação ASP.NET Core | Solution, projetos, referências, API, OpenAPI e health check |
| 2 | Primeiro domínio: CostCenter | entidade, regras e testes unitários, ainda sem persistência |
| 3 | PostgreSQL e EF Core | persistir CostCenter; DbContext, mapping, migration e Compose mínimo |
| 4 | Budget | semântica financeira, criação/consulta, persistência e testes |
| 5 | ExpenseRequest | criação pendente, consulta/listagem, validação e persistência |
| 6 | Aprovação | aprovação/rejeição, compromisso atômico, concorrência e testes |
| 7 | Logging estruturado | Serilog com contexto, sem secrets |
| 8 | Fundamentos de RabbitMQ | um evento de integração, producer e consumer simples |
| 9 | Confiabilidade RabbitMQ | acknowledgement, retry, DLQ e idempotência, um tópico por vez |
| 10 | Observabilidade | OpenTelemetry quando houver HTTP, banco e mensageria a observar |
| 11 | Segurança | autenticação, autorização e segregação progressiva |
| 12 | AdminFlow.People | segundo sistema independente |
| 13 | Integração | REST para confirmação imediata; eventos para propagação assíncrona |

A alteração relevante é colocar `Budget` antes de `ExpenseRequest`. Assim, cada incremento encontra as dependências de negócio já modeladas, e a solicitação não nasce com uma associação temporária ou falsa. A aprovação continua separada para concentrar regras de transição, transação e concorrência.

## Estratégia futura de RabbitMQ

O encadeamento planejado faz sentido:

```text
aprovação confirmada no PostgreSQL
  -> ExpenseApproved integration event
  -> RabbitMQ
  -> consumer inicial de auditoria/processamento
```

## Fundamentos de RabbitMQ implementados

```text
ExpenseApprovalService
  -> SaveChanges (PostgreSQL)
  -> ExpenseApprovedIntegrationEvent
  -> IExpenseApprovedPublisher
  -> RabbitMqExpenseApprovedPublisher
  -> exchange adminflow.budget
  -> routing key expense.approved
  -> queue adminflow.budget.expense-approved
  -> ExpenseApprovedConsumer
  -> log estruturado
```

O contrato do evento e a interface de publicação ficam na Application. O cliente RabbitMQ, produtor, topologia e consumidor hospedado ficam na Infrastructure. O Domain não conhece mensageria. A API habilita a integração apenas quando `RabbitMq:Enabled=true` e atua como composition root.

O evento contém `EventId`, `ExpenseRequestId`, `BudgetId`, `DecisionMakerId`, `Amount`, `Currency` e `ApprovedAt`. Ele representa um fato já confirmado; rejeições não publicam evento de aprovação. A fila e o exchange são duráveis e as mensagens são marcadas como persistentes, mas confirmações do broker ainda não foram implementadas.

Nesta fase, a publicação acontece depois do commit do PostgreSQL. Isso impede publicar uma aprovação não persistida, mas deixa uma janela de falha entre banco e broker.

Na Fase 9.1, o consumidor passou a usar `autoAck=false`. Um evento válido recebe `BasicAck` somente depois do processamento.

Na Fase 9.2, falhas transitórias deixaram de usar requeue imediato. A fila principal encaminha a mensagem para uma fila de retry, cujo TTL introduz um intervalo antes de devolvê-la à fila principal. O consumidor conta as mortes na fila principal pelo cabeçalho `x-death`; após o limite configurado, encaminha a mensagem para a DLQ. JSON ou contrato inválido segue diretamente para a DLQ. O padrão inicial é 3 retries com intervalo fixo de 5 segundos.

```text
adminflow.budget.expense-approved
  ├── falha transitória -> adminflow.budget.expense-approved.retry
  │                         └── TTL -> fila principal
  └── inválida/esgotada -> adminflow.budget.expense-approved.dead-letter
```

Na Fase 9.3, o consumidor passou a delegar o processamento a um componente idempotente. Antes de chamar o handler, ele abre uma transação no PostgreSQL e insere o `EventId` na tabela `processed_integration_events`. A chave primária impede que duas entregas, inclusive concorrentes, processem o mesmo evento. O commit ocorre somente depois do handler; uma falha causa rollback e permite que o retry tente novamente.

```text
ExpenseApprovedConsumer
  -> IdempotentExpenseApprovedIntegrationEventProcessor
       -> IDbContextFactory<BudgetDbContext>
       -> INSERT event_id (chave primária)
       -> IExpenseApprovedIntegrationEventHandler
       -> COMMIT
  -> Ack
```

`IDbContextFactory` é usado porque o consumidor hospedado é singleton e precisa criar um `BudgetDbContext` independente por entrega. A tabela de deduplicação é técnica e permanece na Infrastructure; nenhuma dependência de EF Core ou RabbitMQ foi adicionada ao Domain.

O corpo bruto nunca é registrado. A garantia idempotente cobre a coordenação no PostgreSQL, mas efeitos externos não transacionais ainda podem repetir após uma queda no instante crítico. Uma possível estratégia Outbox e publisher confirms continuam pendentes. O republish explícito para a DLQ seguido de `Ack` possui uma janela de perda caso o broker falhe antes de confirmar internamente a publicação.

O evento comunica um fato já confirmado; o consumidor não participa da decisão síncrona de aprovação. Ainda será necessário avaliar Outbox para evitar perda entre commit no banco e publicação. RabbitMQ não exige um microserviço publicador separado.

Redis não resolve nenhum problema atual e permanece fora do escopo.

## Riscos arquiteturais

- Condição de corrida ao aprovar duas solicitações contra o mesmo saldo.
- Inconsistência se solicitação e orçamento não forem atualizados atomicamente.
- Contratos provisórios de ator criarem falsa sensação de segurança antes da autenticação.
- Duplicação excessiva de modelos e interfaces em nome de Clean Architecture.
- Publicação futura de evento antes de a transação estar confirmada, ou perda após o commit.
- Misturar `Committed` e `Spent`, degradando a linguagem do domínio.

## Revisão de segurança do planejamento

### Importante — identidade declarada pelo cliente

Até a implementação de autenticação, `RequesterId` e `DecisionMakerId` recebidos no corpo não comprovam identidade. Esse modo é aceitável apenas para aprendizado/desenvolvimento e não deve ser apresentado como controle de acesso. Aprovação em ambiente real dependerá de identidade autenticada e autorização explícita.

### Importante — autoaprovação e escopo do aprovador

O domínio futuro deve impedir autoaprovação e verificar se o aprovador pode agir sobre aquele centro de custo. Uma role isolada não resolve autorização contextual. A aplicação deverá negar por padrão quando o vínculo necessário não puder ser comprovado.

### Importante — concorrência financeira

Validação apenas na API permite ultrapassar o orçamento sob requisições simultâneas. Aprovação deverá verificar e persistir saldo/estado atomicamente, com conflito seguro e sem atualizações parciais.

### Melhoria — contratos e exposição

Requests e responses explícitos evitarão mass assignment e exposição acidental. Campos controlados pelo servidor, detalhes internos e dados desnecessários não farão parte das entradas/respostas públicas.

### Informativo — controles adiados

Autenticação, policies, secrets, auditoria completa e hardening HTTP pertencem a fases posteriores. Validação de entradas, respostas de erro controladas e ausência de credenciais no repositório devem existir desde as primeiras implementações, sem esperar a Fase 11.

## Decisões arquiteturais pendentes

- Controllers ou Minimal APIs na Fundação ASP.NET Core.
- Contrato entre Application e persistência: acesso específico ou outra abstração mínima, decidido apenas quando o EF Core entrar.
- Estratégia de transação e concorrência na aprovação.
- Estratégia confiável de publicação de eventos quando RabbitMQ for introduzido.
- Forma de autenticação e modelo de autorização por papel/policy.
