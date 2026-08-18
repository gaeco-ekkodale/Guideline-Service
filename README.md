<div align="center">
  <img src="https://raw.githubusercontent.com/gaeco-ekkodale/.github/main/assets/gaeco_logo_horizontal_color.png" width="200" alt="gaeco logo">

  # GuidelineService

  <em>Stores the gaeco guideline and serves the classifications, properties and standard mappings it defines.</em>

  [![License](https://img.shields.io/badge/license-fair--code-blue.svg)](LICENSE.md)
  [![Version](https://img.shields.io/github/v/release/gaeco-ekkodale/Guideline-Service)](../../releases)

  [gaeco-ekkodale Organization](https://github.com/gaeco-ekkodale) · [All Repos](https://github.com/orgs/gaeco-ekkodale/repositories)
</div>

---

gaeco (Graphs for Architecture, Engineering, Construction, Operations) is an event-driven microservice platform for BIM data management. It translates external building-industry standards (IFC, IBPDI, Brick Schema, ASHRAE 223 and others) into a shared, versioned classification and relationship model (Guideline + Ontology) and exposes consistent, graph-based building data (Instance) across use cases and departments — without forcing every consumer onto one rigid schema. Built for organizations managing building/portfolio data across disconnected departmental systems (construction, facilities management, leasing, accounting) that need automatic, reliable data propagation instead of manual, error-prone hand-offs.

> This project is licensed under the [Source Available](LICENSE.md). Source code is viewable and usable; commercial use is restricted.

---

## What this service does

The guideline is the core of gaeco's classification model: a JSON document that determines the classifications, the properties on each classification, and the mappings to external building-industry standards. The GuidelineService accepts that document, stores it, and serves the contained classifications and properties to the rest of the platform.

Where this service defines *what* things are, the [OntologyService](https://github.com/gaeco-ekkodale/OntologyService) defines *how they may be connected*. Together they form the shared, versioned model that the [AccessService](https://github.com/gaeco-ekkodale/AccessService) attaches permissions to and the [InstanceService](https://github.com/gaeco-ekkodale/InstanceService) validates building data against.

This is a server-only service; guidelines are uploaded through the [PlatformConfig](https://github.com/gaeco-ekkodale/PlatformConfig) admin UI or directly via the API.

## Repository Structure

- `Server/Api/`: ASP.NET Core Web API
- `Server/Api.Tests/`: unit tests
- `Server/Events/`: Kafka event contracts
- `_docker/`: Compose definition, env schemas and the App Registry package manifest
- `_docu/`: developer and user documentation, including the data model diagrams
- `_pipeline/`: Azure DevOps CI/CD pipeline definitions
- `_Eula/`: end user license agreement
- `build/`: NUKE build scripts

## Tech Stack

- **Backend**: .NET 8, ASP.NET Core, Entity Framework Core, AutoMapper, Newtonsoft.Json, Swagger/Swashbuckle, OpenTelemetry
- **Infrastructure**: MinIO (guideline file storage), Apache Kafka, Keycloak, PostgreSQL, Docker
- **Build**: NUKE

## Local Development

### Prerequisites

- Docker Desktop
- .NET 8 SDK
- The shared platform infrastructure (Keycloak, MinIO, Kafka) — see [`_docu/user/01-Installation.md`](_docu/user/01-Installation.md)

### Start with Docker Compose

```bash
cd _docker
docker compose -p guideline-service -f docker-compose.yml -f docker-compose-override.yml up -d
```

Ports are driven by the `GUIDELINE_*_OUTERPORT` variables in the environment files; the API exposes Swagger at `/swagger`.

## Build and Test

```bash
./build.sh     # Linux/macOS
.\build.ps1    # Windows
```

Backend tests: `dotnet test` from the repository root.

## Integration

- **Authentication**: Keycloak (OIDC/JWT). A client must authenticate before requesting data. Authentication is active whenever `ASPNETCORE_ENVIRONMENT` is not `Development`.
- **Events**: every guideline upload publishes an event to Apache Kafka, so services holding local projections of classifications refresh without synchronous calls.
- **Storage**: the guideline file itself lives in MinIO.

## Documentation

- [Concepts](_docu/developer/01-Concepts.md)
- [Patterns](_docu/developer/02-Patterns.md)
- [Used Technologies](_docu/developer/03-Used-Technologies.md)
- [Data Model](_docu/developer/04-Data-Model.md) · [Diagrams](_docu/developer/Datamodel/Readme.md)
- [Software Architecture](_docu/developer/05-Software-Architecture.md)
- [Installation](_docu/user/01-Installation.md) · [User Manual](_docu/user/02-User-Manual.md)
