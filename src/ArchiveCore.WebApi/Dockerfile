# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first so the layer is cached while only source files change.
COPY ArchiveCore.slnx ./
COPY src/ArchiveCore.Domain/ArchiveCore.Domain.csproj src/ArchiveCore.Domain/
COPY src/ArchiveCore.Application/ArchiveCore.Application.csproj src/ArchiveCore.Application/
COPY src/ArchiveCore.Infrastructure/ArchiveCore.Infrastructure.csproj src/ArchiveCore.Infrastructure/
COPY src/ArchiveCore.WebApi/ArchiveCore.WebApi.csproj src/ArchiveCore.WebApi/
RUN dotnet restore src/ArchiveCore.WebApi/ArchiveCore.WebApi.csproj

COPY src/ src/
RUN dotnet publish src/ArchiveCore.WebApi/ArchiveCore.WebApi.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Document storage lives on a named volume; the directory must belong to the non-root user
# so the volume is initialized with writable ownership.
RUN mkdir -p /app/storage/documents && chown -R "$APP_UID:$APP_UID" /app/storage

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_NOLOGO=1
EXPOSE 8080
USER $APP_UID

ENTRYPOINT ["dotnet", "ArchiveCore.WebApi.dll"]
