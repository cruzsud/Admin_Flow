# Status do Projeto AdminFlow

## Fase Atual

Fase 9 — Confiabilidade do RabbitMQ

## Status

CONCLUÍDA — Etapas 9.1, 9.2 e 9.3 concluídas

## Implementado

- Acknowledgement manual depois do processamento.
- Retry limitado com fila de espera, TTL e contagem pelo cabeçalho `x-death`.
- Dead-letter queue para mensagens inválidas ou com tentativas esgotadas.
- Processador idempotente baseado no `EventId` do evento.
- Tabela técnica `processed_integration_events`, com chave primária em `event_id`.
- Transação que desfaz a marcação quando o handler falha.
- Proteção contra duplicatas sequenciais e concorrentes pelo PostgreSQL.
- Migration `AddProcessedIntegrationEvents`.
- ADRs 006, 007 e 008 documentando as decisões de confiabilidade.

## Arquitetura Atual

```text
RabbitMQ
  -> ExpenseApprovedConsumer
  -> valida contrato
  -> IdempotentExpenseApprovedIntegrationEventProcessor
       -> inicia transação PostgreSQL
       -> insere EventId único
       -> executa handler
       -> commit
  -> Ack
```

Uma duplicata encontra a chave já persistida, não executa o handler e recebe `Ack`. Se o handler falhar, a transação é revertida e a política de retry continua aplicável.

## Testes

- primeira entrega é processada e registrada;
- duplicata sequencial não chama o handler novamente;
- duplicatas concorrentes resultam em uma única chamada;
- falha do handler reverte a marcação e permite retry;
- regressões de acknowledgement, retry e DLQ continuam protegidas;
- 53 testes unitários/Application;
- 32 testes de integração/técnicos com PostgreSQL e RabbitMQ reais;
- total: 85 aprovados, 0 falhas, 0 ignorados.

## Segurança

- A tabela técnica armazena somente `EventId`, tipo e instante do processamento.
- Corpo das mensagens e credenciais não são registrados.
- Consultas e inserções continuam parametrizadas pelo EF Core.
- Nenhum pacote novo foi adicionado nesta etapa.

## Decisões Importantes

- A idempotência pertence à Infrastructure e não altera o Domain financeiro.
- PostgreSQL foi reutilizado em vez de adicionar Redis.
- A chave primária fornece exclusão concorrente, evitando o padrão inseguro de apenas consultar antes de inserir.
- A marcação e o handler ficam dentro de uma transação para permitir rollback em falhas.
- `AddDbContextFactory` fornece contextos independentes ao consumidor hospedado e mantém o `BudgetDbContext` disponível por escopo para os casos de uso atuais.

## Problemas Conhecidos

- A garantia cobre a marcação no PostgreSQL, mas não torna atômicos efeitos externos como HTTP, arquivo ou log; esses efeitos ainda podem repetir após uma queda no instante crítico.
- A tabela de eventos processados ainda não possui política de retenção.
- O republish explícito para a DLQ ainda não usa publisher confirms.
- Existe janela de falha entre o commit da aprovação e a publicação do evento; Outbox ainda não foi implementado.
- Producer abre conexão/canal por publicação.
- Ainda não há endpoint HTTP de negócio.

## Trabalho Adiado

- Política de retenção da inbox técnica.
- Avaliação de Outbox e publisher confirms.
- Backoff progressivo e operação/monitoramento da DLQ.
- Reconexão e reutilização de conexão/canal.
- Endpoints, autenticação e autorização.

## Próxima Fase

Fase 10 — Observabilidade com OpenTelemetry

Introduzir traces e métricas somente para os componentes já existentes, começando pelo problema concreto de correlacionar execução da API, PostgreSQL e RabbitMQ. Não iniciar sem autorização explícita.
