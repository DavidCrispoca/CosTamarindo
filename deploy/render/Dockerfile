# Imagen de Render: nginx (sirve el build WebGL + proxy /api/) + uvicorn (API).
# Contexto de build = raíz del repo. Se usa junto a render.yaml.

FROM python:3.12-slim AS builder
ENV PIP_NO_CACHE_DIR=1 PIP_DISABLE_PIP_VERSION_CHECK=1
WORKDIR /build
COPY backend/requirements.txt .
RUN python -m venv /opt/venv \
    && /opt/venv/bin/pip install --no-cache-dir -r requirements.txt

FROM python:3.12-slim AS runtime
ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PATH="/opt/venv/bin:${PATH}"

RUN apt-get update \
    && apt-get install -y --no-install-recommends nginx curl gnupg \
    && rm -rf /var/lib/apt/lists/*

COPY --from=builder /opt/venv /opt/venv

WORKDIR /app
COPY backend/app ./app

# Build WebGL (commitado en deploy/render/webgl).
COPY deploy/render/webgl /webgl

# Nginx: config completa + snippets (cabeceras y CSP).
COPY deploy/render/nginx.conf /etc/nginx/nginx.conf
COPY infra/nginx/snippets/security-headers.conf /etc/nginx/snippets/security-headers.conf
COPY infra/nginx/snippets/csp.conf /etc/nginx/snippets/csp.conf

# Integridad del build (A.8.19): checksums firmados + verificador.
COPY deploy/render/verify-integrity.sh /verify-integrity.sh
COPY evidencias/A.8.19/SHA256SUMS /integrity/SHA256SUMS
COPY evidencias/A.8.19/SHA256SUMS.asc /integrity/SHA256SUMS.asc
COPY evidencias/A.8.19/pubkey.asc /integrity/pubkey.asc

COPY deploy/render/start.sh /start.sh
RUN chmod +x /start.sh /verify-integrity.sh

ENV PORT=10000
EXPOSE 10000

CMD ["/start.sh"]
