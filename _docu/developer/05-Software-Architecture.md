# Software Architecture

This document describes the software architecture of the Guideline Service.

## Overview

The Guideline Service consists of a backend service which is a .NET 8 application that provides a REST API for managing providing data extracted from a guideline.

## Backend Architecture

The backend is a modular, multi-project solution and consists of the following layers:

- **API Layer (`Guideline.Service.Api`)**  
  Handles incoming HTTP requests and outgoing responses. This layer includes controllers that expose the application's REST API, using Data Transfer Objects (DTOs) for client communication. It serves as the main entry point for integrations and user interaction.
- **Events Layer (`Guideline.Service.Events`)**  
  Defines events used within the service (for example, `UploadedGuideline`).
- **Test Layers**
  - **Unit and Integration Tests (`Guideline.Service.Tests`)**  
    Contains automated tests for validating the logic and stability of the API and events.
  - **Common Test Utilities (`Guideline.Tests.Common`)**  
    Shares test helpers, mocks, or fixtures across multiple test projects, promoting code reuse and consistency in testing.

### External Projects

Guideline.Service depends on two external projects to provide essential building blocks:

- **Guideline.Model**
  - Provides domain entities, business models, and related logic shared across the application.
- **Guideline.Editor**
  - Offers tools for creating a new guideline.

### Communication

Guideline.Service exposes a REST API via the API layer. External clients and internal components interact with the service through HTTP endpoints, exchanging data using JSON-based DTOs.
