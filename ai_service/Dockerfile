# syntax=docker/dockerfile:1
#
# Python AI microservice (FastAPI + LangGraph).
#
# Internal-only: the .NET API calls this at http://ai:8000/api/v1/... (see
# AiServiceSettings__BaseUrl). It is never exposed to the internet, so it gets no
# published port in docker-compose.yml.
#
# Python 3.12 matches the version the CI pipeline tests against (.github/workflows/ci.yml).

FROM python:3.12-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PIP_NO_CACHE_DIR=1 \
    PIP_DISABLE_PIP_VERSION_CHECK=1

WORKDIR /app

# requirements.txt first: the dependency layer is only rebuilt when it changes.
COPY requirements.txt ./
RUN pip install --no-cache-dir -r requirements.txt

# main.py imports `core.*` and `agent_N_*.*` as top-level packages, so the working
# directory must be the ai_service directory itself.
COPY . .

# Run as an unprivileged user. The service only reads its own files and calls out
# to the LLM provider, so it needs no write access anywhere.
RUN useradd --create-home --uid 10001 appuser \
 && chown -R appuser:appuser /app
USER appuser

EXPOSE 8000

# /health is defined in main.py and touches no database or LLM, so it is a cheap
# liveness probe.
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD python -c "import sys,urllib.request; sys.exit(0 if urllib.request.urlopen('http://127.0.0.1:8000/health', timeout=4).status == 200 else 1)"

# No --reload in production: the dev entrypoint in main.py enables it, but that
# would spawn a file watcher and a second process for no benefit here.
CMD ["uvicorn", "main:app", "--host", "0.0.0.0", "--port", "8000", "--proxy-headers"]
