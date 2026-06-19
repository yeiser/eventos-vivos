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
| Calidad | **SonarQube Cloud** (análisis estático + cobertura: OpenCover / lcov) |
| Infra | **Terraform** · **Azure Container Instances** (ambiente de demostración mínimo) |

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

La **regla de dependencia** apunta siempre hacia adentro: el `Domain` no referencia a nada (ni EF, ni
ASP.NET); `Application` define **puertos** (interfaces: `IEventoRepository`, `IClock`,
`IPasswordHasher`…) que `Infrastructure` implementa; `Api` solo orquesta y compone por inyección de
dependencias. Consecuencia práctica: las reglas de negocio se prueban **sin** base de datos ni servidor
web, y la infraestructura (PostgreSQL, JWT, EF) queda como un detalle reemplazable.

**¿Por qué Clean Architecture y no un CRUD por capas?** El núcleo del problema no es *persistir datos*,
son las **reglas** (anti-sobreventa, solapamiento de agendas, ventanas horarias, penalizaciones) y su
**concurrencia**. Aislar esas reglas en un dominio puro las hace explícitas, testeables y a prueba de
cambios de framework. El costo —más proyectos y algo de ceremonia— se justifica porque la lógica supera
con creces al CRUD; para un CRUD trivial habría sido sobre-ingeniería.

### Decisiones de diseño (problema → decisión → por qué)

**Anti-sobreventa (RN01) — el riesgo nº 1 del negocio.** Dos reservas simultáneas no deben superar el
aforo. *Decisión:* el caso de uso abre una **transacción** y toma un **bloqueo pesimista**
(`SELECT … FOR UPDATE` sobre la fila del evento) antes de contar el cupo y escribir, de modo que las
reservas concurrentes sobre el mismo evento se **serializan**; como segunda barrera, el `Evento` lleva
el token de concurrencia **`xmin`** de PostgreSQL. *Por qué pesimista y no solo optimista:* con
optimista puro, bajo contención todas las transacciones leen el mismo cupo, una gana y el resto debe
reintentar (tormenta de reintentos); el bloqueo evita ese trabajo desperdiciado en el punto más caliente.
*Por qué no un lock en memoria de la app:* no sobrevive a múltiples instancias — la verdad de
concurrencia debe vivir en la base de datos. Hay una **prueba de integración** que lanza N reservas en
paralelo (Postgres real vía Testcontainers) y verifica `Σ ocupadas ≤ capacidad`.

**Modelo de dominio rico, no anémico.** Las invariantes (RN01–RN07) viven dentro de `Evento`/`Reserva`
como métodos que solo permiten transiciones válidas, no en *services* que manipulan propiedades públicas.
*Por qué:* evita que una regla se duplique o se salte según quién llame; el objeto siempre está en un
estado válido.

**Value Objects (`Email`, `CodigoReserva`).** Encapsulan validación y formato (un `Email` inválido no
puede existir; el código es de 6 dígitos) y se persisten con *value converters* de EF. *Por qué:* hace
**imposible representar estados ilegales** y centraliza el formato en un solo lugar.

**CQRS ligero, sin MediatR.** Cada caso de uso es un *handler* explícito (comando/consulta) inyectado por
DI; las consultas proyectan a DTOs sin pasar por el dominio. *Por qué sin MediatR:* para este tamaño
añade una dependencia y una indirección por reflexión que no aporta — los handlers explícitos se leen y
depuran mejor.

**Tiempo inyectable (`IClock`).** Las reglas dependientes de la hora (RN03 noche/fin de semana, RN04
< 1 h, RN06 auto-completado, RN07 ventana de cancelación) toman el "ahora" de un `IClock`. *Por qué:* las
vuelve **deterministas** — en pruebas se inyecta un reloj fijo y se cubren los bordes sin esperas ni
*flakiness*.

**Errores como contrato (RFC 7807 / ProblemDetails).** Toda respuesta de error es
`application/problem+json` con código de regla (`RN0x`), `title`, `status` y `traceId`. Distinción
intencional: **400** (entrada mal formada, la valida FluentValidation), **401/403** (auth), **409/422**
(estado o regla de negocio violada). *Por qué:* un contrato de error uniforme y legible por máquina deja
que el frontend reaccione por **código**, no parseando textos.

**Auditoría como *cross-cutting concern* (dos interceptores de EF Core).** Uno rellena la **trazabilidad**
de cada entidad (creado/modificado por quién y cuándo); otro escribe un **registro inmutable** (valores
antes/después, con *masking* de datos sensibles). *Por qué interceptores y no código en cada handler:* la
auditoría no debe contaminar la lógica ni poder "olvidarse" — al colgarse de `SaveChanges` se aplica de
forma transversal y consistente.

### Por qué cada tecnología

| Elección | Por qué (preciso) | Alternativa descartada |
|---|---|---|
| **.NET 10 / ASP.NET Core** | LTS, alto rendimiento, DI y *hosting* nativos; es la plataforma del puesto | — |
| **PostgreSQL + Npgsql** | `SELECT … FOR UPDATE` y `xmin` para la concurrencia; `ILIKE` nativo para la búsqueda por título (RF-02); open-source | SQL Server (no se necesitan features exclusivas ni licencia) |
| **EF Core 10** | Migraciones versionadas, *value converters* para los VO e **interceptores** para la auditoría | Dapper (más control SQL, pero perderíamos migraciones/VO/interceptores) |
| **EFCore.NamingConventions** | Columnas `snake_case` idiomáticas en Postgres sin anotar cada propiedad | Mapear a mano (ruidoso y propenso a error) |
| **FluentValidation** | Separa la validación de **entrada** (forma del DTO) de las **invariantes de dominio**; reglas declarativas | *Data annotations* (menos expresivas para reglas compuestas) |
| **JWT Bearer + roles** | Autenticación **sin estado** → escala horizontal sin sesión compartida; encaja con SPA + API | Cookies de sesión (requieren almacén de sesión y afinidad) |
| **PBKDF2 (SHA-256, 100k iter.)** | Derivación de clave robusta **incluida en la BCL**, sin dependencias; comparación en tiempo constante | bcrypt/Argon2 (más fuertes, pero añaden paquete; PBKDF2 cumple aquí) |
| **Serilog** | Logging **estructurado** (consultable) y enriquecido por petición | `ILogger` plano (menos estructura) |
| **Angular 22 (zoneless + signals + standalone)** | "Última versión" pedida; *signals* dan reactividad fina y *zoneless* elimina el costo de Zone.js; *standalone* quita NgModules | Angular con Zone.js/NgModules (más sobrecarga y *boilerplate*) |
| **Tema Metronic (solo CSS)** | Requisito de *look & feel*; se usa su CSS y se reescribe el *shell* en componentes propios | Cargar su JS (choca con ApexCharts/jQuery de Angular) |
| **Nginx (reverse proxy)** | Sirve la SPA y *proxya* `/api` → **mismo origen, sin CORS**; imagen pequeña | Servir API y front por separado (obliga a gestionar CORS) |
| **Vitest** | Runner por defecto del nuevo *builder* de Angular; arranque rápido (esbuild) | Karma/Jasmine (en retirada, más lentos) |
| **Testcontainers (Postgres real)** | Las pruebas de concurrencia y SQL (`FOR UPDATE`, `ILIKE`) corren contra **Postgres de verdad**, no un *in-memory* que mentiría | EF InMemory/SQLite (no reproducen el bloqueo ni el dialecto) |
| **Docker multi-stage + Compose** | Imágenes reproducibles y pequeñas (runtime sin SDK); un comando levanta todo | — |
| **Terraform + Azure ACI** | IaC idempotente con la **mínima** huella para un ambiente de solo demostración | VM/AKS/PaaS completo (sobredimensionado para una demo) |
| **SonarQube Cloud** | *Quality gate* + cobertura en cada push; gratis en repos públicos | Servidor self-hosted (más que mantener para esto) |

### Seguridad (defensa en capas)

- **JWT Bearer + roles** (Admin/Usuario) con política de *fallback*: **todo** requiere autenticación
  salvo `login`. Admin: crear evento, confirmar pago, reporte, auditoría y gestión de reservas.
- **Remediación de fuerza bruta en dos capas:** (1) *rate limiting* por IP sobre `login` y (2) **bloqueo
  de cuenta** tras 5 intentos fallidos durante 15 min (→ HTTP **423**). El lockout se modela en el dominio
  (no es un detalle de infraestructura), así que es testeable y consistente.
- **Superficie de entrada controlada:** DTOs explícitos (*whitelisting*, sin *over-posting*), CORS
  restringido y **sin secretos en el repositorio** (los genera Terraform o se inyectan por variable de
  entorno). La clave JWT y la cadena de conexión de `appsettings.json` son *placeholders* de desarrollo.

---

## Reglas de negocio y ambigüedades resueltas

Las decisiones tomadas:

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

## Calidad de código (SonarCloud)

El análisis estático y la cobertura se publican en **SonarQube Cloud** desde GitHub Actions
([`.github/workflows/sonarcloud.yml`](.github/workflows/sonarcloud.yml)) en cada *push*/PR:

---

## Despliegue en Azure (ambiente de demostración)

Como es un entorno **solo para mostrar**, la infraestructura es la **mínima**: un único
**grupo de contenedores de Azure Container Instances (ACI)** con tres contenedores
(PostgreSQL + API + frontend) que comparten red y se comunican por `localhost`. El frontend
(Nginx) es el único expuesto a Internet y hace de proxy de la API → **mismo origen, sin CORS**.

```
Azure Resource Group (eventosvivos-rg)
 ├─ Container Registry (Basic)         # imágenes privadas: api, web, postgres
 └─ Container Group (ACI, IP pública)  # http://<dns-label>.<region>.azurecontainer.io:8080
     ├─ db   (postgres:16-alpine)      # efímero (localhost:5432); los datos se regeneran con el seed
     ├─ api  (.NET 10, migra + siembra; escucha en localhost:5000)
     └─ web  (Nginx no-root: estáticos + proxy /api → localhost:5000)   ◄── único puerto público (8080)
```

> Los tres contenedores comparten la red del grupo, así que escuchan en puertos **distintos**
> (db 5432, api 5000, web 8080). El frontend usa la imagen `nginx-unprivileged` (corre como
> usuario no-root) y por eso el puerto público es **8080**.

**Sin** VM, **sin** base de datos gestionada, **sin** Key Vault: los secretos (contraseña de
PostgreSQL y clave JWT) se generan aleatoriamente con Terraform y se inyectan como variables
seguras del contenedor. Recursos totales: **3** (Resource Group + ACR + Container Group).

### Infraestructura sugerida para producción

La demo prioriza simplicidad y costo; un ambiente **productivo real** cambiaría ese compromiso por
**alta disponibilidad, seguridad perimetral, secretos gestionados, observabilidad y autoescala**.
Arquitectura recomendada en Azure:

```
                          Internet  (dominio propio + HTTPS)
                              │
                   ┌──────────▼───────────┐
                   │ Azure Front Door + WAF│  TLS · CDN · WAF (OWASP) · enrutado
                   └─────┬───────────┬─────┘
                  /      │           │  /api
          ┌──────────────▼──┐   ┌────▼─────────────────────────┐
          │ Static Web Apps │   │ Azure Container Apps (API)    │  autoescala (KEDA)
          │  (SPA Angular)  │   │  .NET 10 · sin estado         │  revisiones/blue-green
          └─────────────────┘   └────┬───────────┬─────────┬────┘
                                     │ Managed Identity     │
                       ┌─────────────▼───┐ ┌───────▼──────┐ ┌▼───────────────────────┐
                       │ Key Vault        │ │ ACR (priv.)  │ │ PostgreSQL Flexible     │
                       │ (JWT, conn. str.)│ │ imágenes     │ │ Server (HA zonal, PITR) │
                       └──────────────────┘ └──────────────┘ └─────────────────────────┘
                       └──────── VNet + Private Endpoints (DB y Key Vault sin IP pública) ───────┘

   Observabilidad: Application Insights + Log Analytics (Azure Monitor)
   CI/CD: GitHub Actions con OIDC → Azure · estado de Terraform en Azure Storage (con locking)
```

| Necesidad | Servicio Azure | Por qué |
|---|---|---|
| **Cómputo de la API** | **Azure Container Apps** | Serverless con **autoescala** (KEDA) por HTTP/CPU, revisiones (blue-green), ingress con TLS; sin administrar nodos. *AKS* solo si se necesita orquestación avanzada |
| **Hosting del frontend** | **Azure Static Web Apps** (o Blob *static website* + CDN) | Distribución en el borde, barata y escalable para estáticos; CI integrado |
| **Base de datos** | **Azure Database for PostgreSQL Flexible Server** | Gestionado, **HA zona-redundante**, backups automáticos + *point-in-time restore*. Es el mismo Postgres → conserva `FOR UPDATE`/`xmin` sin tocar el código |
| **Registro de imágenes** | **Azure Container Registry** (Standard/Premium) | Privado, geo-replicación; *pull* con **Managed Identity** (sin credenciales) |
| **Secretos** | **Azure Key Vault** + **Managed Identity** | Clave JWT y cadena de conexión fuera del entorno, con rotación; la app accede con identidad administrada → **cero secretos** en config/CI |
| **Borde y seguridad perimetral** | **Azure Front Door (Premium) + WAF** | TLS, CDN, enrutado global y **WAF (OWASP)** que complementa el *rate-limit*/lockout de la app |
| **Red privada** | **VNet + Private Endpoints + NSG** | PostgreSQL y Key Vault **sin exposición pública**; el tráfico no sale de la VNet |
| **Observabilidad** | **Application Insights + Log Analytics** (Azure Monitor) | Trazas distribuidas (se correlacionan con el `traceId` de ProblemDetails), métricas y alertas; los logs estructurados de Serilog fluyen aquí |
| **CI/CD** | **GitHub Actions con OIDC → Azure** | Despliegue federado **sin credenciales almacenadas**: build+push a ACR y *release* a Container Apps |
| **Estado de Terraform** | **Azure Storage** (backend remoto con *locking*) | Estado compartido y bloqueado para trabajo en equipo (vs. el estado local de la demo) |
| **DNS y certificados** | **Azure DNS + certificados gestionados** | Dominio propio con HTTPS automático |

> **Lo que NO cambia es el código:** como la **concurrencia vive en la base de datos** (transacción +
> `FOR UPDATE` + `xmin`) y la **API es sin estado** (JWT), se puede **escalar horizontalmente** sin tocar
> el dominio. Lo que cambia es el *entorno*: servicios gestionados, HA, secretos en Key Vault con Managed
> Identity, red privada, WAF y observabilidad. La migración desde la demo es sustituir recursos en
> Terraform (ACI → Container Apps + PostgreSQL Flexible + Key Vault…), no reescribir la aplicación.

---

## Notas

- **Tema Metronic**: es comercial (ThemeForest); sus *assets* **no** se versionan. Solo se usa su CSS
  (el JS de Metronic se omite para evitar conflictos con las librerías de Angular).
- **Secretos**: nunca en el repositorio; se inyectan por variables de entorno / Azure Key Vault. La
  clave JWT y la cadena de conexión de `appsettings.json` son **placeholders de desarrollo**.
