FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY global.json ./
COPY apps/api/background-worker/iDA.Bu.Worker.csproj apps/api/background-worker/
RUN dotnet restore apps/api/background-worker/iDA.Bu.Worker.csproj
COPY apps/api/background-worker/ apps/api/background-worker/
RUN dotnet publish apps/api/background-worker/iDA.Bu.Worker.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app
COPY --from=build --chown=app:app /app/publish .
USER app
ENTRYPOINT ["dotnet", "iDA.Bu.Worker.dll"]
