FROM debian:bookworm-slim AS csda
ARG CSDA_VERSION=1.10.5
ARG CSDA_SHA256=5c972e016b90e08ae235e2ca8092423a1900868006a6cf0ceecd0f36f1be9cbe
RUN apt-get update \
    && apt-get install --yes --no-install-recommends ca-certificates curl unzip \
    && curl --fail --location --output /tmp/csda.zip \
      "https://github.com/akiver/cs-demo-analyzer/releases/download/v${CSDA_VERSION}/linux-x64.zip" \
    && echo "${CSDA_SHA256}  /tmp/csda.zip" | sha256sum --check --strict \
    && unzip /tmp/csda.zip -d /tmp/csda \
    && install -m 0755 /tmp/csda/csda /usr/local/bin/csda \
    && rm -rf /var/lib/apt/lists/* /tmp/csda /tmp/csda.zip

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore CommunityIntranet.sln
RUN dotnet publish backend/CommunityIntranet.Api/CommunityIntranet.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl ffmpeg \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/logs /app/data-protection-keys /app/party-media /app/counter-strike /app/licenses \
    && chown -R app:app /app
COPY --from=build --chown=app:app /app/publish .
COPY --from=csda /usr/local/bin/csda /usr/local/bin/csda
COPY --chown=app:app third-party/cs-demo-analyzer-LICENSE.md /app/licenses/cs-demo-analyzer-LICENSE.md
USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
HEALTHCHECK --interval=10s --timeout=5s --start-period=30s --retries=10 \
    CMD curl --fail --silent http://127.0.0.1:8080/api/health > /dev/null || exit 1
ENTRYPOINT ["dotnet", "CommunityIntranet.Api.dll"]
