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
helm repo add k8s-agents-operator https://newrelic.github.io/k8s-agents-operator
helm repo update
helm install newrelic-agent-operator k8s-agents-operator/k8s-agents-operator \
  --namespace default \
  --set licenseKey="<YOUR_NEW_RELIC_LICENSE_KEY>"
```

### Upgrade the Operator

```bash
helm repo update
helm upgrade newrelic-agent-operator k8s-agents-operator/k8s-agents-operator \
  --namespace default
```

### Uninstall the Operator

```bash
helm uninstall newrelic-agent-operator --namespace default
```

### Per-Service Setup

Each backend service needs two things:

**1. An `Instrumentation` CR in its namespace:**

```yaml
apiVersion: newrelic.com/v1beta3
kind: Instrumentation
metadata:
  name: newrelic-java
  namespace: <service-namespace>
spec:
  agent:
    language: java
    image: newrelic/newrelic-java-init:latest
  licenseKeySecret: newrelic-key-secret
```

Swap `language` and `image` for the appropriate language/init image.

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

| Language | `spec.agent.language` | `spec.agent.image` | Annotation |
|----------|----------------------|---------------------|-----------|
| Java     | `java` | `newrelic/newrelic-java-init:latest` | `instrumentation.newrelic.com/inject-java: "true"` |
| Node.js  | `nodejs` | `newrelic/newrelic-node-init:latest` | `instrumentation.newrelic.com/inject-nodejs: "true"` |
| Python   | `python` | `newrelic/newrelic-python-init:latest` | `instrumentation.newrelic.com/inject-python: "true"` |
| .NET     | `dotnet` | `newrelic/newrelic-dotnet-init:latest` | `instrumentation.newrelic.com/inject-dotnet: "true"` |

## Dashboard

Create a dashboard in New Relic (**Dashboards > Create a dashboard > Add widget > NRQL query**) with these widgets:

| Widget | NRQL Query | Viz Type |
|--------|-----------|----------|
| Item Count | `SELECT latest(items.count) FROM Metric FACET appName SINCE 30 minutes ago` | Billboard |
| Operations/min | `SELECT rate(sum(items.operations), 1 minute) FROM Metric FACET appName, operation TIMESERIES SINCE 1 hour ago` | Line chart |
| Op Duration p95 | `SELECT percentile(items.operation_duration, 95) FROM Metric FACET appName, operation TIMESERIES SINCE 1 hour ago` | Line chart |
| Error Rate | `SELECT percentage(count(*), WHERE error IS true) FROM Transaction WHERE appName IN ('springboot', 'dotnet-api') FACET appName TIMESERIES SINCE 1 hour ago` | Line chart |
| Request Bodies | `SELECT appName, request.body, response.body, name, httpResponseCode FROM Transaction WHERE appName IN ('springboot', 'dotnet-api') AND request.uri NOT LIKE '%health%' AND request.uri NOT LIKE '%actuator%' SINCE 30 minutes ago` | Table |
| Operations Breakdown | `SELECT sum(items.operations) FROM Metric FACET appName, operation SINCE 1 hour ago` | Pie chart |

### References

- [k8s-agents-operator docs](https://docs.newrelic.com/docs/kubernetes-pixie/kubernetes-integration/installation/k8s-agent-operator/)
- [Helm chart source](https://github.com/newrelic/k8s-agents-operator)
