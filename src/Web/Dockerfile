# syntax=docker/dockerfile:1
# Debian (glibc): `npm ci` falha em Alpine quando o lockfile/fixtures exigem
# `@rolldown/binding-linux-x64-gnu` (ELF gnu), incompatível com musl do Alpine.
FROM node:22-bookworm-slim AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
# Produção: SPA chama `/api/...` relativamente (mesma origem atrás do Nginx)
RUN npm run build

FROM nginx:1.28-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
