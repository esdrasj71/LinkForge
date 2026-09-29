# LinkForge API

A distributed URL shortener built around three production concerns most CRUD APIs ignore: **event-driven analytics**, **cache-aside reads**, and **atomic writes**.

Built with .NET 8, PostgreSQL, and Redis. Containerized with Docker Compose so the entire stack starts with one command.

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D)](https://redis.io/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED)](https://docs.docker.com/compose/)

---

## Frontend

The Angular dashboard lives in a separate repository:

**👉 [LinkForge-Web](https://github.com/esdrasj71/LinkForge-Web)**

You'll need both repositories to run the full stack locally.

---

## Why This Exists

Most URL shorteners do one thing: store a URL and redirect to it. That's a wrapper around a database lookup.

LinkForge is built around the questions that come up once a shortener is actually used at scale:

- **What happens to redirect latency as analytics writes pile up?**
- **How does the system behave when the same link is clicked 10,000 times in a minute?**
- **What happens if Redis goes down? Do redirects stop working?**
- **How do you track who clicked what without slowing down the redirect?**

The answers shaped the architecture:

- **Event-driven analytics** — clicks are published to Redis Streams and processed asynchronously by a background worker. The redirect returns immediately; persistence happens off the request path.
- **Cache-aside reads** — the redirect path checks Redis first. Cache HITs never touch the database.
- **Decoupled failure domains** — if the worker is down, redirects still work. If Redis is down, the redirect falls through to the database. The API doesn't fail because a downstream component did.
- **Denormalized counters** — `TotalClicks` is stored on the `Link` row and updated by the worker, so the analytics endpoint never has to `COUNT(*)` over the click event table.

---

## Tech Stack

| Layer | Technology | Purpose |
|-------|-----------|---------|
| **API** | ASP.NET Core 8, C# | Web API framework |
| **ORM** | Entity Framework Core 8 | Database access and migrations |
| **Database** | PostgreSQL 16 | Persistent storage for links and click events |
| **Cache** | Redis 7 | Cache-aside reads and the click-events stream |
| **Background Processing** | .NET Worker Service | Consumes Redis Streams, persists click events |
| **Containerization** | Docker, Docker Compose | One-command startup for the entire stack |

---

## Quick Start

**Requires only Docker Desktop.** No .NET SDK, PostgreSQL, or Redis install needed.

```bash
git clone https://github.com/esdrasj71/LinkForge.git
cd LinkForge
docker compose up --build