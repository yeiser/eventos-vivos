# EventosVivos — Sistema núcleo de reservas de eventos

Solución fullstack **.NET 10 + Angular** para la gestión de reservas de eventos (control de aforo en
tiempo real, conflictos de agenda por venue y validación de reservas/pagos).

> Prueba técnica · Backend en Clean Architecture + API REST · Frontend Angular (tema Metronic) ·
> PostgreSQL · Docker Compose · Terraform/Azure.
>
> Documentos de referencia:
> - [DISEÑO-ARQUITECTURA.md](DISEÑO-ARQUITECTURA.md) — definición, diseño y decisiones (ADRs).
> - [PLAN-FASES.md](PLAN-FASES.md) — plan de ejecución por fases.

---

## Tecnologías

| Capa | Tecnología |
|------|------------|
| Backend | .NET 10 (Clean Architecture), ASP.NET Core Web API |
| Persistencia | EF Core 10 + PostgreSQL (Npgsql) |
| Validación | FluentValidation |
| Frontend | Angular (standalone + signals) + tema Metronic 8 (Bootstrap 5) |
| Pruebas | xUnit, FluentAssertions, NSubstitute, Testcontainers |
| Infra/Deploy | Docker Compose · Terraform · Azure |

---

## Estructura del repositorio

```
.
├── backend/                # Solución .NET (Domain / Application / Infrastructure / Api + tests)
├── frontend/               # Aplicación Angular (pendiente — Fase 7+)
├── infra/terraform/        # Infraestructura como código (pendiente — Fase 11)
├── docker-compose.yml      # Orquestación local (db; api/frontend en Fase 10)
├── DISEÑO-ARQUITECTURA.md
└── PLAN-FASES.md
```

---

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download) (fijado en `global.json`).
- [Docker](https://www.docker.com/) + Docker Compose.
- (Frontend, fases posteriores) Node.js LTS + Angular CLI.

---

## Ejecución local

### 1. Base de datos (PostgreSQL vía Docker)

```bash
docker compose up -d db
docker compose ps        # verificar estado "healthy"
```

Variables configurables (con valores por defecto para desarrollo): `POSTGRES_DB`, `POSTGRES_USER`,
`POSTGRES_PASSWORD`, `POSTGRES_PORT`.

### 2. Backend

```bash
cd backend
dotnet build              # compila toda la solución
dotnet test               # ejecuta las pruebas
dotnet run --project src/EventosVivos.Api   # API + Swagger UI en /swagger
```

La API aplica migraciones y siembra datos al arrancar (en `Development`). Swagger queda en
`http://localhost:<puerto>/swagger`. Credenciales de demo sembradas: `admin` / `Admin123!` y
`usuario` / `Usuario123!` (la autenticación se activa en la Fase 5).

> **Conflicto de puertos (PostgreSQL local):** si ya tienes un PostgreSQL en el puerto 5432, levanta
> el contenedor en otro puerto y apunta la API ahí:
> ```bash
> POSTGRES_PORT=5440 docker compose up -d db
> # luego, para la API:
> ConnectionStrings__Postgres="Host=127.0.0.1;Port=5440;Database=eventosvivos;Username=eventosvivos;Password=eventosvivos_dev" \
>   dotnet run --project src/EventosVivos.Api
> ```

### 3. Frontend (Angular 22 + tema Metronic)

```bash
cd frontend/eventos-vivos-web
npm install               # usa .npmrc (legacy-peer-deps) por compatibilidad con Angular 22
npm start                 # ng serve en http://localhost:4200
npm run build             # build de producción
npx ng test --watch=false # pruebas (Vitest)
```

La URL de la API en desarrollo se configura en `src/environments/environment.development.ts`
(por defecto `http://localhost:5080/api/v1`). El CORS de la API permite `http://localhost:4200`.

> **Tema Metronic (licencia comercial — no versionado):** los *assets* no están en el repo (ver
> [ADR-008](DISEÑO-ARQUITECTURA.md)). Cópialos desde tu licencia Metronic 8 a
> `frontend/eventos-vivos-web/public/metronic/` con esta estructura mínima:
> `css/style.bundle.css`, `plugins/global/plugins.bundle.{css,js}`, `plugins/global/fonts/`,
> `js/scripts.bundle.js` y `media/` (logos, svg, avatars). `index.html` ya los enlaza.

---

## Estado del proyecto (por fases)

- [x] **Fase 0** — Scaffolding: solución, proyectos, referencias, Docker Compose (db), configuración base.
- [x] **Fase 1** — Dominio + reglas de negocio (RN01–RN07) + 94 pruebas unitarias.
- [x] **Fase 2** — Persistencia (EF Core 10 + PostgreSQL): DbContext, migración, seed, repositorios, `xmin`; pruebas con Testcontainers.
- [x] **Fase 3** — Casos de uso (RF-01..RF-06): CQRS ligero, FluentValidation, transacción + bloqueo anti-sobreventa; prueba de concurrencia.
- [x] **Fase 4** — API REST + ProblemDetails (RFC 7807) + Swagger + CORS + rate limiting + Serilog; pruebas de integración de endpoints.
- [x] **Fase 5** — Autenticación JWT + roles (Admin/Usuario): login, política de fallback, 401/403 ProblemDetails, Swagger Bearer. Incluye **remediación de fuerza bruta** (rate limit por IP + bloqueo de cuenta tras 5 intentos fallidos → 423; ver DISEÑO §15.1).
- [x] **Fase 6** — Auditoría y trazabilidad: interceptores EF (trazabilidad + audit trail inmutable con masking), endpoint `/auditoria` (Admin).
- [x] **Fase 7** — Frontend Angular 22 (zoneless + signals) + tema Metronic 8: layout shell, login, interceptores JWT/errores, guards por rol.
- [x] **Fase 8** — Frontend features: listado+filtros (RF-02), crear evento (RF-01), detalle, reservar (RF-03), confirmar/cancelar (RF-04/05), reporte con ApexCharts (RF-06).
- [x] **Fase 9** — Frontend: vista de auditoría (Admin) con filtros/detalle, UI consciente del rol, UX de reglas, pulido de estados.
- [ ] **Fase 10–11** — Docker Compose + Terraform/Azure + despliegue.

> El detalle de cada fase y sus criterios de aceptación está en [PLAN-FASES.md](PLAN-FASES.md).

---

## Notas

- **Tema Metronic**: es un tema comercial (ThemeForest). Sus *assets* **no** se versionan en el repo;
  se documentará en fases de frontend cómo colocarlos en `frontend/.../src/assets/metronic/` (ver
  [ADR-008](DISEÑO-ARQUITECTURA.md)).
- **Secretos**: nunca en el repositorio; se inyectan por variables de entorno / Azure Key Vault.
