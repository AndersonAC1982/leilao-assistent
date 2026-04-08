FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/LeilaoAuto.sln backend/
COPY backend/LeilaoAuto.Workers/LeilaoAuto.Workers.csproj backend/LeilaoAuto.Workers/
COPY backend/LeilaoAuto.Application/LeilaoAuto.Application.csproj backend/LeilaoAuto.Application/
COPY backend/LeilaoAuto.Domain/LeilaoAuto.Domain.csproj backend/LeilaoAuto.Domain/
COPY backend/LeilaoAuto.Infrastructure/LeilaoAuto.Infrastructure.csproj backend/LeilaoAuto.Infrastructure/
RUN dotnet restore backend/LeilaoAuto.Workers/LeilaoAuto.Workers.csproj

COPY backend/LeilaoAuto.Workers/ backend/LeilaoAuto.Workers/
COPY backend/LeilaoAuto.Application/ backend/LeilaoAuto.Application/
COPY backend/LeilaoAuto.Domain/ backend/LeilaoAuto.Domain/
COPY backend/LeilaoAuto.Infrastructure/ backend/LeilaoAuto.Infrastructure/
RUN dotnet publish backend/LeilaoAuto.Workers/LeilaoAuto.Workers.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LeilaoAuto.Workers.dll"]
