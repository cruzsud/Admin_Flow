# Status do Projeto AdminFlow

## Fase Atual

Phase 8 — Fundamentos de RabbitMQ

## Status

COMPLETE

## Implementado

- Contrato `ExpenseApprovedIntegrationEvent` na Application.
- Abstração `IExpenseApprovedPublisher` independente do RabbitMQ.
- Publicação somente após aprovação persistida.
- Rejeições não publicam evento de aprovação.
- Producer com RabbitMQ.Client 7.2.2.
- Exchange direto durável `adminflow.budget`.
- Routing key `expense.approved`.
- Fila durável `adminflow.budget.expense-approved`.
- Mensagem JSON persistente com `EventId` como `MessageId`.
- Consumer simples como `BackgroundService`.
- Consumer desserializa e registra recebimento estruturado.
- RabbitMQ 4.1 Management no Docker Compose.
- Integração habilitável por configuração.
- Credenciais exclusivamente por ambiente.
- Teste real de publicação e desserialização.
- ADR-005 documentando escolhas e limitações.

## Arquitetura

```text
Application
  ExpenseApprovalService
    -> IExpenseApprovedPublisher
    -> ExpenseApprovedIntegrationEvent

Infrastructure
  RabbitMqExpenseApprovedPublisher
  ExpenseApprovedConsumer
    -> RabbitMQ

Domain
  sem dependência de mensageria
```

## Evento

`ExpenseApprovedIntegrationEvent` contém:

- `EventId`;
- `ExpenseRequestId`;
- `BudgetId`;
- `DecisionMakerId`;
- `Amount`;
- `Currency = BRL`;
- `ApprovedAt`.

Descrição e motivo de rejeição não são publicados.

## Testes

- aprovação persistida publica exatamente um evento;
- falha de persistência não publica;
- rejeição não publica;
- contrato contém os valores esperados;
- producer entrega JSON válido em RabbitMQ real;
- 53 testes unitários/Application aprovados;
- 16 testes de integração esperados com PostgreSQL e RabbitMQ;
- total esperado: 69 aprovados, 0 falhas, 0 ignorados.

## Segurança

- Nenhuma credencial RabbitMQ foi versionada.
- Senha é recebida por `RabbitMq:Password`/variável de ambiente.
- Configuração habilitada exige usuário e senha não vazios.
- Evento não contém secrets, descrição ou motivo de rejeição.
- Pacote RabbitMQ.Client foi consultado no catálogo de vulnerabilidades do NuGet sem alerta reportado.

## Decisões Importantes

- O evento é de integração, não entidade nem mensagem criada pelo Domain.
- Publicação ocorre depois do commit do PostgreSQL.
- RabbitMQ pode permanecer desabilitado para execução sem broker.
- Producer e consumer ficam na Infrastructure.
- Consumidor inicial apenas registra o fato recebido.
- ADR-005 registra a fundação e os riscos aceitos.

## Problemas Conhecidos

- Existe janela de falha entre commit do PostgreSQL e publicação.
- Consumer usa `autoAck=true`.
- Não há retry, DLQ, idempotência ou publisher confirms.
- Producer abre conexão/canal por publicação; otimização foi adiada.
- Ainda não há endpoint HTTP de negócio para acionar o fluxo.
- RabbitMQ desabilitado usa publisher sem operação para manter desenvolvimento local simples.

## Trabalho Adiado

- Acknowledgement manual, retry, dead-letter queue e idempotência.
- Avaliação de Outbox transacional.
- Reconexão e reutilização eficiente de conexões/canais.
- Endpoints HTTP, autenticação e autorização.
- OpenTelemetry e AdminFlow.People.

## Próxima Fase

Phase 9 — Confiabilidade do RabbitMQ

Introduzir progressivamente acknowledgement, tratamento de falhas, retry, dead-letter queue e idempotência, avaliando também a lacuna entre banco e publicação. Não implementar tudo sem novo checkpoint e delimitação didática.

Não iniciar sem autorização explícita.
