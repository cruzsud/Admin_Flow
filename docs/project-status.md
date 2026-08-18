# Status do Projeto AdminFlow

## Fase Atual

Phase 9 — Confiabilidade do RabbitMQ

## Status

IN PROGRESS — Etapa 9.1 concluída

## Implementado nesta etapa

- Consumidor alterado de `autoAck=true` para `autoAck=false`.
- `BasicAck` somente depois de desserialização, validação e processamento.
- `BasicNack` sem requeue para JSON ou contrato inválido.
- `BasicNack` com requeue para falha inesperada potencialmente transitória.
- Validação técnica de identificadores, valor positivo, moeda BRL e instante.
- Logs de falha limitados ao `MessageId`; corpo bruto não é registrado.
- ADR-006 documentando acknowledgement manual e trade-offs.
- README e arquitetura atualizados.

## Fluxo Atual

```text
RabbitMQ entrega mensagem sem confirmação automática
  ├── evento válido e processado -> Ack -> remove da fila
  ├── JSON/contrato inválido ----> Nack sem requeue -> descarta
  └── falha inesperada ----------> Nack com requeue -> nova entrega
```

## Testes

- contrato completo aceito;
- identificador vazio rejeitado;
- valor zero ou negativo rejeitado;
- moeda diferente de BRL rejeitada;
- instante vazio rejeitado;
- evento válido consumido e removido da fila após confirmação;
- JSON inválido rejeitado sem retornar à fila;
- 53 testes unitários/Application existentes;
- 24 testes de integração/técnicos esperados com infraestrutura completa;
- total esperado: 77 aprovados, 0 falhas, 0 ignorados.

## Segurança

- Corpo de mensagem inválida não é registrado.
- Credenciais continuam fora do repositório.
- Mensagens são validadas antes do processamento.
- Nenhum pacote novo foi adicionado nesta etapa.

## Decisões Importantes

- A Fase 9 foi dividida para introduzir um conceito de confiabilidade por vez.
- Mensagem inválida não retorna à fila para evitar loop infinito.
- Falha inesperada retorna temporariamente à fila, ainda sem limite ou atraso.
- ADR-006 registra a política atual de Ack/Nack.

## Problemas Conhecidos

- `requeue=true` pode causar repetição rápida e ilimitada.
- Mensagem inválida é descartada porque a DLQ ainda não existe.
- Não há retry com atraso, limite de tentativas ou backoff.
- Não há idempotência.
- Existe janela de falha entre commit do PostgreSQL e publicação.
- Producer ainda abre conexão/canal por publicação.
- Ainda não há endpoint HTTP de negócio.

## Trabalho Adiado

- Retry limitado com atraso.
- Dead-letter queue.
- Idempotência do consumidor.
- Avaliação de Outbox e publisher confirms.
- Reconexão e reutilização de conexão/canal.
- Endpoints, autenticação, autorização e OpenTelemetry.

## Próxima Etapa

Phase 9.2 — Retry limitado e Dead-Letter Queue

Substituir o requeue imediato por uma política explícita com atraso, limite de tentativas e encaminhamento final para DLQ. Não iniciar sem autorização explícita.
