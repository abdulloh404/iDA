FROM node:24-alpine AS dependencies
WORKDIR /workspace
COPY package.json package-lock.json ./
RUN npm ci

FROM dependencies AS build
COPY . .
ARG VITE_API_URL=http://localhost:3100
ENV VITE_API_URL=$VITE_API_URL
ENV NX_DAEMON=false
RUN npm exec nx -- build web

FROM node:24-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production
ENV HOSTNAME=0.0.0.0
ENV PORT=3000
RUN addgroup --system --gid 1001 nodejs \
    && adduser --system --uid 1001 webapp
COPY package.json package-lock.json ./
RUN npm ci --omit=dev --ignore-scripts
COPY --from=build --chown=webapp:nodejs /workspace/dist/apps/web ./dist/apps/web
COPY --chown=webapp:nodejs tools/start-web.mjs ./tools/start-web.mjs
USER webapp
EXPOSE 3000
CMD ["node", "tools/start-web.mjs"]
