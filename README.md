# GaveLive

A real-time auction platform built on ASP.NET Core and Angular, designed as an event-driven modular monolith with clean seams for a future microservices split.

Users list items for auction, place bids, and watch prices update live across every connected client. The system is built around a tamper-evident bid audit trail, safe concurrent bidding, and independently scalable read and write paths.

## Overview

GavelLive is structured as a modular monolith using Vertical Slice Architecture. Each feature — creating an auction, listing auctions, placing a bid — is a self-contained slice owning its request contract, validation, handler, and endpoint. There is no shared services layer; feature boundaries in the codebase map directly to the service boundaries a future decomposition would use.

The backend follows CQRS. Commands (place bid, create auction) write to PostgreSQL, the system's source of truth. Queries are served from a denormalized read model maintained in Redis, kept fresh by event handlers rather than computed per request. Reads and writes scale independently.

Bid processing is event-driven. A successful bid publishes a `BidPlaced` event to RabbitMQ; downstream consumers handle outbid notifications, read-model projection updates, and live client broadcasts without ever blocking the bid itself. A background worker closes auctions at their end time by publishing `AuctionEnded`, which drives winner notification and payment handling through the same event pipeline.

## Architecture

**Write path.** An incoming bid travels through a thin Minimal API endpoint into a MediatR command handler. The handler validates the bid against the auction's current price and persists it with optimistic concurrency — a row version on the auction row causes stale writes to be rejected and retried, so two simultaneous bids can never corrupt the price. Bids for the same auction are processed with per-auction ordering.

**Read path.** Auction cards are served from a Redis projection (current price, bid count) updated by `BidPlaced` consumers. PostgreSQL is never joined at read time for hot paths.

**Real-time layer.** SignalR pushes price and bid-count updates to clients grouped per auction. Broadcasts are driven exclusively by the event flow — there is no ad-hoc side channel — so every screen reflects the same source of truth. Redis also tracks per-auction watcher presence with short-TTL keys, powering a live viewer count.

**Identity and authorization.** Keycloak owns identity via OpenID Connect. The Angular client redirects to Keycloak for login; the API validates issued tokens and enforces roles (Bidder, Seller, Admin) on protected endpoints. Application user records are thin profiles linked by Keycloak's user ID.

**Secrets.** Connection strings, client secrets, and API keys live in HashiCorp Vault and are fetched at startup. No secrets exist in configuration files or repository history.

**Observability.** Every bid is logged as a structured Serilog event (auction, bidder, amount, timestamp), forming an auditable history from day one. OpenTelemetry traces and metrics — wired through .NET Aspire's service defaults — flow alongside those logs into OpenObserve, where saved queries surface bid activity per auction and flag suspicious patterns such as one account rapidly hammering a single auction.

## Technology

| Concern | Technology |
| --- | --- |
| API | ASP.NET Core Minimal APIs, MediatR, FluentValidation |
| Persistence | PostgreSQL, EF Core (code-first migrations, optimistic concurrency) |
| Caching and presence | Redis |
| Messaging | RabbitMQ with MassTransit |
| Real-time | SignalR |
| Identity | Keycloak (OpenID Connect) |
| Secrets | HashiCorp Vault |
| Logging | Serilog (structured) |
| Telemetry | OpenTelemetry via .NET Aspire service defaults |
| Observability backend | OpenObserve (OTLP) |
| Orchestration | .NET Aspire AppHost; Docker Compose retained as reference |
| Frontend | Angular |

## Solution Layout

The solution contains three projects. `Api` hosts the application itself, organized as one folder per feature slice, plus the domain models and EF Core migrations. `AppHost` is the .NET Aspire orchestrator that declares every resource — PostgreSQL, Redis, RabbitMQ, Keycloak, Vault, OpenObserve, and the API — including startup ordering and health-based waits. `ServiceDefaults` carries the shared OpenTelemetry, resilience, and health-check configuration consumed by all services.

## Running Locally

Prerequisites: .NET SDK, Docker Desktop (running), Node.js with the Angular CLI.

Start the full system through Aspire:

```
cd src/AppHost
dotnet run
```

The AppHost starts every dependency as a container, waits for health, injects connection details into the API via service discovery, and prints the Aspire dashboard URL with a login token. From the dashboard you can open the API's Swagger UI, inspect each resource's configuration, and watch live logs and distributed traces per request.

On a fresh database, apply migrations once using the connection string exposed by the dashboard's PostgreSQL resource:

```
cd src/Api
dotnet ef database update --connection "<connection string from the dashboard>"
```

A Docker Compose definition is also included and runs the API and PostgreSQL standalone (`docker compose up --build`), primarily kept as a reference for what Aspire automates.

## Design Decisions

**Vertical slices over layers.** Changing "place a bid" touches one folder, not five. The codebase states what the product does, and slice boundaries double as the future service boundaries.

**Optimistic concurrency over locking.** Auctions are read-heavy with contention concentrated in short bursts near close. Rejecting and retrying stale writes outperforms pessimistic locking here and keeps the handler logic simple.

**Events as the integration seam.** Notifications, projections, and broadcasts consume `BidPlaced` independently. None of them can slow down or fail a bid, and any consumer group can be lifted into its own deployable service without changing how it communicates.

**A monolith first, deliberately.** The system is designed so that decomposition is mechanical: the Notifications slice group, for example, consumes only events and shares no tables, so moving it into its own service requires no protocol changes. The monolith is the correct first step, not a compromise.

**Pinned infrastructure versions.** All container images are version-pinned. Database images in particular are never run as `latest`; a major-version jump changes the on-disk data layout and breaks existing volumes.

## Audit and Abuse Detection

Every bid attempt produces a structured log event carrying the auction, bidder, amount, and timestamp. Because these are fields rather than sentences, the history is queryable: OpenObserve dashboards show bid activity per auction, and saved queries surface abuse signatures — many rapid bids from one account on one auction, or repeated just-below-threshold probing. The audit trail was built in before the features that depend on it, so the platform's trust story is grounded in data it has collected from the beginning.


