# ADR-005: Fundamentos de integração com RabbitMQ

## Contexto

Uma aprovação confirmada precisa futuramente informar outros componentes sem bloquear o processo principal. O projeto ainda não possuía um contrato de integração, produtor, fila ou consumidor.

## Decisão

Publicar `ExpenseApprovedIntegrationEvent` após o commit do PostgreSQL por meio de `IExpenseApprovedPublisher`. Usar um exchange direto durável, routing key `expense.approved` e uma fila durável. Um `BackgroundService` consome a mensagem com confirmação automática e registra seu recebimento.

RabbitMQ é opcional por configuração. Credenciais são recebidas por variáveis de ambiente e não são versionadas.

## Alternativas

- Chamada HTTP: inadequada para uma notificação que não precisa de resposta imediata.
- Publicação antes do banco: poderia anunciar uma aprovação que depois falhasse.
- Outbox transacional: reduz a janela entre banco e broker, mas acrescenta tabela, dispatcher e recuperação; será avaliado na fase de confiabilidade.
- Domain event direto no RabbitMQ: acoplaria o Domain a uma tecnologia externa.

## Consequências

- Produtor e consumidor simples tornam o fluxo assíncrono demonstrável.
- Domain e Application não dependem do cliente RabbitMQ.
- Se a publicação falhar depois do commit, a aprovação já estará persistida e o evento poderá ser perdido.
- `autoAck=true` pode perder mensagem se o consumidor falhar durante o processamento.
- Retry, DLQ, idempotência, confirmação manual e Outbox permanecem pendentes para a Fase 9.
