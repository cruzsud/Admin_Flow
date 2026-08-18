# Domínio inicial do AdminFlow.Budget

## Objetivo do domínio

O AdminFlow.Budget representa a decisão administrativa de autorizar ou rejeitar uma despesa com base no orçamento disponível de um centro de custo. O primeiro recorte deve demonstrar um fluxo de negócio completo, e não apenas cadastros independentes.

O fluxo do MVP é:

```text
CostCenter -> Budget -> ExpenseRequest -> decisão (Approved ou Rejected)
```

`Department`, execução financeira e acompanhamento posterior à aprovação continuam relevantes, mas não são necessários para provar esse primeiro fluxo.

## Análise crítica

### Partes essenciais

- Um `CostCenter` que identifique onde o recurso é administrado.
- Um `Budget` que declare quanto foi alocado e quanto já está comprometido.
- Uma `ExpenseRequest` com valor, finalidade, solicitante e estado.
- Uma decisão de aprovação ou rejeição sujeita às regras de estado e saldo.
- Consulta da solicitação e de sua decisão.
- Atualização atômica do orçamento e da solicitação ao aprovar.

### Funcionalidades que podem esperar

- `Department` como entidade própria e sua hierarquia organizacional.
- `BudgetAccount` e classificação contábil detalhada.
- Vários níveis ou alçadas de aprovação.
- Edição, cancelamento e reenvio de solicitações.
- Anexos, fornecedores, compras, pagamentos e execução da despesa.
- Autenticação, autorização, notificações, relatórios e trilha de auditoria completa.
- Multi-moeda, remanejamento e suplementação orçamentária.

### Ambiguidades esclarecidas para o MVP

- **Quando o saldo é reservado?** Somente na aprovação.
- **Aprovação equivale a pagamento?** Não. Ela gera compromisso orçamentário, não gasto realizado.
- **Qual moeda?** Apenas BRL.
- **Qual período?** Um exercício fiscal identificado pelo ano civil.
- **Há rascunho ou submissão?** Não. A criação já deixa a solicitação pendente.
- **Há entidade `ExpenseApproval`?** Não no MVP. Os dados da única decisão ficam na solicitação.
- **Há departamento?** Não no MVP. O centro de custo é suficiente como unidade administrativa inicial.

### Riscos de overengineering

- Modelar toda a estrutura organizacional antes do fluxo orçamentário.
- Criar agregados grandes, value objects para todo primitivo ou serviços de domínio genéricos.
- Introduzir CQRS, MediatR, repositório genérico ou hierarquia configurável de aprovação.
- Tratar evento de domínio, evento de integração e mensagem RabbitMQ como a mesma coisa.
- Projetar execução financeira e contabilidade sem requisitos suficientes.

## MVP

O menor MVP funcional permite:

1. criar e consultar um centro de custo;
2. criar um orçamento anual em BRL para esse centro de custo;
3. criar uma solicitação de despesa diretamente no estado `Pending`;
4. consultar uma solicitação e listar solicitações;
5. aprovar quando houver saldo, comprometendo o valor;
6. rejeitar uma solicitação pendente, registrando o motivo;
7. consultar o estado e os dados da decisão.

O MVP termina na decisão administrativa. `Approved` significa autorizado e comprometido, não comprado, pago ou contabilizado.

## Atores mínimos

### Requester

Cria a solicitação e informa o centro de custo/orçamento, valor, descrição e sua identificação. Enquanto não houver autenticação, o identificador do ator será recebido explicitamente e não será uma prova de identidade.

### BudgetManager

Avalia saldo e finalidade, aprova ou rejeita e informa sua identificação. No MVP, esse ator acumula a responsabilidade conceitual de gestor e aprovador orçamentário para evitar um fluxo com múltiplos níveis.

### Responsabilidades futuras

- Um `Manager` poderá realizar a aprovação administrativa antes da avaliação orçamentária.
- O solicitante não deverá aprovar a própria solicitação.
- Alçadas poderão variar conforme valor, centro de custo ou papel.
- Autenticação confirmará a identidade; autorização determinará as ações permitidas.

A segregação é documentada agora, mas sua aplicação confiável depende da fase de segurança. Até lá, identificadores recebidos pela API servem apenas para exercitar o domínio.

## Conceitos escolhidos

### CostCenter

- **Significado:** unidade administrativa à qual um orçamento pertence.
- **Responsabilidade:** manter identidade, código e nome válidos desde a criação.
- **Identidade:** `Guid` gerado na criação e exposto por `Id`.
- **Relações:** possui orçamentos de diferentes exercícios.
- **Invariantes da entidade:** identidade não vazia; código e nome não podem ser nulos, vazios ou compostos apenas por espaços; espaços externos são removidos.
- **Regra do conjunto:** código deve ser único. Essa regra não pode ser verificada por uma entidade isolada e será aplicada no caso de uso/persistência em fase posterior.
- **Lifecycle atual:** criado e mantido sem transições de estado. Ativação, edição e inativação ficam adiadas até existir um requisito concreto.
- **Encapsulamento:** `Id`, `Code` e `Name` são somente leitura após a construção; alterações arbitrárias não são permitidas.

Implementação atual:

```text
new CostCenter(code, name)
  -> valida dados obrigatórios
  -> remove espaços externos
  -> gera identidade
  -> retorna uma entidade válida ou falha por completo
```

### Budget

- **Significado:** envelope orçamentário de um centro de custo em um exercício.
- **Responsabilidade:** conhecer alocação e compromissos e proteger a disponibilidade.
- **Identidade:** `Guid` gerado na criação e exposto por `Id`.
- **Relações:** referencia um `CostCenter` existente por `CostCenterId`.
- **Invariantes implementadas:** centro de custo não vazio; exercício entre 1 e 9999; alocação maior que zero e com no máximo duas casas decimais; comprometido inicial igual a zero; disponível calculado por alocado menos comprometido.
- **Regras do conjunto/persistência:** combinação centro de custo/exercício única; centro de custo referenciado deve existir; valores usam `numeric(18,2)`; banco impede comprometido negativo ou superior à alocação.
- **Lifecycle atual:** criado com valor alocado e comprometido igual a zero. Comprometer saldo, suplementar, reduzir e encerrar ficam adiados para fases com casos de uso concretos.
- **Moeda:** BRL implícito e único no MVP; não há funcionalidade multi-moeda.

Implementação atual:

```text
new Budget(costCenterId, fiscalYear, allocated)
  -> valida centro, exercício e valor
  -> gera identidade
  -> define Committed = 0
  -> calcula Available = Allocated - Committed
```

### ExpenseRequest

- **Significado:** pedido para autorizar uma despesa contra um orçamento.
- **Responsabilidade:** proteger seus dados, estado e decisão.
- **Identidade:** `ExpenseRequestId`.
- **Relações:** referencia um `Budget` e, por consequência, seu `CostCenter`.
- **Invariantes:** valor maior que zero; descrição e solicitante obrigatórios; apenas uma decisão terminal; rejeição exige motivo.
- **Lifecycle:** criada como `Pending`; termina como `Approved` ou `Rejected`.

Implementado na Fase 5:

```text
ExpenseRequest
  Id
  BudgetId
  RequesterId
  Description
  Amount (decimal, BRL)
  Status = Pending
```

A criação valida referências não vazias, descrição obrigatória e normalizada, valor positivo com no máximo duas casas decimais e estado inicial `Pending`.

Na Fase 6, `Approve` e `Reject` passaram a controlar as transições. Ambas registram decisor e instante. Aprovação impede autoaprovação e compromete o valor no orçamento; rejeição exige motivo e não altera saldo. `Approved` e `Rejected` são terminais.

### Conceitos adiados

- **Department:** sua relação com centros de custo ainda não altera nenhuma decisão do MVP.
- **ExpenseApproval:** uma entidade própria só se justifica com múltiplas etapas, histórico de decisões ou lifecycle independente.
- **Money:** o MVP pode começar com `decimal` e moeda fixa BRL; um value object será reavaliado se regras de moeda/arredondamento crescerem.

## Modelo orçamentário mínimo

Termos adotados:

- `Allocated`: valor aprovado para o orçamento no exercício.
- `Committed`: soma das solicitações aprovadas que reservaram recursos.
- `Available`: valor ainda autorizável.
- `Spent`: valor efetivamente executado/pago; não existe no MVP.

Fórmula do MVP:

```text
Available = Allocated - Committed
```

A criação de uma solicitação pendente não altera saldo. A aprovação exige `Available >= ExpenseRequest.Amount` e, na mesma operação lógica, aumenta `Committed` e muda a solicitação para `Approved`.

O comportamento `Budget.Commit(amount)` protege `Committed <= Allocated`. A Application verifica o saldo antes de alterar as entidades e persiste solicitação e orçamento em uma única chamada. A constraint do banco reforça a coerência entre estado e dados de decisão.

Consequências:

- O modelo separa autorização de pagamento e evita chamar compromisso de gasto.
- Duas solicitações pendentes podem somar mais que o disponível; a primeira aprovada consome o saldo e a outra poderá falhar.
- A aprovação precisará de transação e controle de concorrência quando houver persistência.
- Não há liberação de compromisso, pois cancelamento de aprovadas está fora do MVP.
- Valores usam `decimal`, BRL, duas casas decimais e arredondamento não é necessário enquanto entradas com mais de duas casas forem rejeitadas.

## Lifecycle de ExpenseRequest

```text
Create
  |
  v
Pending ----approve com saldo----> Approved
  |
  +--------reject com motivo-----> Rejected
```

### Transições válidas

- criação -> `Pending`;
- `Pending` -> `Approved`, se houver saldo suficiente;
- `Pending` -> `Rejected`, se houver motivo.

### Transições inválidas

- `Approved` -> `Approved` ou `Rejected`;
- `Rejected` -> `Rejected` ou `Approved`;
- qualquer atribuição arbitrária de estado;
- aprovação sem saldo ou rejeição sem motivo.

`Draft` e `Submitted` foram removidos porque não há edição nem etapa distinta de submissão no MVP. Se essas capacidades surgirem, o workflow será reavaliado.

## Regras de negócio classificadas

### Necessárias para o MVP

- Valor da solicitação maior que zero, em BRL, com no máximo duas casas decimais.
- Centro de custo e orçamento devem existir e estar relacionados.
- Descrição e identificador do solicitante são obrigatórios.
- Solicitação nasce em `Pending`.
- Somente solicitação pendente pode ser decidida.
- Aprovação exige saldo disponível suficiente.
- Aprovação compromete orçamento exatamente uma vez.
- Rejeição exige motivo e não altera orçamento.
- Decisão registra responsável e instante.
- Alocação é positiva e centro de custo/exercício não se repete.

### Importantes para fases posteriores

- Impedir autoaprovação com identidade autenticada.
- Controle otimista de concorrência e tratamento explícito de conflitos.
- Auditoria imutável de cada ação.
- Cancelamento com liberação de compromisso.
- Inativação de centros de custo e encerramento do exercício.
- Alçadas, múltiplos aprovadores e separação entre Manager e BudgetManager.

### Fora do escopo inicial

- Pagamento, conciliação, contabilização e cálculo de `Spent`.
- Multi-moeda e conversão cambial.
- Remanejamento, suplementação e redução de orçamento.
- Aprovação paralela, delegação e workflow configurável.
- Anexos, fornecedores e processo de compras.

## Casos de uso do MVP

| Caso de uso | Ator | Entrada principal | Resultado | Regra principal |
|---|---|---|---|---|
| `CreateCostCenter` | BudgetManager | código e nome | centro criado | código único e campos válidos |
| `GetCostCenter` | Requester/BudgetManager | id | centro encontrado | id deve existir |
| `CreateBudget` | BudgetManager | centro, exercício e alocação | orçamento criado | um por centro/exercício; alocação positiva |
| `GetBudget` | Requester/BudgetManager | id | alocado, comprometido e disponível | cálculo usa semântica documentada |
| `CreateExpenseRequest` | Requester | orçamento, valor, descrição e solicitante | solicitação pendente | referências válidas e valor positivo |
| `GetExpenseRequest` | Requester/BudgetManager | id | estado e decisão | id deve existir |
| `ListExpenseRequests` | Requester/BudgetManager | filtros opcionais de estado/orçamento | lista paginável | sem regras novas de domínio |
| `ApproveExpenseRequest` | BudgetManager | id da solicitação e decisor | solicitação aprovada | pendente e saldo suficiente |
| `RejectExpenseRequest` | BudgetManager | id, decisor e motivo | solicitação rejeitada | pendente e motivo obrigatório |

Listagens gerais de centros e orçamentos são úteis, mas podem ser acrescentadas quando a interface/manual de teste exigir; não são necessárias para provar o fluxo central.

## Persistência a considerar em fases futuras

- **Implementado na Fase 3:** `CostCenter` é mapeado para `cost_centers`; `Id` usa `uuid`; código e nome são obrigatórios; `CostCenter.Code` possui índice único.
- Unicidade de `(CostCenterId, FiscalYear)` em `Budget`.
- Precisão monetária coerente no PostgreSQL.
- Chaves estrangeiras entre solicitação, orçamento e centro de custo.
- Transação única para aprovar e comprometer saldo.
- Concorrência para impedir comprometimento acima da alocação.

Essas constraints reforçam, mas não substituem, as invariantes do domínio.

O índice único atual diferencia maiúsculas de minúsculas conforme a comparação padrão do PostgreSQL. Portanto, `ADM-001` e `adm-001` são códigos distintos por enquanto. Normalização de caixa ou comparação case-insensitive exige uma decisão explícita de negócio e permanece pendente.

## Comportamentos que exigirão testes

- **Cobertos na Fase 2:** criar `CostCenter` válido; gerar identidade não vazia; normalizar espaços externos; rejeitar código ou nome nulo, vazio ou composto apenas por espaços.
- **Cobertos na Fase 3:** persistir e recuperar `CostCenter`; impedir códigos duplicados entre centros de custo.
- **Cobertos na Fase 4:** criar `Budget` válido; rejeitar centro vazio, exercício inválido, alocação não positiva ou com mais de duas casas; iniciar compromisso em zero; calcular disponibilidade; persistir relacionamento; impedir orçamento duplicado por centro/exercício; rejeitar centro inexistente.
- aprovar com saldo e comprometer exatamente o valor;
- recusar aprovação sem saldo;
- recusar segunda decisão;
- rejeitar com motivo sem alterar orçamento;
- garantir que uma falha não deixe orçamento e solicitação inconsistentes.

## Decisões pendentes

- Formato dos identificadores dos conceitos futuros; `CostCenter` e `Budget` utilizam `Guid`.
- Tamanho e regras exatas de código, nome, descrição e motivo.
- Política de paginação e filtros da listagem.
- Estratégia concreta de concorrência no PostgreSQL/EF Core.
- Forma de representar o ator antes da autenticação nos endpoints de desenvolvimento.
- Se endpoints de negócio usarão controllers ou Minimal APIs quando forem introduzidos.
