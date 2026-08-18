# Status do Projeto AdminFlow

## Fase Atual

Phase 6 — Fluxo de Aprovação

## Status

COMPLETE

## Implementado

- `Budget.Commit(amount)` com validação de valor e saldo disponível.
- Aprovação de solicitação pendente por `ExpenseRequest.Approve`.
- Rejeição de solicitação pendente por `ExpenseRequest.Reject`.
- Estados terminais sem nova decisão.
- Bloqueio de autoaprovação.
- Registro de `DecisionMakerId`, `DecidedAt` e `RejectionReason`.
- Rejeição com motivo obrigatório e normalizado.
- `ExpenseApprovalService` na Application para orquestrar decisões.
- `TimeProvider` para obter tempo de forma testável.
- Contrato específico `IExpenseApprovalStore`.
- `BudgetDbContext` como implementação direta do contrato.
- Gravação atômica de solicitação e orçamento pelo `SaveChanges` do EF Core.
- Concorrência otimista de `Budget` pela coluna interna `xmin` do PostgreSQL.
- Constraint de coerência entre status e dados de decisão.
- Migration `AddExpenseApproval`.
- ADR-003 para a estratégia de concorrência.

Não foram criados endpoints HTTP, autenticação, logging ou eventos.

## Arquitetura

```text
Application
  ExpenseApprovalService
          |
          v
  IExpenseApprovalStore
          |
          v
Infrastructure
  BudgetDbContext -> EF Core -> PostgreSQL
          |
          v
Domain
  ExpenseRequest.Approve/Reject
  Budget.Commit
```

Application coordena e Domain decide as regras. Infrastructure carrega e persiste. O `BudgetDbContext` já oferece comportamento de Repository e Unit of Work, portanto nenhum wrapper genérico foi criado.

## Domínio Atual

```text
ExpenseRequest: Pending
  ├── Approve
  |     ├── decisor válido e diferente do solicitante
  |     ├── saldo suficiente
  |     ├── Budget.Commit(Amount)
  |     └── Approved
  └── Reject
        ├── decisor e motivo válidos
        ├── orçamento inalterado
        └── Rejected
```

`Approved` e `Rejected` são estados terminais. `Available = Allocated - Committed` continua calculado, e apenas aprovação aumenta `Committed`.

## Testes

O ciclo Red/Green/Refactor foi aplicado ao Domain e à Application.

Comportamentos protegidos:

- aprovação com saldo maior ou exatamente igual;
- rejeição por saldo insuficiente sem modificar entidades;
- autoaprovação proibida;
- decisão repetida ou troca de estado terminal proibida;
- decisor, instante e motivo validados;
- rejeição sem compromisso orçamentário;
- gravação conjunta de orçamento e solicitação;
- conflito concorrente detectado e segunda aprovação revertida.

Validação contra PostgreSQL 17 real:

- 46 testes unitários/Application aprovados;
- 15 testes de integração aprovados;
- total: 61 aprovados, 0 falhas, 0 ignorados com banco configurado;
- build: 0 erros e 0 avisos;
- modelo EF Core sem mudanças pendentes.

Sem `ADMINFLOW_TEST_DB_CONNECTION_STRING`, os 12 testes PostgreSQL são ignorados explicitamente e os 3 testes técnicos continuam executando.

## Segurança

- Autoaprovação é impedida no Domain.
- `RequesterId` e `DecisionMakerId` ainda são identificadores informados, não identidades autenticadas.
- Nenhum secret foi adicionado ao repositório.
- Constraints protegem dados mesmo contra gravações externas à aplicação.
- Erros de concorrência ainda não são expostos por HTTP porque não existem endpoints.

## Decisões Importantes

- A solicitação controla sua transição e o orçamento controla seu saldo.
- Application coordena os dois conceitos e faz uma única gravação.
- `IExpenseApprovalStore` é específico do caso de uso; não foi criado repository genérico.
- `BudgetDbContext` implementa o contrato diretamente.
- `xmin` é propriedade sombra do EF Core para não contaminar o Domain.
- O conflito concorrente não é repetido automaticamente; a futura borda HTTP decidirá como comunicá-lo.
- ADR-003 documenta a estratégia de concorrência otimista.

## Problemas Conhecidos

- Ainda não há endpoints HTTP para operar o fluxo.
- Não existe autenticação nem autorização por papel.
- Identificadores de atores ainda são fornecidos pelo chamador.
- Não há trilha completa de auditoria ou histórico de múltiplas decisões.
- O health check ainda não verifica prontidão do PostgreSQL.
- Erros ainda não possuem tradução padronizada para contratos HTTP.

## Trabalho Adiado

- Endpoints e contratos HTTP de criação, consulta e decisão.
- Autenticação, autorização e políticas por papel.
- Auditoria completa.
- Tratamento HTTP de concorrência.
- Serilog, RabbitMQ, OpenTelemetry e AdminFlow.People.
- Redis, que não possui requisito atual.

## Próxima Fase

Phase 7 — Logging Estruturado

Introduzir Serilog para registrar operações relevantes com propriedades estruturadas, sem secrets. O checkpoint da fase deverá delimitar como o fluxo atual será acionado e observado, considerando que ainda não há endpoints de negócio.

Não iniciar sem autorização explícita.
