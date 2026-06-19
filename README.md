# EventosVivos — Sistema núcleo de reservas de eventos

Solución **fullstack .NET 10 + Angular 22** para el núcleo de un sistema de reservas de eventos:
control de aforo en tiempo real (sin sobreventa), prevención de conflictos de agenda por venue y
automatización de la validación de reservas y pagos.

> Prueba técnica · Backend en **Clean Architecture** + API REST · Frontend Angular (tema Metronic) ·
> PostgreSQL · JWT + roles · Auditoría · Docker Compose · (Terraform/Azure para despliegue).

---

## Tecnologías

| Capa | Tecnología |
|------|------------|
| Backend | **.NET 10** (Clean Architecture), ASP.NET Core Web API |
| Persistencia | **EF Core 10 + PostgreSQL** (Npgsql), migraciones + seed |
| Validación | FluentValidation (entrada) + invariantes de dominio |
| Auth | JWT Bearer + roles (Admin/Usuario), PBKDF2, lockout anti-fuerza-bruta |
| Frontend | **Angular 22** (standalone, zoneless, signals) + tema **Metronic 8** (Bootstrap 5) + ApexCharts |
| Pruebas | xUnit, FluentAssertions, NSubstitute, **Testcontainers** (backend) · **Vitest** (frontend) |
| Empaquetado | **Docker** (multi-stage) + **Docker Compose** · **Nginx** (sirve la SPA y hace de proxy de la API) |
| CI/CD | **GitHub Actions** (build + test de backend y frontend, smoke de imágenes) |
| Infra | Terraform · Azure (despliegue) |

---

## Arquitectura y decisiones clave

### Clean Architecture (4 capas)

```
┌───────────────────────────── Api ─────────────────────────────┐
│  Controllers · Middleware · ProblemDetails · JWT · Swagger     │
└───────────────▲───────────────────────────────┬───────────────┘
                │ depende de                     │ depende de
┌───────────────┴──────────────┐   ┌─────────────▼──────────────┐
│         Application           │   │        Infrastructure       │
│  Casos de uso (CQRS ligero)   │◄──┤  EF Core · Repositorios     │
│  DTOs · Validators · Puertos  │   │  Interceptores · Seed · JWT │
└───────────────▲──────────────┘   └────────────────────────────┘
                │ depende de
┌───────────────┴──────────────────────────────────────────────┐
│                           Domain                               │
│  Entidades · Value Objects · Reglas (RN/RF) · sin dependencias │
└────────────────────────────────────────────────────────────────┘
```

La **regla de dependencia** apunta hacia adentro: el `Domain` no referencia a nadie; `Application`
define puertos (interfaces) que `Infrastructure` implementa; `Api` compone todo por DI.

### Decisiones de diseño (y por qué)

- **Modelo de dominio rico**: las reglas de negocio (RN01–RN07) viven en las entidades (`Evento`,
  `Reserva`) y se prueban sin infraestructura. Evita el *anemic domain model*.
- **Anti-sobreventa real** (problema #1 del cliente): el flujo de reserva corre dentro de una
  **transacción con bloqueo pesimista** (`SELECT … FOR UPDATE` sobre el evento), de modo que las
  reservas concurrentes se serializan y el aforo nunca se excede. Reforzado con `xmin` (token de
  concurrencia optimista de PostgreSQL). Hay una **prueba de concurrencia** que lanza N reservas en
  paralelo sobre un aforo pequeño y verifica `Σ ocupadas ≤ capacidad`.
- **Tiempo inyectable** (`IClock`): todas las reglas sensibles a la hora (RN03/RN04/RN06/RN07, ventana
  de 24h) son deterministas y testeables.
- **CQRS ligero** en `Application` (comandos/queries como handlers, sin MediatR) — explícito y testeable.
- **Errores RFC 7807** (`application/problem+json`) uniformes, con código de regla (`RN0x`) y `traceId`.
  Distinción intencional: **400** (entrada), **401/403** (auth), **409/422** (estado/regla de negocio).
- **Auditoría como *cross-cutting concern***: dos interceptores de EF Core llenan la **trazabilidad**
  (creado/modificado por quién y cuándo) y un **audit trail inmutable** (valores antes/después, con
  *masking* de datos sensibles), sin contaminar la lógica de dominio.
- **PostgreSQL**: `ILIKE` nativo para la búsqueda de título (RF-02) y `xmin` para concurrencia.
- **Frontend Angular última versión + tema Metronic**: se integra el **CSS** de Metronic y se replica
  el *layout shell* en componentes standalone (no se usa el JS de Metronic para evitar conflictos con
  las librerías de Angular). El frontend usa signals + interceptores (JWT y errores) + guards por rol.

### Seguridad

- **JWT Bearer + roles** (Admin/Usuario) con política de *fallback* (todo requiere autenticación salvo
  `login`). Admin: crear evento, confirmar pago, reporte, auditoría, gestión de reservas.
- **Remediación de fuerza bruta en dos capas**: (1) *rate limiting* por IP en `login` y (2) **bloqueo
  de cuenta** tras 5 intentos fallidos durante 15 min (→ HTTP 423). El lockout está modelado en el dominio.
- **Hash de contraseñas** PBKDF2 con comparación en tiempo constante; **validación/whitelisting** de
  entrada (DTOs explícitos); **CORS** restringido; **sin secretos** en el repositorio.

---

## Reglas de negocio y ambigüedades resueltas

El enunciado contiene **contradicciones/imprecisiones intencionales**; detectarlas y resolverlas con
criterio es parte del valor. Las decisiones tomadas:

| Caso | Resolución |
|------|-----------|
| **RF-05 contradictorio** (cancelar "desde confirmada" y a la vez "rechazar si confirmada") | Se cancela desde *pendiente* y *confirmada*; se rechaza solo en estados **terminales**. Es la única lectura coherente con RN07. |
| **Composición de límites por transacción** (RN04 <1h, RF-03 <24h→5, RN05 >$100→10) | Se aplican todas y **gana la más restrictiva** (`min`); <1h prohíbe reservar. |
| **¿Las reservas pendientes consumen cupo?** | Sí: `Pendiente + Confirmada + Perdida` ocupan aforo (anti-sobreventa). El reporte (RF-06) cuenta solo **confirmadas**. |
| **RN06 auto-completado** sin scheduler | Estado **efectivo** derivado al leer (`ahora > fin → Completado`), determinista con `IClock`. |
| **Zona horaria (RN03 noche/fin de semana)** | Las fechas se reciben con el *offset* local del venue (Colombia) para validar la hora de pared; se **normalizan a UTC** al persistir (requisito de Npgsql). |

RF/RN cubiertos: **RF-01..RF-06** (crear/listar/reservar/confirmar/cancelar/reporte) y **RN01..RN07**
(capacidad ≤ venue, solapamiento de venues, horario nocturno, reserva tardía, límite por precio,
auto-completado, cancelación con penalización), todos con pruebas de borde.

---

## Estructura del repositorio

```
.
├── backend/                       # Solución .NET (Clean Architecture)
│   ├── src/
│   │   ├── EventosVivos.Domain/         # Entidades, VOs, reglas (sin dependencias)
│   │   ├── EventosVivos.Application/    # Casos de uso, DTOs, validators, puertos
│   │   ├── EventosVivos.Infrastructure/ # EF Core, repos, interceptores, seed, JWT
│   │   └── EventosVivos.Api/            # Controllers, middleware, auth, Swagger
│   └── tests/                           # Domain / Application / Integration (Testcontainers)
│   └── Dockerfile                       # Imagen multi-stage de la API
├── frontend/eventos-vivos-web/    # Angular 22 (standalone + signals) + tema Metronic
│   ├── Dockerfile                       # Build de Angular + Nginx
│   └── nginx.conf                       # SPA fallback + proxy /api → contenedor api
├── scripts/seed-demo.ps1          # Generador de datos de demostración (vía API)
├── .github/workflows/ci.yml       # CI: build + test (backend y frontend) + smoke Docker
├── docker-compose.yml             # Stack local: db + api + frontend
├── docker-compose.prod.yml        # Plantilla de despliegue (sin db; imágenes de registry)
└── README.md
```

---

## Requisitos

- [.NET SDK 10](https://dotnet.microsoft.com/download) (fijado en `global.json`).
- [Docker](https://www.docker.com/) + Docker Compose.
- Node.js ≥ 22.22 + npm (para el frontend).

---

## Ejecución con Docker Compose (recomendada)

Levanta **todo el stack** (PostgreSQL + API + frontend) con un solo comando:

```bash
docker compose up --build
```

| Servicio | URL | Notas |
|----------|-----|-------|
| **Web** | http://localhost:8080 | SPA de Angular (Nginx) |
| **API** | http://localhost:5080 | Swagger en `/swagger` |
| **DB** | localhost:5440 | PostgreSQL (puerto 5440 para no chocar con uno local) |

La API aplica migraciones y siembra datos al arrancar (`Database__AutoMigrate=true`). El frontend
llama a la API por **ruta relativa** (`/api/v1`): Nginx hace de *reverse proxy* hacia el contenedor
`api`, así que **no hay CORS** en este modo. Credenciales de demo: `admin` / `Admin123!` y
`usuario` / `Usuario123!`.

> Puertos configurables por variables de entorno: `WEB_PORT` (8080), `API_PORT` (5080),
> `POSTGRES_PORT` (5440). El secreto JWT se inyecta con `JWT_SECRET`.
> Para parar y borrar el volumen de datos: `docker compose down -v`.

Para producción existe `docker-compose.prod.yml` (sin contenedor de base de datos —usa PostgreSQL
gestionado— e imágenes desde un registry; todo secreto se inyecta por variable de entorno).

---

## Ejecución local (sin Docker)

### 1. Base de datos (PostgreSQL vía Docker)

```bash
docker compose up -d db
docker compose ps        # verificar estado "healthy"
```

Variables configurables (con valores por defecto de desarrollo): `POSTGRES_DB`, `POSTGRES_USER`,
`POSTGRES_PASSWORD`, `POSTGRES_PORT` (por defecto **5440**, para no chocar con un PostgreSQL local en
el 5432).

### 2. Backend

Como la base de datos se publica en el **5440**, se pasa la cadena de conexión a la API:

```bash
cd backend
dotnet build
dotnet test                                  # pruebas (incluye integración con Testcontainers)
ConnectionStrings__Postgres="Host=127.0.0.1;Port=5440;Database=eventosvivos;Username=eventosvivos;Password=eventosvivos_dev" \
  dotnet run --project src/EventosVivos.Api  # API + Swagger en /swagger
```

La API aplica migraciones y siembra datos al arrancar (en `Development`). Credenciales de demo:
`admin` / `Admin123!` y `usuario` / `Usuario123!`.

> Si prefieres no pasar la cadena, levanta la DB en el puerto que espera `appsettings.json` (5432)
> con `POSTGRES_PORT=5432 docker compose up -d db` —siempre que el 5432 esté libre.

### 3. Frontend (Angular 22 + tema Metronic)

```bash
cd frontend/eventos-vivos-web
npm install               # usa .npmrc (legacy-peer-deps) por compatibilidad con Angular 22
npm start                 # ng serve en http://localhost:4200
npm run build             # build de producción
npx ng test --watch=false # pruebas (Vitest)
```

La URL de la API en desarrollo está en `src/environments/environment.development.ts`
(por defecto `http://localhost:5080/api/v1`). El CORS de la API permite `http://localhost:4200`.

> **Tema Metronic (licencia comercial — no versionado):** los *assets* no están en el repositorio.
> Cópialos desde tu licencia de **Metronic 8** a `frontend/eventos-vivos-web/public/metronic/` con
> esta estructura mínima: `css/style.bundle.css`, `plugins/global/plugins.bundle.css`,
> `plugins/global/fonts/` y `media/` (logos, svg, avatars). El `index.html` ya enlaza el CSS.

### 4. (Opcional) Datos de demostración

Con la API corriendo, puebla eventos y reservas realistas (respetando las reglas de negocio):

```bash
powershell -ExecutionPolicy Bypass -File scripts/seed-demo.ps1 -ApiBase http://localhost:5080/api/v1
```

---

## Pruebas

- **Backend** (~169): unitarias de dominio (todas las RN/RF con casos de borde), unitarias de
  aplicación (handlers + validators con NSubstitute), e integración con **Testcontainers** (PostgreSQL
  efímero) incluyendo la **prueba de concurrencia anti-sobreventa** y los flujos de API end-to-end.
- **Frontend** (~24): servicios, interceptores (JWT/errores), sesión y componentes clave (login,
  formulario de evento, límite de reserva) con **Vitest**.

```bash
cd backend && dotnet test
cd frontend/eventos-vivos-web && npx ng test --watch=false
```

---

## Endpoints principales (`/api/v1`)

| Método | Ruta | Rol |
|--------|------|-----|
| `POST` | `/auth/login` | público |
| `GET` | `/venues` · `/eventos` · `/eventos/{id}` | autenticado |
| `POST` | `/eventos` | Admin |
| `GET` | `/eventos/{id}/reporte` · `/eventos/{id}/reservas` · `/reservas` · `/auditoria` | Admin |
| `POST` | `/eventos/{id}/reservas` · `/reservas/{id}/cancelacion` | Usuario/Admin |
| `POST` | `/reservas/{id}/confirmacion` | Admin |
| `GET` | `/reservas/{id}` | Usuario/Admin |

Errores en formato `application/problem+json` (RFC 7807) con código de regla y `traceId`.

---

## Integración continua y empaquetado

- **GitHub Actions** (`.github/workflows/ci.yml`) en cada *push*/PR a `main`:
  1. **Backend** — `restore` + `build` + `dotnet test` (incluye integración con Testcontainers).
  2. **Frontend** — `npm ci` + `build` + `ng test` (Vitest).
  3. **Smoke Docker** — construye las imágenes de API y Web para validar los `Dockerfile`.
- **Imágenes multi-stage**: la API publica sobre `aspnet:10.0` corriendo como usuario **no-root**; el
  frontend compila con Node y se sirve con **Nginx** (estáticos + proxy `/api`).
- El stack completo se valida de extremo a extremo con `docker compose up --build`
  (login, listado de eventos y SPA verificados a través del proxy de Nginx).

---

## Notas

- **Tema Metronic**: es comercial (ThemeForest); sus *assets* **no** se versionan. Solo se usa su CSS
  (el JS de Metronic se omite para evitar conflictos con las librerías de Angular).
- **Secretos**: nunca en el repositorio; se inyectan por variables de entorno / Azure Key Vault. La
  clave JWT y la cadena de conexión de `appsettings.json` son **placeholders de desarrollo**.
