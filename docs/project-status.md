# Status do Projeto AdminFlow

## Fase Atual

Phase 9 — Confiabilidade do RabbitMQ

## Status

IN PROGRESS — Etapa 9.2 concluída

## Implementado nesta etapa

- Retry limitado para falhas transitórias, sem `requeue=true` imediato.
- Fila de retry com TTL configurável; ao expirar, a mensagem retorna à fila principal.
- Contagem de tentativas pelo cabeçalho `x-death` mantido pelo RabbitMQ.
- Dead-letter queue (DLQ) para mensagens inválidas ou que esgotaram as tentativas.
- Política padrão de 3 retries com intervalo de 5 segundos.
- Handler separado da entrega para permitir simular sucesso e falha nos testes.
- Validação das opções de retry durante a inicialização.
- ADR-007 documentando topologia, política e trade-offs.

## Fluxo Atual

```text
fila principal
  ├── processada com sucesso -----------> Ack
  ├── JSON/contrato inválido -----------> DLQ
  └── falha transitória
        ├── tentativas disponíveis ------> fila de retry --TTL--> fila principal
        └── tentativas esgotadas --------> DLQ
```

## Testes

- mensagem válida é processada e confirmada;
- mensagem inválida é encaminhada à DLQ;
- falha transitória é repetida e concluída com sucesso;
- esgotamento do limite encaminha a mensagem à DLQ;
- contagem de `x-death` considera somente mortes na fila principal;
- 53 testes unitários/Application;
- 29 testes de integração/técnicos com PostgreSQL e RabbitMQ reais;
- total: 82 aprovados, 0 falhas, 0 ignorados.

## Segurança

- Corpo das mensagens não é registrado nos logs.
- Credenciais continuam fora do repositório.
- Mensagens são validadas antes do processamento.
- Nenhum pacote novo foi adicionado nesta etapa.
- A DLQ contém dados financeiros do evento e deve ter acesso operacional restrito.

## Decisões Importantes

- A fila principal usa dead-letter exchange para encaminhar falhas à fila de retry.
- A fila de retry usa TTL e devolve as mensagens à exchange principal.
- O número de mortes na fila principal, registrado em `x-death`, define o limite.
- Mensagens inválidas não consomem retries e seguem diretamente para a DLQ.
- O padrão inicial é intervalo fixo; backoff progressivo foi adiado.

## Problemas Conhecidos

- O republish explícito para a DLQ ainda não usa publisher confirms; uma falha do broker entre publicação e `Ack` pode causar perda.
- Filas duráveis criadas pela topologia anterior precisam ser recriadas ao adotar os novos argumentos de dead-letter; o Compose local não persiste dados do RabbitMQ, mas uma implantação real exigirá migração operacional.
- Não há idempotência; uma mensagem pode produzir o mesmo efeito mais de uma vez.
- Existe janela de falha entre commit do PostgreSQL e publicação.
- Producer ainda abre conexão/canal por publicação.
- Ainda não há endpoint HTTP de negócio.

## Trabalho Adiado

- Idempotência do consumidor.
- Avaliação de Outbox e publisher confirms.
- Backoff progressivo e política operacional de retenção/alerta da DLQ.
- Reconexão e reutilização de conexão/canal.
- Endpoints, autenticação, autorização e OpenTelemetry.

## Próxima Etapa

Phase 9.3 — Idempotência do consumidor

Impedir que uma nova entrega do mesmo evento repita seu efeito de negócio. Não iniciar sem autorização explícita.
