FROM gcc:14-bookworm AS rules
WORKDIR /src
COPY game .
RUN g++ -std=c++17 -O2 core/pier.cpp tools/balance.cpp -o /balance

FROM python:3.12-slim-bookworm
WORKDIR /studio
COPY platform ./platform
RUN pip install --no-cache-dir uv==0.13.6 && uv sync --project platform --locked --no-dev
COPY game/agent_review.py game/layout_review.py game/layout-metrics.json ./game/
COPY --from=rules /balance /studio/oracle
ENV ATLAS_ORACLE=/studio/oracle PYTHONDONTWRITEBYTECODE=1
USER 65534:65534
CMD ["/studio/platform/.venv/bin/uvicorn","gateway:app","--app-dir","/studio/platform","--host","0.0.0.0","--port","8080"]
