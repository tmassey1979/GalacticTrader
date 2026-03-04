# Performance Regression Gate

This document defines the automated performance gates enforced in CI and the deeper benchmark run cadence.

## CI Performance Gates (Pull Requests + Main)

`dotnet-ci.yml` executes targeted performance tests that fail the build on regression:

1. Route planning service average latency
- Test: `RoutePlanningServiceTests.CalculateRouteAsync_Performance_AverageUnder20Milliseconds`
- Threshold: average `< 20ms`

2. Combat tick service average latency
- Test: `CombatServiceTests.ProcessTickAsync_Performance_Under50MillisecondsAverage`
- Threshold: average `< 50ms`

3. Representative API endpoint latency
- Test: `PerformanceRegressionIntegrationTests.RepresentativeApiEndpoints_P95Latency_Under150Milliseconds`
- Threshold: overall p95 `< 150ms`
- Test failure output includes per-endpoint p95/average breakdown for actionability.

## Benchmark Workflow (Scheduled + Manual)

`performance-benchmarks.yml` runs BenchmarkDotNet in short-job mode:

- Schedule: weekly (Monday 07:00 UTC)
- Trigger: `workflow_dispatch` supported for ad-hoc runs
- Artifact: BenchmarkDotNet JSON outputs uploaded from `tests/GalacticTrader.Benchmarks/BenchmarkDotNet.Artifacts`

## Methodology Notes

- CI gate tests are lightweight regression guards, not full-scale capacity tests.
- Benchmarks are comparative trend signals across commits, not absolute production SLO certification.
- Warm-up iterations are included in API p95 integration tests before measurement sampling.

## Hardware/Environment Assumptions

- CI gates run on GitHub-hosted `windows-latest` runners.
- Measured values include runner variance; thresholds are set to be stable while still catching regressions.
- Production hardware and runtime tuning will differ; use staging load tests for release readiness sign-off.
