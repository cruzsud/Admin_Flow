# ADR-007: Retry limitado e Dead-Letter Queue no RabbitMQ

## Contexto

O `requeue=true` da etapa anterior devolvia uma falha inesperada imediatamente à mesma fila. Uma falha persistente podia gerar um loop rápido e ilimitado, enquanto mensagens inválidas eram descartadas sem local para inspeção.

## Decisão

Usar três filas duráveis ligadas por exchanges diretas:

```text
fila principal --falha--> fila de retry --TTL--> fila principal
      └── inválida ou limite esgotado ----------> DLQ
```

A fila principal encaminha `BasicNack(requeue=false)` para a exchange de retry. A fila de retry mantém a mensagem pelo TTL configurado e depois a devolve à exchange principal. O consumidor lê o `x-death` gerado pelo RabbitMQ para contar mortes na fila principal. O padrão é 3 retries, com 5 segundos entre entregas.

JSON ou contrato inválido segue diretamente para a DLQ, sem retry. Uma falha de processamento recebe retry enquanto estiver abaixo do limite; depois, a mensagem original é publicada na exchange de dead-letter e confirmada na fila principal.

## Alternativas

- `requeue=true`: implementação menor, mas sem atraso ou limite.
- Retry dentro do processo com `Task.Delay`: ocupa o consumidor e perde o estado se a aplicação reiniciar.
- Várias filas para backoff progressivo: permite intervalos crescentes, porém acrescenta topologia antes de haver necessidade concreta.
- Plugin de delayed messages: reduz filas, mas adiciona dependência operacional ao broker.

## Consequências

- Falhas transitórias não geram loop imediato e possuem limite explícito.
- Mensagens problemáticas ficam disponíveis na DLQ para diagnóstico e tratamento operacional.
- A política adiciona duas exchanges, duas filas e configurações de retry.
- Argumentos de dead-letter de uma fila existente não podem ser alterados por uma nova declaração; brokers que preservem a topologia anterior exigem uma migração operacional da fila.
- A DLQ deve ter retenção, monitoramento e acesso restrito definidos antes de produção.
- O republish final ainda não usa publisher confirms; existe uma pequena janela de perda entre a publicação na DLQ e o `Ack` da entrega original.
- Retry não impede processamento duplicado; idempotência permanece como próxima etapa.
