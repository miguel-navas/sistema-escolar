-- Migração: tabela "anos_escolares" (agregado AnoEscolar / série)
-- Compatível com o mapeamento EF Core em AnoEscolarConfiguration.cs.
-- Rodar no SQL editor do Supabase ou via CLI de migrações, após 004_create_anos_letivos.sql.

create table if not exists anos_escolares (
    id uuid primary key,
    nome varchar(100) not null,
    nivel_ensino varchar(30) not null,
    criado_em timestamp not null default now()
);

comment on table anos_escolares is 'Agregado raiz AnoEscolar/Série (bounded context Acadêmico).';
