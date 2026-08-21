-- Migração: tabela "disciplinas" (agregado Disciplina)
-- Compatível com o mapeamento EF Core em DisciplinaConfiguration.cs.
-- Rodar no SQL editor do Supabase ou via CLI de migrações, após 007_create_professores.sql.

create table if not exists disciplinas (
    id uuid primary key,
    nome varchar(100) not null,
    carga_horaria integer not null,
    ano_escolar_id uuid not null,
    criado_em timestamp not null default now(),
    constraint fk_disciplinas_ano_escolar foreign key (ano_escolar_id) references anos_escolares (id)
);

create index if not exists ix_disciplinas_ano_escolar_id on disciplinas (ano_escolar_id);

comment on table disciplinas is 'Agregado raiz Disciplina (bounded context Acadêmico).';
