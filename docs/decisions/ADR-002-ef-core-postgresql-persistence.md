# ADR-002: Persistência com EF Core e PostgreSQL

## Contexto

`CostCenter` precisa sobreviver ao processo da API e a unicidade de seu código depende do conjunto completo de centros cadastrados. O domínio não deve conhecer banco de dados, SQL ou framework de persistência.

## Decisão

Usar Entity Framework Core com o provider Npgsql no projeto Infrastructure e PostgreSQL como banco relacional.

O mapeamento será feito por classes `IEntityTypeConfiguration<T>`, mantendo a entidade livre de atributos de persistência. Mudanças de esquema serão versionadas por migrations. A unicidade de `CostCenter.Code` será reforçada por índice único no PostgreSQL.

O PostgreSQL local será executado por Docker Compose. Credenciais serão fornecidas por variáveis de ambiente e não serão versionadas.

## Alternativas

### SQL manual com Npgsql

Oferece controle explícito sobre SQL e menos abstração, mas exige código repetitivo de materialização, tracking de mudanças e migrations externas. Não oferece vantagem concreta para o modelo atual.

### EF Core InMemory

É simples para testes, mas não reproduz constraints, migrations, tipos e semântica relacional do PostgreSQL. Não é adequado para validar persistência real.

### SQLite para testes

É relacional, mas diferenças de tipos, SQL e constraints ainda poderiam ocultar problemas específicos do PostgreSQL.

### Repository genérico sobre EF Core

Adicionaria outra abstração sem resolver um problema atual. `DbContext` já oferece comportamentos semelhantes a Repository e Unit of Work.

### Testcontainers nesta fase

Automatizaria o lifecycle do container durante o teste, mas acrescentaria pacote, API de infraestrutura e tempo de inicialização. O Compose existente é suficiente para o checkpoint atual.

## Consequências

### Benefícios

- Persistência e schema reais do PostgreSQL são validados.
- Domain continua independente de infraestrutura.
- Migrations tornam o schema reproduzível e versionado.
- Constraint única protege contra duplicidade inclusive sob concorrência.
- Consultas futuras podem usar o provider oficial Npgsql.

### Trade-offs

- EF Core acrescenta tracking, convenções e um modelo próprio que precisam ser compreendidos.
- Testes PostgreSQL exigem Docker em execução e são mais lentos que testes unitários.
- Migrations precisam acompanhar toda mudança relevante no mapeamento.
- Erros de constraint precisam ser traduzidos em resultados de aplicação quando os casos de uso forem implementados.
- Comparação de código é case-sensitive até uma decisão explícita em contrário.
