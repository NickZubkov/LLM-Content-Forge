# LLM Content Forge

A Unity **editor tool** that generates game content (items, enemies) through an LLM,
backed by a purpose-built **ASP.NET Core** service. The Unity side never talks to the
model directly — it calls *my* API, which owns prompt building, validation, and
resilience.

```
Unity Editor tool  ──►  Content Forge API (ASP.NET Core)  ──►  LLM
   (unity/)                     (server/)                    (local Ollama)
```

> **Status:** work in progress. Stage 1 (server skeleton, health endpoint, CI, Docker,
> first test) is in place. Generation, LLM integration, and the Unity tool follow.

## Repository layout

```
server/    ASP.NET Core (.NET 8) — the "Content Forge API"
unity/     Unity 6 editor tool (URP 2D project)
.github/   CI workflows
```

The two halves are independent and are built and tested separately.

## Running the server

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
cd server
dotnet run --project src/ContentForge.Api
```

Then open the Swagger UI at `http://localhost:<port>/swagger`, or hit the health probe:

```bash
curl http://localhost:<port>/api/v1/health   # {"status":"ok"}
```

### With Docker

```bash
cd server
docker compose up --build
# Swagger UI: http://localhost:8080/swagger
```

## Tests

```bash
cd server
dotnet test
```

## Design decisions

- **Provider-agnostic LLM backend.** The API talks to an LLM behind an abstraction, so
  the provider is swappable via config. The default is a **local Ollama** instance
  (OpenAI-compatible endpoint) — no cloud account, no API keys, no regional limits — and
  any cloud provider can be dropped in later.
- **Minimal API over MVC controllers.** The API surface is small; Minimal API keeps it
  concise and readable while modeling current ASP.NET Core practice.
- **Monorepo.** One repository, two clearly separated halves (`server/`, `unity/`), each
  with its own build and test lifecycle.

## Next steps (out of MVP scope)

Authentication, a database, cloud deployment, image generation, and runtime (in-game)
use are intentionally out of scope for the MVP.

## License

MIT — see [LICENSE](LICENSE).
