namespace GalacticTrader.IntegrationTests;

using System.Diagnostics;
using System.Net;
using System.Text;

public sealed class PerformanceRegressionIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PerformanceRegressionIntegrationTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });
    }

    [Fact]
    [Trait("Category", "Performance")]
    public async Task RepresentativeApiEndpoints_P95Latency_Under150Milliseconds()
    {
        var endpoints = new (string Path, HttpStatusCode ExpectedStatus)[]
        {
            ("/api/navigation/sectors", HttpStatusCode.OK),
            ("/api/strategic/volatility", HttpStatusCode.OK),
            ("/api/leaderboards/wealth", HttpStatusCode.OK)
        };

        foreach (var endpoint in endpoints)
        {
            for (var warmup = 0; warmup < 5; warmup++)
            {
                var response = await _client.GetAsync(endpoint.Path);
                Assert.Equal(endpoint.ExpectedStatus, response.StatusCode);
            }
        }

        var byEndpointMeasurements = endpoints.ToDictionary(
            endpoint => endpoint.Path,
            _ => new List<double>());
        var allMeasurements = new List<double>();

        const int iterations = 90;
        for (var index = 0; index < iterations; index++)
        {
            var endpoint = endpoints[index % endpoints.Length];
            var stopwatch = Stopwatch.StartNew();
            var response = await _client.GetAsync(endpoint.Path);
            stopwatch.Stop();

            Assert.Equal(endpoint.ExpectedStatus, response.StatusCode);

            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            byEndpointMeasurements[endpoint.Path].Add(elapsedMs);
            allMeasurements.Add(elapsedMs);
        }

        var overallP95 = CalculatePercentile(allMeasurements, 95);
        Assert.True(
            overallP95 < 150d,
            $"Expected overall API p95 <150ms, actual: {overallP95:0.00}ms. Breakdown: {BuildBreakdown(byEndpointMeasurements)}");
    }

    private static double CalculatePercentile(IReadOnlyList<double> values, int percentile)
    {
        if (values.Count == 0)
        {
            return 0d;
        }

        var ordered = values.OrderBy(value => value).ToArray();
        var index = (int)Math.Ceiling((percentile / 100d) * ordered.Length) - 1;
        index = Math.Clamp(index, 0, ordered.Length - 1);
        return ordered[index];
    }

    private static string BuildBreakdown(IReadOnlyDictionary<string, List<double>> byEndpointMeasurements)
    {
        var builder = new StringBuilder();
        foreach (var endpoint in byEndpointMeasurements.OrderBy(entry => entry.Key))
        {
            if (builder.Length > 0)
            {
                builder.Append(" | ");
            }

            var p95 = CalculatePercentile(endpoint.Value, 95);
            var avg = endpoint.Value.Average();
            builder.Append($"{endpoint.Key}: p95={p95:0.00}ms avg={avg:0.00}ms");
        }

        return builder.ToString();
    }
}
