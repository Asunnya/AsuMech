FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG VERSION=0.0.0
WORKDIR /src

COPY AsuMech.Relay/AsuMech.Relay.csproj AsuMech.Relay/
COPY AsuMech.Relay.Host/AsuMech.Relay.Host.csproj AsuMech.Relay.Host/
RUN dotnet restore AsuMech.Relay.Host/AsuMech.Relay.Host.csproj

COPY AsuMech.Relay/ AsuMech.Relay/
COPY AsuMech.Relay.Host/ AsuMech.Relay.Host/
RUN dotnet publish AsuMech.Relay.Host/AsuMech.Relay.Host.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    -p:UseAppHost=false \
    -p:Version=${VERSION}

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final

WORKDIR /app
COPY --from=build --chown=app:app /app/publish .

USER app
EXPOSE 7890

ENTRYPOINT ["dotnet", "AsuMech.Relay.Host.dll"]
