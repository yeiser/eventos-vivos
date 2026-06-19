# EventosVivos Web — build de Angular servido por Nginx.
# Contexto de build: ./frontend/eventos-vivos-web
# syntax=docker/dockerfile:1

# ---------- build ----------
FROM node:24-alpine AS build
WORKDIR /app

# Instalación reproducible (npm ci respeta .npmrc → legacy-peer-deps).
# --ignore-scripts evita ejecutar lifecycle scripts de dependencias (hardening).
COPY package.json package-lock.json .npmrc ./
RUN npm ci --ignore-scripts

# Código + build de producción (configuración por defecto = production).
COPY . .
RUN npm run build

# ---------- runtime ----------
# Imagen "unprivileged": corre como usuario no-root (uid 101) y escucha en 8080.
FROM nginxinc/nginx-unprivileged:1.27-alpine AS final
# nginx.conf es una plantilla: el entrypoint hace envsubst de ${API_UPSTREAM} al arrancar.
# Por defecto apunta al servicio "api" del compose; en ACI se sobreescribe a localhost:8080.
ENV API_UPSTREAM=api:8080
COPY nginx.conf /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist/eventos-vivos-web/browser /usr/share/nginx/html
EXPOSE 8080
HEALTHCHECK --interval=15s --timeout=3s --start-period=5s --retries=5 \
    CMD wget -qO- http://localhost:8080/ >/dev/null 2>&1 || exit 1
