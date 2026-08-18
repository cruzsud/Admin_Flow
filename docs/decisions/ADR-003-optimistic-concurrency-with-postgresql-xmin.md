# ADR-003: Concorrência otimista do orçamento com xmin

## Contexto

Duas solicitações podem ser aprovadas simultaneamente contra o mesmo saldo disponível. Sem controle de concorrência, ambas poderiam ler o mesmo valor e sobrescrever `Committed`, aprovando despesas acima do orçamento ou perdendo uma atualização.

## Decisão

Usar a coluna interna `xmin` do PostgreSQL como token de concorrência otimista para `Budget`. O EF Core inclui a versão lida na condição do `UPDATE`. Se a linha tiver sido alterada, a gravação falha com conflito de concorrência e a transação inteira é revertida.

`xmin` é configurado como propriedade sombra do EF Core. Assim, o Domain não recebe uma propriedade técnica de persistência. A migration não cria nem remove `xmin`, pois o PostgreSQL já mantém essa coluna internamente.

## Alternativas

- Bloqueio pessimista com `SELECT FOR UPDATE`: correto, mas exige transação explícita e mantém bloqueios enquanto a operação acontece.
- Coluna de versão própria: mais portável, porém exige manter uma coluna adicional e sua atualização.
- Apenas conferir saldo antes de salvar: insuficiente, pois duas operações podem conferir o mesmo saldo simultaneamente.
- Nível de isolamento serializável: oferece proteção ampla, mas aumenta conflitos e exige estratégia de repetição para toda a transação.

## Consequências

- Atualizações concorrentes não sobrescrevem silenciosamente o orçamento.
- Solicitação e orçamento são revertidos juntos em caso de conflito.
- O Domain permanece independente do PostgreSQL.
- A solução fica vinculada ao mecanismo `xmin` enquanto esta estratégia for usada.
- A futura camada HTTP deverá traduzir conflitos para resposta de negócio apropriada e poderá orientar uma nova tentativa.
