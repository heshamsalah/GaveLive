# 🔨 GavelLive

A real-time auction platform built with **.NET 9** and **Angular** — developed phase by phase as a hands-on learning project, where every technology is added only after feeling the problem it solves.

> **Golden rule of this project:** Never add a technology before you've felt the problem it solves. Redis comes after reads feel slow. A message broker comes after a notification blocks a bid.

---

## 🏗️ Architecture

GavelLive is a **modular monolith** built with **Vertical Slice Architecture** — every feature lives in its own self-contained folder with its request, validation, handler, and endpoint. The design deliberately prepares clean seams for a future split into microservices.

```
src/
├── Api/                  # ASP.NET Core Minimal API
│   ├── Features/         # Vertical slices (one folder per feature)
│   │   ├── CreateAuction/
│   │   ├── GetAuctions/
│   │   └── PlaceBid/
│   ├── Models/           # Auction, Bid entities
│   ├── Migrations/       # EF Core migrations
│   └── Program.cs
├── AppHost/              # .NET Aspire orchestration
└── ServiceDefaults/      # Shared OpenTelemetry / health check defaults
```

## 🧰 Tech Stack

| Technology | Role |
|---|---|
| **ASP.NET Core (Minimal APIs)** | JSON API backend |
| **PostgreSQL + EF Core** | Source of truth for auctions & bids |
| **MediatR** | Routes requests to handlers (CQRS spine) |
| **Serilog** | Structured logging — the bid audit trail |
| **Docker + Docker Compose** | Containerized API + database |
| **.NET Aspire** | Orchestration, service discovery, live dashboard (logs + traces) |
| **Angular** | Frontend (auction list, detail, bidding) |

## ✅ Progress

Built strictly in order — each phase has a Definition of Done that must pass before moving on.

- [x] **Phase 0 — Setup & foundations** · Toolbelt, Git repo, solution skeleton
- [x] **Phase 1 — Core canvas** · Create auction, list auctions, place bid (refresh-to-see)
- [x] **Phase 2 — Vertical slices + MediatR + CQRS** · Feature folders, thin endpoints, and the two-bidders race condition reproduced then fixed with **optimistic concurrency**
- [x] **Phase 3 — Serilog structured logging** · Every bid logged as a structured event (`AuctionId`, `BidderId`, `Amount`, `Timestamp`) — the start of the audit trail
- [x] **Phase 4 — Docker + Compose** · API + Postgres running together with one command
- [x] **Phase 4.5 — .NET Aspire** · AppHost orchestration, automatic connection injection, live dashboard with logs **and distributed traces**
- [ ] **Phase 5 — Redis** · Cache auction prices + live "X watching" presence counter
- [ ] **Phase 6 — RabbitMQ + MassTransit** · Event-driven bids, background auction-ending worker, CQRS read model
- [ ] **Phase 7 — SignalR** · Live bids on every screen, no refresh
- [ ] **Phase 8 — Keycloak** · Login, roles (Bidder / Seller / Admin)
- [ ] **Phase 9 — HashiCorp Vault** · Secrets out of config files
- [ ] **Phase 10 — OpenObserve** · Searchable logs/traces/metrics + suspicious-bidding detection
- [ ] **Appendix A — Microservices split** (optional endgame)

## 🚀 Running Locally

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (running)
- [Node.js + Angular CLI](https://angular.dev) (for the frontend)

### Option 1 — .NET Aspire (recommended)

One command starts everything — Postgres container, API, with connection strings injected automatically:

```bash
cd src/AppHost
dotnet run
```

Then open the **Aspire dashboard** URL printed in the console (grab the login token from the same output). From there you can:
- See all resources (`postgres`, `gavellive` db, `api`) and their health
- Jump to the API's Swagger UI from the resource's URL
- Watch **live logs** and **distributed traces** for every request

> **First run only:** apply EF Core migrations against the Aspire-managed database. Copy the Postgres connection string from the dashboard (click the `postgres` resource), then:
> ```bash
> cd src/Api
> dotnet ef database update --connection "<connection string from dashboard>"
> ```

### Option 2 — Docker Compose (the manual way, kept for reference)

```bash
docker compose up --build
```

API + Swagger available at `http://localhost:8080/swagger/index.html`.

## 📚 Key Lessons Baked Into the Code

- **The concurrency bug** — two simultaneous bids on one auction corrupt the price. Fixed with EF Core optimistic concurrency (a row version on `Auction`; stale saves are rejected and retried). *Phase 2*
- **Structured logs, not sentences** — `log.Information("Bid placed {AuctionId} {BidderId} {Amount} {Timestamp}", ...)` produces queryable fields, not grep-only strings. *Phase 3*
- **Compose before Aspire** — hand-wiring connection strings first makes it obvious what Aspire automates (and makes password-mismatch bugs structurally impossible afterward). *Phase 4 → 4.5*
- **Pin your database version** — `postgres:latest` silently jumped to a major version with incompatible data layout. Pinned images (`postgres:16`) keep dev environments predictable. *Phase 4, learned the hard way*

## 🗺️ Roadmap Philosophy

This project follows a two-document plan (Project Brief + Build Roadmap). Every phase uses the same structure: **Goal → Build → Why/Where/How → Definition of Done → Pitfalls.** The monolith is not a throwaway — it's the correct first step, and the eventual microservices split is mechanical because of the event-driven seams designed in from Phase 6.

---

*Built as a deep-dive learning project — one phase, one technology, one felt problem at a time.*
