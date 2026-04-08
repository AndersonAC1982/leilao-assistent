FROM node:22-alpine AS build
WORKDIR /app

COPY apps/web/package*.json ./
RUN npm ci

COPY apps/web/ ./
RUN npm run build

FROM nginx:1.27-alpine AS final
COPY apps/web/nginx.conf /etc/nginx/conf.d/default.conf
# Angular build output for application builder.
COPY --from=build /app/dist/leilaoauto-web/browser /usr/share/nginx/html
EXPOSE 80
