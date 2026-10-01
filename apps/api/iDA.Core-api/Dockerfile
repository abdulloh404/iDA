FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY global.json ./
COPY apps/api/core-api/api/Ida.Api.csproj apps/api/core-api/api/
COPY apps/api/core-api/application/Ida.Application.csproj apps/api/core-api/application/
COPY apps/api/core-api/domain/Ida.Domain.csproj apps/api/core-api/domain/
COPY apps/api/infrastructure/Ida.Infrastructure.csproj apps/api/infrastructure/
RUN dotnet restore apps/api/core-api/api/Ida.Api.csproj
COPY apps/api/core-api/ apps/api/core-api/
COPY apps/api/infrastructure/ apps/api/infrastructure/
COPY apps/api/ingest-worker/Fixtures/ apps/api/ingest-worker/Fixtures/
COPY apps/api/ingest-worker/Sql/ apps/api/ingest-worker/Sql/
RUN dotnet publish apps/api/core-api/api/Ida.Api.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=3100
COPY --from=build --chown=app:app /app/publish .
USER app
EXPOSE 3100
ENTRYPOINT ["dotnet", "iDA.Core.Api.dll"]
