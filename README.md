# MujDomecek (My Little House)

This repository contains the new implementation of MujDomecek based on the full specification in `docs/`.
It is a clean-start monorepo with a .NET 10 backend and a SvelteKit frontend.

## Structure

```
/
├── docs/                # Product + architecture specification
├── src/
│   ├── api/             # .NET API (Clean Architecture)
│   ├── web/             # SvelteKit app (PWA, local-first)
│   └── shared/          # Shared types (OpenAPI codegen output)
├── tests/               # Test projects
└── package.json         # Root tooling (husky + lint-staged)
```

## Getting started

- Install .NET 10 SDK and Node 22
- `npm install`
- Backend: `dotnet build src/api/MujDomecek.sln`
- Frontend: `npm --prefix src/web install` and `npm --prefix src/web run dev`

## References

- `docs/README.md` for the full spec
- `docs/11-tech-standards.md` for architecture and conventions
- `docs/13-api-reference.md` for API endpoints
