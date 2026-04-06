FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY LeilaoAuto.sln ./
COPY LeilaoAuto.Api/LeilaoAuto.Api.csproj LeilaoAuto.Api/
COPY LeilaoAuto.Application/LeilaoAuto.Application.csproj LeilaoAuto.Application/
COPY LeilaoAuto.Domain/LeilaoAuto.Domain.csproj LeilaoAuto.Domain/
COPY LeilaoAuto.Infrastructure/LeilaoAuto.Infrastructure.csproj LeilaoAuto.Infrastructure/
RUN dotnet restore LeilaoAuto.Api/LeilaoAuto.Api.csproj

COPY LeilaoAuto.Api/ LeilaoAuto.Api/
COPY LeilaoAuto.Application/ LeilaoAuto.Application/
COPY LeilaoAuto.Domain/ LeilaoAuto.Domain/
COPY LeilaoAuto.Infrastructure/ LeilaoAuto.Infrastructure/
RUN dotnet publish LeilaoAuto.Api/LeilaoAuto.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "LeilaoAuto.Api.dll"]
