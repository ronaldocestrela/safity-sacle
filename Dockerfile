FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /build

# TargetFramework vem de Directory.Build.props (net10.0)
COPY Directory.Build.props ./

# Layer cache-friendly restore
COPY src/Domain/SafetyScale.Domain.csproj ./src/Domain/
COPY src/Application/SafetyScale.Application.csproj ./src/Application/
COPY src/Infrastructure/SafetyScale.Infrastructure.csproj ./src/Infrastructure/
COPY src/Api/SafetyScale.Api.csproj ./src/Api/

RUN dotnet restore src/Api/SafetyScale.Api.csproj

COPY src/ ./src/
RUN dotnet publish src/Api/SafetyScale.Api.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "SafetyScale.Api.dll"]
