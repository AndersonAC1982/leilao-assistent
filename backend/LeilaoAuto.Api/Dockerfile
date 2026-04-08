FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/LeilaoAuto.sln backend/
COPY backend/LeilaoAuto.Api/LeilaoAuto.Api.csproj backend/LeilaoAuto.Api/
COPY backend/LeilaoAuto.Application/LeilaoAuto.Application.csproj backend/LeilaoAuto.Application/
COPY backend/LeilaoAuto.Domain/LeilaoAuto.Domain.csproj backend/LeilaoAuto.Domain/
COPY backend/LeilaoAuto.Infrastructure/LeilaoAuto.Infrastructure.csproj backend/LeilaoAuto.Infrastructure/
RUN dotnet restore backend/LeilaoAuto.Api/LeilaoAuto.Api.csproj

COPY backend/LeilaoAuto.Api/ backend/LeilaoAuto.Api/
COPY backend/LeilaoAuto.Application/ backend/LeilaoAuto.Application/
COPY backend/LeilaoAuto.Domain/ backend/LeilaoAuto.Domain/
COPY backend/LeilaoAuto.Infrastructure/ backend/LeilaoAuto.Infrastructure/
RUN dotnet publish backend/LeilaoAuto.Api/LeilaoAuto.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "LeilaoAuto.Api.dll"]
