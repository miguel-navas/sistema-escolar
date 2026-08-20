-- Migração: tabela "anos_letivos" (agregado AnoLetivo)
-- Compatível com o mapeamento EF Core em AnoLetivoConfiguration.cs.
-- Rodar no SQL editor do Supabase ou via CLI de migrações, após 003_create_matriculas.sql.

create table if not exists anos_letivos (
    id uuid primary key,
    ano integer not null,
    data_inicio date not null,
    data_fim date not null,
    status varchar(20) not null default 'Planejado',
    criado_em timestamp not null default now()
);

comment on table anos_letivos is 'Agregado raiz AnoLetivo (bounded context Acadêmico).';
comment on column anos_letivos.status is
    'Ciclo Planejado -> Ativo -> Encerrado. Regra "só um Ativo por vez" é reforçada na Application, não no banco.';
