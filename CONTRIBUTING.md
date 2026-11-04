# Contributing

Follow the [code of conduct](CODE_OF_CONDUCT.md). Use [SETUP.md](SETUP.md) for environment configuration and the [developer guide](docs/DEVELOPER_GUIDE.md) for checks and common tasks.

## Workflow

1. Search existing issues and pull requests before starting substantial work. Report vulnerabilities privately through [SECURITY.md](SECURITY.md).
2. Fork or create a focused branch. Existing conventions use `feature/*`, `bugfix/*`, `hotfix/*`, and `release/*`; `develop` is the integration branch and `main` the release branch. Name the intended PR base explicitly.
3. Implement one coherent change, update its canonical documentation, and add meaningful unit/integration tests for behavior changes. Include UI screenshots when relevant.
4. Run the [development checks](docs/DEVELOPER_GUIDE.md#checks). Record commands, results, and pre-existing failures or documented skips; do not disable assertions to make a PR pass.
5. Use Conventional Commits (`feat`, `fix`, `docs`, `refactor`, `test`, `build`, `ci`, `chore`, `perf`, or `style`), for example `fix(mqtt): validate command topics`.
6. Self-review the complete diff, remove generated output and secrets, and explain the problem, resulting behavior, validation, and limitations in the PR. Obtain a maintainer review and resolve feedback before merging.

## Code and tests

Follow [.editorconfig](.editorconfig) and nearby C# conventions: PascalCase public members/types, `_camelCase` private fields, constructor injection, and `Async` suffixes for asynchronous operations. Avoid `async void` outside event handlers. Use structured logs without credentials and retain explanations of important contracts and design decisions.

Use asynchronous EF operations, `AsNoTracking` for read-only entity queries, and transactions when several writes must succeed together. Validate MQTT topics/payloads before processing and handle malformed input explicitly. Keep Core/Application/Infrastructure/API responsibilities cohesive; see the [architecture guide](docs/ARCHITECTURE.md) for current coupling rather than assuming separate assemblies.

Unit tests use xUnit, Moq, and FluentAssertions. HTTP integration tests use `WebApplicationFactory` and an in-memory database; they do not prove SQL Server or physical-device behavior. Test success/failure boundaries, cancellation, and external failures where relevant. Preserve public routes, JSON contracts, recovery paths, and useful fixtures. Coverage of 80% and end-to-end coverage are goals, not established results or enforced thresholds in the current CI.

## Issues and releases

Bug reports should include environment versions, reproduction steps, expected/actual results, and sanitized logs. Feature proposals should explain the user need and alternatives. Link an existing issue instead of creating a duplicate.

Use semantic versioning. For a release, prepare a release branch, update project versions and [CHANGELOG.md](CHANGELOG.md), run the full suite and deployment checks, then obtain review before merging, tagging, publishing release notes, and deploying. Release actions require the maintainer's authorization.

Contributions are licensed under the project's [MIT License](LICENSE).
