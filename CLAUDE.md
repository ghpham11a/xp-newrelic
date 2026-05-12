# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Repo Is

Experimental repo for integrating New Relic observability across multiple backend services deployed to Kubernetes. Two equivalent CRUD APIs (Spring Boot + .NET) demonstrate APM auto-instrumentation, custom metrics, request/response body capture with PII sanitization, and Kustomize-based multi-environment deployments.

## Build & Test Commands

### Spring Boot (`springboot/`)
```bash
./mvnw clean package          # build
./mvnw test                   # run tests
./mvnw spring-boot:run        # run locally (default profile)
./mvnw spring-boot:run -Dspring-boot.run.profiles=dev  # run with dev profile (enables Micrometer)
```

### .NET (`dotnet/`)
```bash
dotnet build                  # build
dotnet run                    # run locally
dotnet publish -c Release -o out  # publish for container
```

### Deploy to Local Kubernetes (Docker Desktop)
```bash
cd springboot && deploy.bat   # build image, apply k8s, port-forward to :8080, test API
cd dotnet && deploy.bat       # build image, apply k8s, port-forward to :5000, test API
```

### Teardown
```bash
cd springboot && teardown.bat
cd dotnet && teardown.bat
```

## Architecture

Both projects implement the same in-memory CRUD API (`/api/items`) with identical New Relic observability features. The operator injects agents at pod startup — no agent JARs or SDKs are bundled in the container images.

### New Relic Integration Layers

1. **APM Agent** — Injected automatically by the k8s-agents-operator via pod annotations (`instrumentation.newrelic.com/inject-java` / `inject-dotnet`). The `Instrumentation` CR lives in the operator's namespace (`default`), not the app namespace — this is a hard requirement enforced by the operator's webhook.

2. **Body Capture** — Servlet filter (Java) / ASP.NET middleware (.NET) buffers request/response bodies, runs them through `BodySanitizer` to strip PII/secrets, then attaches sanitized strings as `request.body` / `response.body` custom attributes on the New Relic transaction. Max 4096 chars (New Relic attribute limit).

3. **Custom Metrics** — Business metrics (item counts, operation counters, name length distributions, payload sizes). Spring Boot uses Micrometer with the New Relic registry; .NET uses `System.Diagnostics.Metrics` which the agent auto-discovers.

### Kubernetes Structure

Both projects use Kustomize with `base/` + `overlays/{dev,staging,prod}`. The `operator-instrumentation.yml` is applied separately (not via Kustomize) because it must stay in the operator's namespace while Kustomize overlays set namespace on all resources.

The operator itself is installed once at cluster level:
```bash
helm repo add k8s-agents-operator https://newrelic.github.io/k8s-agents-operator
helm install newrelic-agent-operator k8s-agents-operator/k8s-agents-operator \
  --namespace default --set licenseKey="<KEY>"
```

### Key Conventions

- `k8s/base/secret.yml` contains real credentials and is gitignored — `secret.example.yml` is the committed template.
- The operator's license key secret is named `newrelic-key-secret` with key `new_relic_license_key` (created by the helm install).
- Spring Boot dev profile (`application-dev.properties`) enables Micrometer; activated via `SPRING_PROFILES_ACTIVE=dev` in the configmap.
- Port assignments: Spring Boot → `localhost:8080`, .NET → `localhost:5000`.
