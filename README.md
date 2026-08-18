# Sistema Escolar

Ver `CLAUDE.md` para contexto completo do projeto (arquitetura, stack, convencoes).

## Setup rapido
1. Copie `.env.example` para `.env` e preencha os valores.
2. Backend: `cd backend; dotnet build`
3. Frontend: `cd frontend-web; npm install; npm run dev`
4. Mobile: `cd mobile; npm install; npm start`

## Com Claude Code
Rode `claude` na raiz do projeto. O contexto (CLAUDE.md, docs/DOMAIN.md, docs/DECISIONS.md)
sera carregado automaticamente para orientar as sugestoes e implementacoes.
