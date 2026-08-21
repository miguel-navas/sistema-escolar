-- Migração: tabela "professores" (agregado Professor)
-- Compatível com o mapeamento EF Core em ProfessorConfiguration.cs.
-- Rodar no SQL editor do Supabase ou via CLI de migrações, após 006_add_fk_turmas_matriculas.sql.

create table if not exists professores (
    id uuid primary key,
    nome_completo varchar(200) not null,
    email varchar(200) not null,
    formacao varchar(200) not null,
    criado_em timestamp not null default now()
);

comment on table professores is 'Agregado raiz Professor (bounded context Acadêmico).';
