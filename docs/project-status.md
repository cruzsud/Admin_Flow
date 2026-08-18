# Status do Projeto AdminFlow

## Fase Atual

Fase 10 — Observabilidade com OpenTelemetry

## Status

CONCLUÍDA

## Implementado

- OpenTelemetry SDK configurado na API para traces e métricas.
- Resource com nome, versão e ambiente do serviço.
- Traces automáticos de ASP.NET Core, `HttpClient` e Npgsql.
- Métricas de ASP.NET Core, `HttpClient`, runtime .NET e Npgsql.
- Spans manuais para publicação e consumo de `ExpenseApproved` no RabbitMQ.
- Propagação W3C de `traceparent` e `tracestate` nos headers da mensagem.
- Contador `adminflow.budget.rabbitmq.messages` por operação e resultado.
- Sampling configurável, usando decisão do trace pai.
- Exportadores Console e OTLP opcionais e desabilitados por padrão.
- `NpgsqlDataSource` nomeado para impedir que a connection string identifique o pool nas métricas.
- ADR-009 documentando o escopo e os trade-offs.

## Arquitetura Atual

```text
ASP.NET Core ─┐
HttpClient ───┤
Npgsql ───────┼→ OpenTelemetry SDK → Console opcional
RabbitMQ ─────┘                    → OTLP opcional
```

```text
trace HTTP
  -> span PostgreSQL
  -> span RabbitMQ publish
       -> traceparent na mensagem
       -> span RabbitMQ consume
            -> span PostgreSQL da idempotência
```

Domain e Application permanecem sem dependência do SDK. A instrumentação manual na Infrastructure utiliza as APIs nativas `ActivitySource` e `Meter`; a API registra listeners e exportadores.

## Testes

- produtor e consumidor compartilham o mesmo TraceId após propagação;
- span consumidor referencia o span produtor como pai;
- métrica RabbitMQ contém somente operação e resultado controlados;
- baggage arbitrário não é propagado na mensagem;
- providers de trace e métricas são registrados no host;
- regressões completas com PostgreSQL e RabbitMQ reais;
- 53 testes unitários/Application;
- 35 testes de integração/técnicos;
- total: 88 aprovados, 0 falhas, 0 ignorados.

## Segurança

- Corpo de mensagens, parâmetros SQL, senhas e connection strings não são adicionados manualmente à telemetria.
- Baggage não é propagado para evitar transportar dados arbitrários.
- Nome fixo do pool Npgsql evita expor a connection string nas métricas.
- Endpoint OTLP remoto exige HTTPS; HTTP é permitido somente para loopback local.
- Credenciais/headers OTLP devem ser fornecidos por variáveis de ambiente.

## Decisões Importantes

- Console é apenas didático; OTLP é o protocolo preparado para integração com backends.
- Nenhum Collector, Jaeger, Prometheus ou Grafana foi adicionado nesta fase.
- Logs continuam no Serilog; não foram duplicados pelo pipeline OpenTelemetry.
- Tags de métricas usam valores de baixa cardinalidade.
- Sampling padrão é 100% para aprendizado e deve ser reduzido conforme volume e política do ambiente.

## Problemas Conhecidos

- Não há backend visual ou retenção de telemetria configurados.
- Tracing do Npgsql segue convenções que ainda podem evoluir entre versões.
- Sampling de 100% pode ser caro em produção.
- O republish para DLQ ainda não usa publisher confirms.
- Existe janela entre commit da aprovação e publicação; Outbox não foi implementado.
- Ainda não há endpoints HTTP de negócio.

## Trabalho Adiado

- Escolha e configuração de backend/Collector de observabilidade.
- Dashboards, alertas e política de retenção.
- Outbox, publisher confirms e retenção da inbox técnica.
- Endpoints HTTP de negócio.

## Próxima Fase

Fase 11 — Segurança

Introduzir autenticação e autorização progressivamente, associadas a casos de uso concretos. Não iniciar sem autorização explícita.
