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
EXPOSE 80
