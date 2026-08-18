# Status do Projeto AdminFlow

## Fase Atual

Phase 7 — Logging Estruturado

## Status

COMPLETE

## Implementado

- Serilog configurado como provedor de logging da API.
- Sink inicial de console com template legível e propriedades estruturadas.
- Níveis configurados por ambiente em `appsettings`.
- Um evento HTTP por requisição com método, caminho, status e duração.
- `ILogger<ExpenseApprovalService>` na Application, sem dependência direta de Serilog.
- Evento `ExpenseRequestApproved` após aprovação persistida.
- Evento `ExpenseRequestRejected` após rejeição persistida.
- Propriedades `ExpenseRequestId`, `BudgetId`, `DecisionMakerId`, `Amount`, `Action` e `OccurredAt`.
- Eventos de sucesso emitidos somente após `SaveChanges`.
- Testes que inspecionam propriedades estruturadas, não somente texto.
- Testes contra falso sucesso quando validação ou persistência falha.
- ADR-004 documentando tecnologia, limites e dados proibidos.

Não foram adicionados arquivos de log, serviço externo, auditoria persistente, RabbitMQ ou OpenTelemetry.

## Arquitetura

```text
Application
  ExpenseApprovalService
       -> ILogger<T> (abstração .NET)

API
  Serilog (implementação)
       -> Console
  SerilogRequestLogging
       -> evento HTTP resumido

Domain
  sem logging
```

O Serilog permanece na borda da aplicação. A Application conhece somente a abstração de logging e o Domain continua sem dependências externas.

## Eventos Estruturados

```text
ExpenseRequestApproved
ExpenseRequestRejected
  ExpenseRequestId
  BudgetId
  DecisionMakerId
  Amount
  Action
  OccurredAt
```

Descrição, motivo de rejeição, corpo HTTP, query string, headers, senha, token e connection string não são registrados.

## Testes

- Evento de aprovação contém propriedades estruturadas esperadas.
- Evento de rejeição não contém o motivo reservado.
- Saldo insuficiente não gera falso evento de sucesso.
- Falha de persistência não gera falso evento de sucesso.
- API continua iniciando com health check e Swagger.
- 50 testes unitários/Application aprovados.
- 15 testes de integração aprovados com PostgreSQL configurado.
- total esperado com banco: 65 aprovados, 0 falhas, 0 ignorados.

## Segurança

Escopo revisado: configuração do Serilog, templates de eventos de negócio, middleware HTTP, appsettings e dependências adicionadas.

### Achados

- Crítico: nenhum.
- Importante: nenhum novo problema introduzido.
- Melhoria adiada: definir retenção e acesso quando existir armazenamento centralizado.
- Informativo: identificadores e valores financeiros não são secrets, mas continuam sendo dados administrativos e exigem acesso controlado aos logs.

### Controles Positivos

- Nenhum secret no repositório ou nos templates.
- Nenhuma descrição ou justificativa de rejeição registrada.
- Nenhum corpo, query string ou header HTTP registrado.
- Application não depende diretamente do Serilog.
- Eventos de sucesso só ocorrem após persistência.

## Decisões Importantes

- Serilog está na API; `ILogger<T>` está na Application.
- Domain não registra logs.
- Console é o único sink nesta fase.
- Logging operacional não substitui auditoria de negócio.
- Não foi criado enriquecimento automático com dados potencialmente sensíveis.
- ADR-004 registra a decisão.

## Problemas Conhecidos

- Ainda não há endpoints HTTP de negócio para acionar o fluxo externamente.
- Logs existem apenas no console e não possuem retenção centralizada.
- Não há identificador autenticado nem autorização por papel.
- Não existe trilha durável de auditoria.
- Conflitos e erros ainda não possuem tradução HTTP padronizada.
- O health check ainda não verifica prontidão do PostgreSQL.

## Trabalho Adiado

- Auditoria persistente e política de retenção.
- Armazenamento ou agregação centralizada de logs.
- Correlation ID explícito além do contexto HTTP padrão.
- Endpoints e contratos HTTP de negócio.
- Autenticação e autorização.
- RabbitMQ, confiabilidade de mensageria, OpenTelemetry e AdminFlow.People.

## Próxima Fase

Phase 8 — Fundamentos de RabbitMQ

Introduzir um evento de integração para aprovação, um producer e um consumer simples. Antes disso, o checkpoint deverá resolver como o fluxo será acionado e como a publicação ocorrerá sem comprometer a transação já existente, sem antecipar retry, DLQ ou idempotência da Fase 9.

Não iniciar sem autorização explícita.
