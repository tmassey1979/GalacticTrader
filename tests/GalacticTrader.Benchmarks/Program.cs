using BenchmarkDotNet.Running;
using GalacticTrader.Benchmarks;

var runArgs = args.ToList();
if (!runArgs.Contains("--filter", StringComparer.OrdinalIgnoreCase))
{
    runArgs.Add("--filter");
    runArgs.Add("*");
}

BenchmarkSwitcher.FromAssembly(typeof(RoutePlanningBenchmarks).Assembly).Run(runArgs.ToArray());
