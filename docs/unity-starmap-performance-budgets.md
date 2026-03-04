# Unity Starmap Performance Budgets

Story: `#266` Unity starmap rendering subsystem

## Runtime Budgets

- CPU (starmap planning): <= 8 ms/frame at 60 FPS target on recommended desktop hardware.
- GPU (starmap render pass): <= 8 ms/frame budget in main gameplay view.
- Memory (starmap runtime allocations): <= 350 MB incremental working set over shell baseline.
- Startup starmap preparation: no full-map mesh materialization; streaming planner warm-up <= 2 seconds.

## Planner Defaults

- Chunk size: `80`
- Base chunk radius: `2`
- Max active chunks: `125`
- Max rendered sectors: `480`
- Max rendered routes: `1200`
- LOD bands: near `80`, mid `220`, far `>220`

## Validation Approach

- Functional regression coverage:
  - `StarmapStreamingPlannerTests`
  - chunking, active budget culling, distance/frustum culling, LOD selection
- Benchmark harness:
  - `tests/GalacticTrader.Benchmarks/Benchmarks/StarmapStreamingBenchmarks.cs`
  - run with:
    - `dotnet run --project tests/GalacticTrader.Benchmarks -- --filter *StarmapStreamingBenchmarks*`

## Notes

- Console targets (`Xbox`, `PS4`) follow the same planner logic and budget strategy; platform-specific profiling should be run after devkit onboarding.
- This document defines budget thresholds; hardware-specific captures must be attached to release readiness checks.

## Baseline Measurements (March 4, 2026)

Run command:

- `dotnet run -c Release --project tests/GalacticTrader.Benchmarks -- --filter *StarmapStreamingBenchmarks* --job short --warmupCount 1 --iterationCount 3`

Results from `BenchmarkDotNet.Artifacts/results/GalacticTrader.Benchmarks.StarmapStreamingBenchmarks-report-github.md`:

- `SectorCount=2000`: `649.8 us` mean plan time, `363.89 KB` managed allocation per frame plan.
- `SectorCount=8000`: `763.3 us` mean plan time, `363.89 KB` managed allocation per frame plan.

Interpretation:

- Planner CPU budget target (`<= 8 ms/frame`) is met in this baseline by a large margin.
- Planner memory behavior is bounded and stable in this baseline.
- Unity GPU pass budget (`<= 8 ms`) still requires capture in Unity Profiler on target hardware.
