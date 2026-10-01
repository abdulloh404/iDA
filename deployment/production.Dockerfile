FROM mcr.microsoft.com/dotnet/sdk:9.0.315 AS dotnet-build
WORKDIR /src
COPY global.json ./
COPY apps/api/ ./apps/api/

FROM dotnet-build AS core-publish
RUN dotnet publish apps/api/iDA.Core-api/api/Ida.Api.csproj --configuration Release --output /out --no-self-contained -p:UseAppHost=false

FROM dotnet-build AS tenant-publish
RUN dotnet publish apps/api/iDA.Tanent-api/Ida.Tenant.Api.csproj --configuration Release --output /out --no-self-contained -p:UseAppHost=false

FROM dotnet-build AS ingest-publish
RUN dotnet publish apps/api/IDA.Ingest-worker/Ida.Worker.Ingest.csproj --configuration Release --output /out --no-self-contained -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS dotnet-runtime
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_ENVIRONMENT=Production
USER $APP_UID

FROM dotnet-runtime AS core-api
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=core-publish --chown=app:app /out/ ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "iDA.Core.Api.dll"]

FROM dotnet-runtime AS tenant-api
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=tenant-publish --chown=app:app /out/ ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "iDA.Tenant.Api.dll"]

FROM dotnet-runtime AS ingest
COPY --from=ingest-publish --chown=app:app /out/ ./
ENTRYPOINT ["dotnet", "Ida.Worker.Ingest.dll"]
CMD ["serve"]

FROM node:24-bookworm-slim AS web-build
WORKDIR /src
ENV NX_DAEMON=false \
    NX_ISOLATE_PLUGINS=false
COPY package.json package-lock.json ./
RUN npm ci
COPY global.json nx.json tsconfig.base.json tsconfig.json ./
COPY apps/web/ ./apps/web/
RUN npm run build:web

FROM nginx:stable-alpine AS web
COPY deployment/web.conf /etc/nginx/conf.d/default.conf
COPY --from=web-build /src/dist/apps/web/ /usr/share/nginx/html/
EXPOSE 8080
