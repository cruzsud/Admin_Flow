# ADR-004: Logging estruturado com Serilog

## Contexto

O fluxo de aprovação já altera estado e saldo, mas não existia registro operacional estruturado. Mensagens concatenadas dificultariam busca por solicitação, orçamento, ator ou ação e aumentariam o risco de formatos inconsistentes antes da introdução de RabbitMQ.

## Decisão

Usar `ILogger<T>` na Application e Serilog como provedor configurado na API. Os eventos de negócio usam templates com propriedades nomeadas e são emitidos somente depois da persistência bem-sucedida. O sink inicial é o console.

Registrar somente identificadores, ação, valor e instante. Não registrar descrição, motivo de rejeição, secrets, tokens, connection strings, headers de autorização ou corpos de requisição.

## Alternativas

- Logging padrão do ASP.NET Core: suficiente para mensagens básicas, mas Serilog fornece configuração e ecossistema consistentes para eventos estruturados.
- Dependência direta de Serilog na Application: mais simples na chamada, porém acoplaria o caso de uso à implementação concreta.
- Arquivos locais: acrescentariam rotação, retenção e armazenamento sem requisito atual.
- Banco de auditoria: auditoria durável possui finalidade diferente de logs operacionais e será tratada quando houver requisito próprio.

## Consequências

- Eventos podem ser filtrados por propriedades, não apenas por texto.
- Application continua substituível e testável por depender de `ILogger<T>`.
- Domain permanece livre de infraestrutura.
- Logs no console não formam uma trilha de auditoria durável.
- Dados financeiros e identificadores exigem acesso operacional controlado ao ambiente de logs.
- Novos destinos de logs poderão ser adicionados depois sem mudar as regras de negócio.
