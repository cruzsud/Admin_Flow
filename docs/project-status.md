# Status do Projeto AdminFlow

## Fase Atual

Phase 4 — Budget

## Status

COMPLETE

## Implementado

- Entidade de domínio `Budget`.
- Identidade `Guid` e associação por `CostCenterId`.
- Exercício fiscal entre 1 e 9999.
- `Allocated` positivo, em `decimal`, com no máximo duas casas decimais.
- `Committed` iniciado em zero.
- `Available = Allocated - Committed` calculado pelo domínio.
- BRL como única moeda implícita do MVP.
- `DbSet<Budget>` no `BudgetDbContext`.
- Mapeamento para a tabela `budgets`.
- Valores financeiros como `numeric(18,2)`.
- Chave estrangeira restritiva para `cost_centers`.
- Índice único composto por centro de custo e exercício.
- Checks de exercício, alocação e compromisso.
- Migration `AddBudgets`.
- Testes unitários e testes reais de persistência.

Não foram criados caso de uso, endpoint ou operação de comprometer orçamento.

## Arquitetura

```text
Domain
  Budget -> CostCenterId

Infrastructure
  BudgetDbContext
    -> CostCenters
    -> Budgets
  BudgetConfiguration
    -> PostgreSQL
```

Domain continua sem EF Core. `Available` não é armazenado porque é derivado de `Allocated` e `Committed`; persistir o valor calculado criaria risco de inconsistência.

Não foram criados repository, serviço genérico, CQRS ou MediatR.

## Domínio Atual

```text
CostCenter
  └── Budget por exercício
        Allocated
        Committed = 0
        Available = Allocated - Committed
```

`Budget` nasce válido e sem transições de estado. A futura aprovação será responsável por comprometer saldo; essa operação não foi antecipada.

## Testes

O ciclo Red/Green/Refactor foi aplicado:

1. Red: testes falharam porque `Budget` não existia.
2. Green: a entidade mínima satisfez invariantes e cálculo.
3. Refactor: persistência e testes foram organizados sem novas abstrações.

Validação contra PostgreSQL 17 real em container:

- migrations `InitialCreate` e `AddBudgets` aplicadas;
- orçamento persistido e reconstruído corretamente;
- mesmo exercício permitido para centros diferentes;
- duplicidade de centro/exercício rejeitada;
- centro inexistente rejeitado pela FK;
- 16 testes unitários aprovados;
- 9 testes de integração aprovados;
- total: 25 aprovados, 0 falhas, 0 ignorados;
- build: 0 erros e 0 avisos;
- modelo EF sem mudanças pendentes.

Sem `ADMINFLOW_TEST_DB_CONNECTION_STRING`, os 6 testes PostgreSQL são ignorados explicitamente e os 3 testes técnicos continuam executando.

## Segurança

- Nenhum secret novo foi criado.
- Testes usam senha efêmera em memória.
- Operações normais continuam parametrizadas por EF Core/Npgsql.
- A FK e as constraints protegem integridade mesmo fora da aplicação.

## Decisões Importantes

- O modelo orçamentário documentado na Fase 0 foi materializado sem alterações semânticas.
- `decimal` foi suficiente; um value object `Money` não se justifica com moeda única.
- `Available` é calculado, não persistido.
- Não existe método `Commit` ainda, pois compromisso acontece no fluxo de aprovação.
- Um orçamento é único por `(CostCenterId, FiscalYear)`.
- Exclusão de centro com orçamento é restrita pelo PostgreSQL.
- Nenhum ADR novo foi necessário; a arquitetura de persistência já está registrada no ADR-002.

## Problemas Conhecidos

- Ainda não há caso de uso ou endpoint para criar/consultar centros e orçamentos.
- `Committed` permanece sempre zero até o fluxo de aprovação.
- Suplementação, redução, encerramento e cancelamento não existem.
- O health check ainda não verifica prontidão do PostgreSQL.
- Formato e normalização de código de centro continuam pendentes.

## Trabalho Adiado

- Casos de uso e endpoints de `CostCenter` e `Budget`.
- `ExpenseRequest` e fluxo de aprovação.
- Concorrência para compromisso orçamentário.
- FluentValidation, autenticação, autorização e auditoria.
- Serilog, RabbitMQ, OpenTelemetry e AdminFlow.People.
- Redis, que não possui requisito atual.

## Próxima Fase

Phase 5 — ExpenseRequest

Implementar criação pendente, consulta conceitual, validação e persistência de `ExpenseRequest`, referenciando um orçamento existente. Aprovação, rejeição e alteração de saldo permanecem para a Fase 6.

Não iniciar sem autorização explícita.
