FROM mcr.microsoft.com/dotnet/sdk:9.0 AS source
WORKDIR /src
COPY iDA.Common/ iDA.Common/
COPY iDA.Core/ iDA.Core/
COPY iDA.Tanent/ iDA.Tanent/
COPY IDA.Ingest-worker/ IDA.Ingest-worker/

FROM source AS core-publish
RUN dotnet publish iDA.Core/api/Ida.Api.csproj --configuration Release --output /publish/core -p:UseAppHost=false

FROM source AS tenant-publish
RUN dotnet publish iDA.Tanent/api/Ida.Tenant.Api.csproj --configuration Release --output /publish/tenant -p:UseAppHost=false

FROM source AS ingest-publish
RUN dotnet publish IDA.Ingest-worker/Ida.Worker.Ingest.csproj --configuration Release --output /publish/ingest -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
USER $APP_UID

FROM runtime AS core
COPY --from=core-publish /publish/core/ .
EXPOSE 3100
ENTRYPOINT ["dotnet", "iDA.Core.Api.dll"]

FROM runtime AS tenant
COPY --from=tenant-publish /publish/tenant/ .
EXPOSE 3100
ENTRYPOINT ["dotnet", "iDA.Tenant.Api.dll"]

FROM runtime AS ingest
COPY --from=ingest-publish /publish/ingest/ .
ENTRYPOINT ["dotnet", "Ida.Worker.Ingest.dll"]
CMD ["serve"]
