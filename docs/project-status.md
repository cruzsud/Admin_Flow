# Status do Projeto AdminFlow

## Fase Atual

Phase 5 — ExpenseRequest

## Status

COMPLETE

## Implementado

- Entidade de domínio `ExpenseRequest` com identidade própria.
- Associação obrigatória por `BudgetId`.
- Identificação provisória do solicitante por `RequesterId`.
- Descrição obrigatória, sem espaços nas extremidades.
- Valor positivo em `decimal`, BRL implícito e no máximo duas casas decimais.
- Estado inicial `Pending` protegido pela entidade.
- Enum com a linguagem do workflow: `Pending`, `Approved` e `Rejected`.
- `DbSet<ExpenseRequest>` no `BudgetDbContext` para inclusão e consulta.
- Mapeamento para a tabela `expense_requests`.
- Valor financeiro como `numeric(18,2)`.
- Chave estrangeira restritiva para `budgets`.
- Índice para consultas por `budget_id`.
- Checks de valor positivo e status válido.
- Migration `AddExpenseRequests`.
- Testes unitários e testes reais de persistência e consulta.

Não foram criados casos de uso, endpoints, aprovação, rejeição ou alteração do orçamento.

## Arquitetura

```text
Domain
  CostCenter
    <- Budget
         <- ExpenseRequest (Pending)

Infrastructure
  BudgetDbContext
    -> CostCenters
    -> Budgets
    -> ExpenseRequests
  ExpenseRequestConfiguration
    -> PostgreSQL
```

O Domain continua independente do EF Core. A Infrastructure adapta a entidade ao PostgreSQL. O `BudgetDbContext` é suficiente nesta fase; não foram adicionados repository genérico, CQRS, MediatR ou serviço genérico.

## Domínio Atual

```text
CostCenter
  └── Budget por exercício
        ├── Allocated
        ├── Committed = 0
        ├── Available = Allocated - Committed
        └── ExpenseRequest
              Amount
              RequesterId
              Description
              Status = Pending
```

Criar uma solicitação pendente não altera `Committed` nem `Available`. Os estados terminais foram nomeados para formar a linguagem do domínio, mas nenhuma transição para eles existe ainda.

## Testes

O ciclo Red/Green/Refactor foi aplicado:

1. Red: os testes falharam porque `ExpenseRequest` ainda não existia.
2. Green: a entidade mínima satisfez criação, normalização e invariantes.
3. Refactor: mapeamento e consultas foram adicionados sem abstrações extras.

Validação contra PostgreSQL 17 real em container:

- migrations `InitialCreate`, `AddBudgets` e `AddExpenseRequests` aplicadas;
- solicitação persistida e reconstruída corretamente;
- orçamento inexistente rejeitado pela chave estrangeira;
- consulta por orçamento retorna apenas suas solicitações;
- 25 testes unitários aprovados;
- 12 testes de integração aprovados;
- total: 37 aprovados, 0 falhas, 0 ignorados nos testes com banco;
- build: 0 erros e 0 avisos.

Sem `ADMINFLOW_TEST_DB_CONNECTION_STRING`, os testes PostgreSQL são ignorados explicitamente e os testes técnicos continuam executando.

## Segurança

- Nenhum secret foi criado ou armazenado no repositório.
- A senha usada nos testes foi efêmera e limitada ao processo local.
- `RequesterId` ainda é informado pelo chamador e não representa identidade autenticada.
- EF Core/Npgsql parametrizam as operações normais.
- Chave estrangeira e constraints reforçam integridade no banco.

## Decisões Importantes

- Uma solicitação nasce diretamente `Pending`; não existem `Draft` nem submissão separada no MVP.
- Criação pendente não reserva saldo.
- `RequesterId` foi incluído agora para sustentar futuramente a regra de não autoaprovação.
- Descrição usa `text`; nenhum limite arbitrário foi criado sem requisito.
- O enum contém os três estados planejados, mas as transições ficam na Fase 6.
- Não foi necessário novo ADR; a separação Domain/Infrastructure e o uso de EF Core já estão registrados.

## Problemas Conhecidos

- Ainda não há casos de uso ou endpoints HTTP para criar e consultar entidades.
- `RequesterId` não é autenticado.
- `Committed` permanece zero até o fluxo de aprovação.
- Ainda não há registro de decisor, data da decisão ou motivo de rejeição.
- O health check ainda não verifica prontidão do PostgreSQL.
- Formato e normalização do código de centro continuam pendentes.

## Trabalho Adiado

- Aprovação e rejeição de `ExpenseRequest`.
- Compromisso atômico do orçamento e controle de concorrência.
- Casos de uso e endpoints de `CostCenter`, `Budget` e `ExpenseRequest`.
- FluentValidation, autenticação, autorização e auditoria.
- Serilog, RabbitMQ, OpenTelemetry e AdminFlow.People.
- Redis, que não possui requisito atual.

## Próxima Fase

Phase 6 — Fluxo de Aprovação

Implementar transições de aprovação e rejeição, impedir decisões repetidas, exigir saldo suficiente, comprometer o orçamento de forma atômica e tratar concorrência. Casos de uso e endpoints devem ser delimitados no checkpoint da fase.

Não iniciar sem autorização explícita.
