#!/bin/sh
# Roda quando o container do nginx inicia.
# Se a variável API_BASE_URL existir (ex.: no Render), reescreve o appsettings.json
# com o endereço da API de produção. Sem a variável, mantém o padrão (http://localhost:5080).
set -e

if [ -n "$API_BASE_URL" ]; then
  printf '{\n  "ApiBaseUrl": "%s"\n}\n' "$API_BASE_URL" > /usr/share/nginx/html/appsettings.json
  echo "appsettings.json: ApiBaseUrl=$API_BASE_URL"
fi
