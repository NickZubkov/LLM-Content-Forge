# LLM Content Forge

A Unity **editor tool** that generates game content — items and enemies — through an LLM,
backed by a purpose-built **ASP.NET Core** service. The Unity side never talks to the
model directly: it calls *my* API, which owns prompt building, validation, and resilience.
Generated content is reviewed in a diff preview and written to `ScriptableObject` assets.

```
┌────────────────────────┐     POST /api/v1/generate      ┌──────────────────────────┐         ┌──────────────┐
│  Unity editor tool     │  ───────────────────────────►  │  Content Forge API       │  ────►  │  LLM         │
│  (unity/)              │                                │  (server/, ASP.NET Core) │         │ (local Ollama)│
│                        │  ◄───────────────────────────  │                          │  ◄────  │              │
│  request → diff        │     validated JSON content     │  prompt → validate →     │         └──────────────┘
│  preview → SO assets   │                                │  resilient LLM call      │
└────────────────────────┘                                └──────────────────────────┘
```

> **Status:** MVP complete. Server (LLM integration, validation, resilience, tests, Docker,
> CI) and the Unity editor tool (request → diff preview → ScriptableObject assets, EditMode
> tests) are in place. See [Next steps](#next-steps-out-of-mvp-scope) for what is intentionally left out.

## Repository layout

```
server/    ASP.NET Core (.NET 8) — the "Content Forge API"
unity/     Unity 6 editor tool (URP 2D project)
.github/   CI workflow (builds and tests the server on every push)
```

The two halves are independent and are built and tested separately.

## How it works

1. In Unity, **Window → Content Forge** opens the tool. You pick a content type (Item or
   Enemy), a count, a theme, and a level range, then press **Generate**.
2. The tool `POST`s the request to the API. The API builds a prompt from templates, calls
   the LLM behind an `ILlmClient` abstraction, extracts the JSON array from the (possibly
   noisy) completion, deserializes it into typed content, and validates every entry against
   the requested value ranges. Failures come back as normalized `ProblemDetails` errors.
3. The tool parses the validated response, maps it to in-memory `ScriptableObject`s, applies
   a second line of client-side validation, and diffs the result against existing assets in
   the target folder — showing each entry as **New / Changed / Unchanged / Invalid** with a
   field-level diff.
4. You tick the entries you want and press **Apply Selected**; the tool creates or updates
   the `.asset` files.

## Running it

The end-to-end flow needs three things: a local LLM (Ollama), the API, and the Unity tool.

### 1. LLM backend — Ollama

Install [Ollama](https://ollama.com/download) and pull the default model:

```bash
ollama pull qwen2.5:7b
```

Ollama serves an OpenAI-compatible API on `http://localhost:11434`. The model, base URL,
timeout, and an optional API key are all config-driven (see `server/src/ContentForge.Api/appsettings.json`,
section `Llm`); the API key is only ever read from an environment variable.

### 2. The server

**With Docker (one command, reaches the host's Ollama):**

```bash
cd server
docker compose up --build
# API + Swagger UI: http://localhost:8080/swagger
```

**Or with the .NET 8 SDK directly:**

```bash
cd server
dotnet run --project src/ContentForge.Api
# Swagger UI at the printed http URL (e.g. http://localhost:5283/swagger)
```

Health check either way:

```bash
curl http://localhost:8080/api/v1/health   # {"status":"ok"}
```

### 3. The Unity editor tool

Open the `unity/` project in **Unity 6000.3.19f1** (the version is pinned in
`ProjectSettings/ProjectVersion.txt`). Then **Window → Content Forge**, set **Server URL** to
match the server above (`http://localhost:8080` for Docker), and press **Generate**. Review
the diff and **Apply Selected** to write assets under `Assets/ContentForge/Generated/`.

## Tests

**Server** (xUnit + FluentAssertions + Moq — the `ILlmClient` is mocked):

```bash
cd server
dotnet test
```

**Unity** (EditMode / NUnit — parser, mapper, client validation, differ, asset writer,
end-to-end pipeline): open **Window → General → Test Runner**, EditMode tab, **Run All**.

## Design decisions

- **The Unity side never calls the LLM directly.** Prompting, schema validation, value-range
  checks, retries, and timeouts all live server-side, behind one REST endpoint. The editor
  tool stays thin and the "hard" logic is testable in isolation.
- **Provider-agnostic LLM backend.** The API talks to the model behind an `ILlmClient`
  abstraction. The default is a **local Ollama** via its OpenAI-compatible endpoint — no cloud
  account, no API keys, no regional limits — and any OpenAI-compatible cloud provider can be
  swapped in through config alone.
- **Minimal API over MVC controllers.** The surface is small (`generate`, `health`); Minimal
  API keeps it concise while modeling current ASP.NET Core practice.
- **Typed validation over a JSON-schema library.** Generated entries are deserialized into
  records and checked in code (allowed rarities, value ranges, positivity), which keeps the
  rules readable and unit-testable. Invalid content fails fast as a `502`.
- **Resilience via `Microsoft.Extensions.Http.Resilience` (Polly v8).** Transient LLM failures
  are retried with exponential backoff + jitter; `CancellationToken` flows through the whole
  chain so requests are cancellable end-to-end.
- **Prompt templates as embedded files.** System/item/enemy templates with `{{placeholders}}`
  live next to the code and are compiled in, so prompt changes are reviewable in diffs.
- **Editor tool: IMGUI + UnityWebRequest + UniTask.** IMGUI suits a simple tool window;
  UniTask gives cancellable `async` over `UnityWebRequest` without pulling in a DI container.
- **A pure, testable pipeline core.** Parsing, mapping, and diffing are pure static classes
  with no `AssetDatabase` calls; the differ takes already-loaded assets as input. Only a thin
  `AssetWriter` touches the asset database, so the interesting logic is covered by fast
  EditMode tests.
- **ScriptableObject content, matched by name slug.** Generated assets are runtime
  `ScriptableObject`s (usable in a game), keyed by a slug of their name, so re-generating
  produces a clear New/Changed/Unchanged diff and updates assets in place instead of
  duplicating them.
- **Monorepo.** One repository, two clearly separated halves, each with its own build and
  test lifecycle.

## Next steps (out of MVP scope)

Authentication, a database, cloud deployment, image generation, richer content types, and
runtime (in-game) use are intentionally out of scope for the MVP.

## License

MIT — see [LICENSE](LICENSE).
