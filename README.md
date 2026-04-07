# LEILAOAUTO - Fase 3

Fase 3 implementa autenticacao, usuario logado (`/api/auth/me`) e gestao de veiculos monitorados com CRUD completo.

## Estrutura da solution

- `LeilaoAuto.Api`
- `LeilaoAuto.Application`
- `LeilaoAuto.Domain`
- `LeilaoAuto.Infrastructure`
- `LeilaoAuto.Workers`
- `LeilaoAuto.Tests`
- `leilaoauto-web`

## Backend - Endpoints de autenticacao

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/auth/me` (autenticado)

## Backend - Endpoints de monitoramento

- `GET /api/monitoring`
- `POST /api/monitoring`
- `PUT /api/monitoring/{id}`
- `DELETE /api/monitoring/{id}`

## Regras aplicadas

- Usuario autenticado gerencia apenas os proprios veiculos.
- Limite de 4 veiculos monitorados por usuario.
- Validacao de campos obrigatorios com FluentValidation.
- Erros retornados em ProblemDetails (incluindo conflito, validacao e regras de dominio).

## Frontend Angular

Telas entregues:

- Login
- Cadastro
- Dashboard inicial
- Monitoramento

Recursos de frontend ativos:

- `AuthService` com estado simples por `signals`.
- `JWT interceptor` para chamadas autenticadas.
- `auth guard` para rotas privadas.
- Formularios com Reactive Forms.
- Tela de monitoramento com listagem e CRUD dos veiculos do usuario.

## Seed inicial

Credenciais seed:

- Admin: `admin@leilaoauto.local` / `Admin1234`
- User: `demo@leilaoauto.local` / `Demo1234`

## Execucao

Subir API + Postgres via Docker:

```bash
docker compose up --build
```

Rodar frontend:

```bash
cd leilaoauto-web
npm install
npm start
```

Endpoints uteis:

- API: `http://localhost:8080`
- Health: `http://localhost:8080/health`
- Swagger (Development): `http://localhost:8080/swagger`
- Frontend: `http://localhost:4200`

## Proximas fases

- Conectores reais de leiloeiros.
- Matching avancado de lotes.
- Score de oportunidade e risco com dados reais.
