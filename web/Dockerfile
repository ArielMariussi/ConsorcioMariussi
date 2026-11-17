# Estágio 1: compila e publica o Blazor WebAssembly
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ConsorciosMariussi.Web/ConsorciosMariussi.Web.csproj ConsorciosMariussi.Web/
RUN dotnet restore ConsorciosMariussi.Web/ConsorciosMariussi.Web.csproj

COPY . .
RUN dotnet publish ConsorciosMariussi.Web/ConsorciosMariussi.Web.csproj -c Release -o /app/publish --no-restore

# Estágio 2: serve os arquivos estáticos com nginx
FROM nginx:alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html

# Gera o appsettings.json a partir da variável API_BASE_URL, se ela existir.
# O sed remove quebras de linha do Windows (CRLF), que impediriam o script de rodar.
COPY docker-entrypoint.d/40-appsettings.sh /docker-entrypoint.d/40-appsettings.sh
RUN sed -i 's/\r$//' /docker-entrypoint.d/40-appsettings.sh \
    && chmod +x /docker-entrypoint.d/40-appsettings.sh

EXPOSE 80
