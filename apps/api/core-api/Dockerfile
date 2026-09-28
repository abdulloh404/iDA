FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY global.json ./
COPY apps/api/core-api/iDA.Core.Api.csproj apps/api/core-api/
RUN dotnet restore apps/api/core-api/iDA.Core.Api.csproj
COPY apps/api/core-api/ apps/api/core-api/
RUN dotnet publish apps/api/core-api/iDA.Core.Api.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=3100
COPY --from=build --chown=app:app /app/publish .
USER app
EXPOSE 3100
ENTRYPOINT ["dotnet", "iDA.Core.Api.dll"]
