# ADR-009: Observabilidade com OpenTelemetry

## Contexto

Logs estruturados descrevem eventos isolados, mas não mostram automaticamente a relação temporal entre uma requisição HTTP, operações PostgreSQL e mensagens RabbitMQ. Também faltavam medidas agregadas de duração, runtime, banco e resultados da mensageria.

## Decisão

Configurar OpenTelemetry na API como composition root de traces e métricas.

Instrumentações automáticas cobrem ASP.NET Core, `HttpClient`, runtime .NET e Npgsql. A Infrastructure cria spans e métricas de RabbitMQ com `ActivitySource` e `Meter`, sem depender do SDK. O contexto W3C `traceparent`/`tracestate` é propagado nos headers para ligar publicação e consumo.

O console pode ser habilitado para aprendizado local. OTLP pode ser habilitado para um Collector ou backend externo. Ambos ficam desabilitados por padrão.

## Alternativas

- Somente logs: menor complexidade, mas sem árvore distribuída ou métricas padronizadas.
- Instrumentação automática por agent: menos código, porém oculta os conceitos desta fase e não define os resultados de negócio da mensageria.
- Adicionar Jaeger, Prometheus e Grafana: permitiria visualização imediata, mas acrescentaria vários componentes operacionais antes de uma necessidade concreta.
- Exportar diretamente para um fornecedor: cria acoplamento prematuro; OTLP mantém o backend substituível.

## Consequências

- Uma operação pode ser correlacionada entre HTTP, PostgreSQL e RabbitMQ.
- Métricas técnicas e de resultado da mensageria ficam disponíveis para exportação.
- A API recebe dependências do SDK e dos instrumentadores; Domain e Application permanecem independentes.
- Sampling, custo, retenção e proteção de dados tornam-se preocupações operacionais explícitas.
- Console não é apropriado para produção e não fornece consulta histórica.
- Um backend/Collector deverá ser escolhido somente quando houver requisito de operação e visualização.
