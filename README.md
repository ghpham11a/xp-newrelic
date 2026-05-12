# xp-newrelic

Experimental repo for integrating New Relic observability across multiple backend services.

## New Relic Kubernetes APM Auto-Attach Operator

The operator automatically injects New Relic APM agents into pods via annotations. It's installed once at the cluster level and shared by all backend services.

### Prerequisites

- A Kubernetes cluster
- [Helm 3](https://helm.sh/docs/intro/install/)
- A [New Relic Ingest License Key](https://docs.newrelic.com/docs/apis/intro-apis/new-relic-api-keys/#ingest-license-key)

### Install the Operator

```bash
helm repo add newrelic https://helm-charts.newrelic.com
helm repo update
helm install newrelic-agent-operator newrelic/k8s-agents-operator \
  --namespace newrelic \
  --create-namespace
```

### Upgrade the Operator

```bash
helm repo update
helm upgrade newrelic-agent-operator newrelic/k8s-agents-operator \
  --namespace newrelic
```

### Uninstall the Operator

```bash
helm uninstall newrelic-agent-operator --namespace newrelic
```

### Per-Service Setup

Each backend service needs two things:

**1. An `Instrumentation` CR in its namespace:**

```yaml
apiVersion: newrelic.com/v1alpha2
kind: Instrumentation
metadata:
  name: newrelic-java
  namespace: <service-namespace>
spec:
  java:
    image: newrelic/newrelic-java-init:latest
  env:
    - name: NEW_RELIC_LICENSE_KEY
      valueFrom:
        secretKeyRef:
          name: newrelic-secret
          key: license-key
```

Swap `java` for the appropriate language block if needed (`nodejs`, `python`, `dotnet`).

**2. A pod annotation on the Deployment:**

```yaml
spec:
  template:
    metadata:
      annotations:
        instrumentation.newrelic.com/inject-java: "true"
```

That's it — the operator handles init container injection, volume mounts, and `JAVA_TOOL_OPTIONS` automatically.

### Supported Languages

| Language | Annotation | Instrumentation block |
|----------|-----------|----------------------|
| Java     | `instrumentation.newrelic.com/inject-java: "true"` | `spec.java` |
| Node.js  | `instrumentation.newrelic.com/inject-nodejs: "true"` | `spec.nodejs` |
| Python   | `instrumentation.newrelic.com/inject-python: "true"` | `spec.python` |
| .NET     | `instrumentation.newrelic.com/inject-dotnet: "true"` | `spec.dotnet` |

### References

- [k8s-agents-operator docs](https://docs.newrelic.com/docs/kubernetes-pixie/kubernetes-integration/installation/k8s-agent-operator/)
- [Helm chart source](https://github.com/newrelic/helm-charts/tree/master/charts/k8s-agents-operator)
