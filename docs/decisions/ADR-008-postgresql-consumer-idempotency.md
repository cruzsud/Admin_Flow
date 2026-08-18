# ADR-008: Idempotência do consumidor com PostgreSQL

## Contexto

RabbitMQ oferece entrega pelo menos uma vez. Uma mensagem pode reaparecer quando o processamento termina, mas a confirmação não chega ao broker, ou quando ocorre retry. Sem deduplicação, o mesmo `ExpenseApprovedIntegrationEvent` poderia produzir o mesmo efeito mais de uma vez.

## Decisão

Registrar cada `EventId` na tabela técnica `processed_integration_events`, usando `event_id` como chave primária.

O processador abre uma transação, tenta inserir o identificador, executa o handler e confirma a transação. Se o handler falhar, a transação é revertida e uma nova tentativa pode processar o evento. Se a chave já existir, inclusive por uma entrega concorrente, o handler não é chamado e a mensagem pode receber `Ack`.

O processador usa `IDbContextFactory<BudgetDbContext>` porque o consumidor é um serviço hospedado singleton e cada entrega precisa de um contexto EF Core independente.

## Alternativas

- Manter IDs em memória: perde dados ao reiniciar e não coordena múltiplas instâncias.
- Consultar antes de processar: duas entregas concorrentes podem observar ausência e executar juntas.
- Redis: resolveria a coordenação, mas adicionaria infraestrutura sem necessidade enquanto PostgreSQL já está disponível.
- Exigir efeito naturalmente idempotente em todo handler: desejável quando possível, porém não fornece uma defesa uniforme para este consumidor.

## Consequências

- Duplicatas sequenciais e concorrentes são bloqueadas pela restrição do PostgreSQL.
- Falhas do handler não deixam um evento marcado como concluído.
- O Domain permanece independente de mensageria e persistência técnica.
- A tabela cresce continuamente até que seja definida uma política segura de retenção.
- A transação protege a marcação; efeitos externos que não participem dela não recebem garantia de execução exatamente uma vez.
- O padrão não corrige a janela entre o commit da aprovação e a publicação; isso exigiria avaliar Outbox separadamente.
