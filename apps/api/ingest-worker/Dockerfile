FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY global.json ./
COPY apps/api/core-api/domain/Ida.Domain.csproj apps/api/core-api/domain/
COPY apps/api/core-api/application/Ida.Application.csproj apps/api/core-api/application/
COPY apps/api/infrastructure/Ida.Infrastructure.csproj apps/api/infrastructure/
COPY apps/api/ingest-worker/Ida.Worker.Ingest.csproj apps/api/ingest-worker/
RUN dotnet restore apps/api/ingest-worker/Ida.Worker.Ingest.csproj
COPY apps/api/core-api/domain/ apps/api/core-api/domain/
COPY apps/api/core-api/application/ apps/api/core-api/application/
COPY apps/api/infrastructure/ apps/api/infrastructure/
COPY apps/api/ingest-worker/ apps/api/ingest-worker/
RUN dotnet publish apps/api/ingest-worker/Ida.Worker.Ingest.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build --chown=app:app /app/publish .
USER app
ENTRYPOINT ["dotnet", "Ida.Worker.Ingest.dll", "serve", "--hospital=all"]
