# ADR-001: Estrutura inicial em quatro projetos

## Contexto

O AdminFlow.Budget precisa ensinar e demonstrar separação entre regras administrativas, coordenação de casos de uso, entrada HTTP e detalhes técnicos. Uma API em projeto único seria menor, mas tornaria os limites menos explícitos e facilitaria o acoplamento do domínio ao ASP.NET Core e ao EF Core conforme o sistema crescer.

## Decisão

Adotar inicialmente quatro projetos de produção:

- `AdminFlow.Budget.Api`;
- `AdminFlow.Budget.Application`;
- `AdminFlow.Budget.Domain`;
- `AdminFlow.Budget.Infrastructure`.

As dependências apontam para o domínio: Application referencia Domain; Infrastructure referencia Application/Domain; API referencia Application e Infrastructure para composição. Domain não referencia frameworks ou projetos externos da aplicação.

Esta é uma aplicação única organizada em camadas, não uma decomposição em microserviços.

## Alternativas

### Projeto único por feature

Menos configuração e navegação inicial, porém limites de dependência apenas convencionais e menor clareza didática para a evolução planejada.

### Três projetos sem Application separada

Seria suficiente para poucos endpoints, mas a aplicação terá casos de uso de criação, consulta e decisão; separar sua orquestração evita que API ou Infrastructure assumam esse papel.

### Mais projetos e padrões

Separar contratos, shared kernel, CQRS, mediador ou serviços genéricos aumentaria complexidade sem resolver um problema atual.

## Consequências

### Benefícios

- Regras de domínio permanecem testáveis e independentes.
- Responsabilidades e direção das dependências ficam visíveis na Solution.
- Persistência e HTTP podem evoluir sem definir o modelo de negócio.
- A estrutura suporta as fases futuras sem antecipar microserviços.

### Trade-offs

- Mais projetos, referências e navegação desde a Fundação.
- Necessidade de mapear contratos nos limites quando isso for útil.
- A separação não garante boa arquitetura por si só; responsabilidades ainda precisam ser respeitadas.
- Interfaces e abstrações serão adicionadas apenas diante de dependências concretas, não automaticamente.

