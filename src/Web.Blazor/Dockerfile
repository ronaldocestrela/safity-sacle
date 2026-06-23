# Blazor WASM standalone — publish + Nginx (B10.2)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/Web.Blazor/SafetyScale.Web.Blazor.csproj src/Web.Blazor/
RUN dotnet restore src/Web.Blazor/SafetyScale.Web.Blazor.csproj

COPY src/Web.Blazor/ src/Web.Blazor/
WORKDIR /src/src/Web.Blazor

# Same-origin prod: ApiBaseUrl vazio (proxy /api no Nginx).
# Split-origin: passar build-arg API_BASE_URL (ex.: https://api.exemplo.com).
ARG API_BASE_URL=
RUN dotnet publish SafetyScale.Web.Blazor.csproj -c Release -o /app/publish \
    && if [ -n "$API_BASE_URL" ]; then \
         printf '{"ApiBaseUrl":"%s"}\n' "$API_BASE_URL" > /app/publish/wwwroot/appsettings.json; \
       fi

FROM nginx:1.28-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

EXPOSE 80
