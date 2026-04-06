FROM node:22-alpine AS build
WORKDIR /app

COPY leilaoauto-web/package*.json ./
RUN npm ci

COPY leilaoauto-web/ ./
RUN npm run build

FROM nginx:1.27-alpine AS final
COPY leilaoauto-web/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/leilaoauto-web/browser /usr/share/nginx/html
EXPOSE 80
