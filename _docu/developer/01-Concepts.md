# Concepts

This document describes the main concepts used in the Guideline Service.

## Guideline Management

The `Guideline Service` is responsible for uploading a guideline and providing the contained classifications with their respective properties.

### Guideline

The Guideline is a json file which determines classifications, their properties and mappings. It represents the core of the `Guideline Service`.

## Authentication and Authorization

Authentication and authorization are handled by Keycloak. Before requesting data from the `Guideline Service`, a client must authenticate. Authentication can be enabled by setting the `ASPNETCORE_ENVIRONMENT` to any value other than `Development`.

## Event Driven Design with Kafka

The `Guideline Service` uses an event-driven architecture to communicate changes in Classifications across the system. This is implemented using [Apache Kafka](https://kafka.apache.org/) as the message broker.

### Kafka Events

Whenever a new Guideline is uploaded a corresponding event is published to Kafka. This event allows other services to subscribe to Guideline changes, promoting loose coupling and enabling real-time reactions elsewhere in the platform.