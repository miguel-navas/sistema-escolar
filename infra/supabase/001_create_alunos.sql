-- Migração inicial: tabela "alunos" (agregado Aluno)
-- Compatível com o mapeamento EF Core em AlunoConfiguration.cs.
-- Rodar no SQL editor do Supabase ou via CLI de migrações.

create table if not exists alunos (
    id uuid primary key,
    nome_completo varchar(200) not null,
    data_nascimento date not null,
    cpf varchar(11) unique,
    responsavel_id uuid not null,
    status varchar(20) not null default 'PreCadastrado',
    consentimento_biometrico_registrado boolean not null default false,
    criado_em timestamp not null default now()
);

create index if not exists ix_alunos_responsavel_id on alunos (responsavel_id);

comment on table alunos is 'Agregado raiz Aluno (bounded context Acadêmico).';
comment on column alunos.consentimento_biometrico_registrado is
    'Obrigatório = true antes de qualquer enrolamento biométrico (LGPD).';
