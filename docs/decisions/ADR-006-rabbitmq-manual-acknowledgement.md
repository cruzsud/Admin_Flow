# ADR-006: Acknowledgement manual no consumidor RabbitMQ

> A política transitória de requeue e descarte descrita aqui foi substituída pelo retry limitado e pela DLQ do ADR-007. O acknowledgement manual permanece vigente.

## Contexto

Com `autoAck=true`, o RabbitMQ considerava a mensagem concluída no momento da entrega. Uma interrupção durante desserialização ou processamento poderia causar perda sem possibilidade de nova entrega.

## Decisão

Configurar o consumidor com `autoAck=false` e confirmar cada entrega explicitamente:

- evento válido e processado: `BasicAck`;
- JSON ou contrato inválido: `BasicNack` com `requeue=false`;
- falha inesperada: `BasicNack` com `requeue=true`.

Validar identificadores, valor, moeda e instante antes do processamento. Logs de erro usam apenas `MessageId` e metadados conhecidos, nunca o corpo bruto.

## Alternativas

- Manter confirmação automática: código menor, mas permite perda durante processamento.
- Sempre devolver falhas à fila: evita descarte imediato, porém mensagens inválidas causariam loop infinito.
- Implementar retry e DLQ ao mesmo tempo: mais seguro no final, mas mistura vários conceitos e dificulta o aprendizado incremental.

## Consequências

- Mensagens válidas só são removidas após processamento concluído.
- Mensagens malformadas não entram em loop infinito, mas são descartadas enquanto a DLQ não existe.
- Falhas inesperadas podem gerar repetição rápida e ilimitada com `requeue=true`.
- Retry limitado e DLQ tornam-se o próximo incremento obrigatório da Fase 9.
