# GitHub Copilot Instructions for pefi.http

## Project Overview

`pefi.http` is a **C# Roslyn incremental source generator** that reads an OpenAPI 3.x JSON specification and emits a strongly-typed `HttpClient` wrapper at compile time. Consumers decorate a `partial class` with `[GenerateHttpClient("spec-file.json")]`; the generator picks up the spec (supplied as an MSBuild `AdditionalFiles` entry) and writes a `.g.cs` file containing the client and all model classes.

The package targets **netstandard2.0** and is published to GitHub Packages.

---

## Repository Layout

```
pefi.http/
├── src/
│   ├── OpenApiClientGenerator.cs   # Roslyn IIncrementalGenerator entry point
│   ├── HttpClientGenerator.cs      # Core code-generation logic (ClientGenerator class)
│   ├── ClassDeclarationContext.cs  # Internal DTO holding parsed attribute metadata
│   └── pefi.http.csproj            # netstandard2.0 Roslyn analyser project
├── test/
│   └── pefi.http.OpenApiClientGenerator.Tests/
│       ├── UnitTest1.cs            # xUnit tests exercising ClientGenerator directly
│       ├── client_config/
│       │   └── service_mgr_openapi.json   # Example OpenAPI spec used by tests
│       └── pefi.http.OpenApiClientGenerator.Tests.csproj
├── .github/
│   ├── workflows/
│   │   └── main.yml                # CI: build, pack, and publish NuGet to GitHub Packages
│   └── copilot-instructions.md     # This file
├── pefi.http.sln
└── README.md
```

---

## Architecture

### How the Generator Works

1. **`OpenApiClientGenerator`** (`IIncrementalGenerator`) is the Roslyn entry point.
   - It watches for `ClassDeclarationSyntax` nodes whose `AttributeLists` are non-empty.
   - It also collects all `AdditionalTextsProvider` files.
   - When a class with `[GenerateHttpClientAttribute("spec.json")]` is found, it calls `ClientGenerator.Execute(...)`.

2. **`ClientGenerator`** (`HttpClientGenerator.cs`) does the actual work:
   - Parses the OpenAPI document using `OpenApiDocument.Parse(...)` from the `Microsoft.OpenApi` library.
   - Emits C# source as a `StringBuilder`:
     - Model classes for each entry in `components/schemas`
     - A `partial class` with constructor and one `async Task` method per operation
   - Returns the generated source as a `string`.

3. **`ClassDeclarationContext`** is a small immutable DTO holding the class symbol, the spec file name, and the original syntax node.

### Attribute Lookup

The generator looks for the fully-qualified attribute name `pefi.http.GenerateHttpClientAttribute`. The attribute itself is expected to be defined inside the consuming project (or a shared library); the generator only inspects the attribute by name.

### Spec Resolution

The spec is resolved by **file name only** — the `FileName` of each `AdditionalTextsProvider` entry is compared against the value passed to the attribute constructor. This means the attribute argument must match the file name (e.g. `"my-api.json"`), not a full path.

---

## Key Conventions & Patterns

- **Incremental generator**: Use `IIncrementalGenerator` / `IncrementalGeneratorInitializationContext`, not the older `ISourceGenerator`.
- **`async void` in the generator**: `Execute` in `OpenApiClientGenerator` is currently declared `async void` — this is an acknowledged anti-pattern (exceptions can be silently swallowed). All exceptions must be caught explicitly inside the method body. Prefer wrapping the async work in a `try/catch` and reporting any failure via `context.ReportDiagnostic`. Ideally, this should be refactored to a synchronous call or a properly-awaited helper in future.
- **Identifier sanitisation**: All names from the OpenAPI spec are sanitised via `SanitizeIdentifier` (replaces non-alphanumeric characters, handles C# reserved keywords with `@` prefix). Follow the same pattern when mapping new field types.
- **Null handling**: The project has `<Nullable>enable</Nullable>`. Use nullable reference types and annotations throughout.
- **PrivateAssets="all"**: Dependencies like `Microsoft.CodeAnalysis.CSharp` and `Microsoft.OpenApi` are private assets to avoid polluting consumer projects.
- **`EnforceExtendedAnalyzerRules`**: Enabled — avoid APIs that are forbidden in analysers (e.g. file I/O in the generator itself).

---

## Adding New OpenAPI Features

When extending the generator to support additional OpenAPI constructs:

1. Add or update logic in `HttpClientGenerator.cs` (the `ClientGenerator` partial class).
2. Add a matching test in `UnitTest1.cs` using a real or minimal OpenAPI JSON spec.
3. Keep identifier sanitisation consistent — always go through `SanitizeIdentifier` / `SanitizeParameterName`.
4. Update `GetCSharpTypeName` for any new type mappings.
5. Update `README.md` if the new feature changes the public usage pattern or type-mapping table.

---

## Testing

Tests use **xUnit** and exercise `ClientGenerator.Execute(...)` directly (unit-testing the code generation logic without running the full Roslyn pipeline).

```bash
dotnet test
```

When adding a new test scenario:
- Place the OpenAPI spec JSON in `test/pefi.http.OpenApiClientGenerator.Tests/client_config/`.
- Read it via `System.IO.File.ReadAllText(...)` and pass its contents to `ClientGenerator.Execute(...)`.
- Assert on the returned source string (e.g. that expected type names and method signatures are present).

---

## CI / CD

The GitHub Actions workflow (`.github/workflows/main.yml`) runs on every push to `main`:

1. Restore → Build (Release) → Pack → Publish to GitHub Packages.
2. The package version is set to `1.0.0-ci-<date>.<run_number>`.

To publish a stable release, update `<Version>` in `src/pefi.http.csproj` and push to `main`.

---

## Do's and Don'ts for Copilot

**Do:**
- Keep generated code readable and idiomatic C# (proper indentation, standard naming conventions).
- Sanitise every name that comes from the OpenAPI spec before emitting it as a C# identifier.
- Prefer `StringBuilder.AppendLine(...)` for source generation to keep line endings consistent.
- Write unit tests for any new generation logic.

**Don't:**
- Perform file I/O inside the generator — all spec content arrives via `AdditionalTextsProvider`.
- Use APIs banned under `EnforceExtendedAnalyzerRules` (e.g. `Environment`, `Console`, synchronous file access).
- Add new NuGet package dependencies without checking they can be bundled as private analyser assets.
- Break the `netstandard2.0` target — do not use APIs unavailable in that TFM.
