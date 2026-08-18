# Guideline Service

Der Guideline Service ist verantwortlich für die Verwaltung und Bereitstellung von Guidelines im Gaeco-Ökosystem. Er stellt eine API zur Verfügung, über die Guidelines abgerufen und aktualisiert werden können. Guidelines werden als Dateiartefakte in MinIO gespeichert und über Kafka-Events an andere Dienste kommuniziert.

## Enthaltene Dienste

- **Guideline Server** (`guideline-server`) – .NET Backend, erreichbar über Traefik unter `GUIDELINE_SERVER_HOSTNAME`
