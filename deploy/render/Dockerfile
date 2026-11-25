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
    && apt-get install -y --no-install-recommends nginx curl \
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

COPY deploy/render/start.sh /start.sh
RUN chmod +x /start.sh

ENV PORT=10000
EXPOSE 10000

CMD ["/start.sh"]
