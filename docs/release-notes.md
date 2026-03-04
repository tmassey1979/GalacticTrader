# Release Notes

## 2026-03-04

### Security

- Desktop dependency graph hardening for NuGet advisory remediation (`#209`):
  - Added explicit top-level package references in desktop projects:
    - `System.Net.Http` `4.3.4`
    - `System.Text.RegularExpressions` `4.3.1`
  - Rationale:
    - NuGet audit reported high-severity vulnerabilities via transitive `4.3.0` packages.
    - Pinning safe versions at top level removes the vulnerable transitive resolution path while preserving current desktop runtime behavior.
  - Validation:
    - `dotnet list package --vulnerable --include-transitive` reports no vulnerabilities for:
      - `src/Desktop/GalacticTrader.Desktop.csproj`
      - `tests/GalacticTrader.Desktop.Tests/GalacticTrader.Desktop.Tests.csproj`
    - Desktop test suite remains green after update.
