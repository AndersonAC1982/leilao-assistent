FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY LeilaoAuto.sln ./
COPY LeilaoAuto.Workers/LeilaoAuto.Workers.csproj LeilaoAuto.Workers/
COPY LeilaoAuto.Application/LeilaoAuto.Application.csproj LeilaoAuto.Application/
COPY LeilaoAuto.Domain/LeilaoAuto.Domain.csproj LeilaoAuto.Domain/
COPY LeilaoAuto.Infrastructure/LeilaoAuto.Infrastructure.csproj LeilaoAuto.Infrastructure/
RUN dotnet restore LeilaoAuto.Workers/LeilaoAuto.Workers.csproj

COPY LeilaoAuto.Workers/ LeilaoAuto.Workers/
COPY LeilaoAuto.Application/ LeilaoAuto.Application/
COPY LeilaoAuto.Domain/ LeilaoAuto.Domain/
COPY LeilaoAuto.Infrastructure/ LeilaoAuto.Infrastructure/
RUN dotnet publish LeilaoAuto.Workers/LeilaoAuto.Workers.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LeilaoAuto.Workers.dll"]
