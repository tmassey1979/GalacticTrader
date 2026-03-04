using GalacticTrader.ClientSdk.Starmap;
using System.Collections.Generic;
using UnityEngine;

namespace GalacticTrader.Unity.Starmap;

public sealed class UnityStarmapStreamingController : MonoBehaviour
{
    [Header("Streaming")]
    [SerializeField] private float viewDistance = 220f;
    [SerializeField] private float chunkSize = 80f;
    [SerializeField] private int baseChunkRadius = 2;
    [SerializeField] private int maxRenderedSectors = 480;
    [SerializeField] private int maxRenderedRoutes = 1200;
    [SerializeField] private int maxActiveChunks = 125;

    [Header("LOD Distances")]
    [SerializeField] private float nearLodDistance = 80f;
    [SerializeField] private float midLodDistance = 220f;

    private StarmapStreamingPlanner? _planner;

    public StarmapFramePlan? LastFramePlan { get; private set; }

    public void Configure(
        IReadOnlyList<StarmapSectorNode> sectors,
        IReadOnlyList<StarmapRouteEdge> routes)
    {
        var chunking = new StarmapChunkingOptions(
            ChunkSize: Mathf.Max(1f, chunkSize),
            BaseChunkRadius: Mathf.Max(0, baseChunkRadius));
        var budget = new StarmapRenderBudget(
            MaxRenderedSectors: Mathf.Max(1, maxRenderedSectors),
            MaxRenderedRoutes: Mathf.Max(0, maxRenderedRoutes),
            MaxActiveChunks: Mathf.Max(1, maxActiveChunks));
        var lodBands = new StarmapLodBands(
            NearDistance: Mathf.Max(1f, nearLodDistance),
            MidDistance: Mathf.Max(nearLodDistance + 1f, midLodDistance));

        var index = StarmapChunkIndex.Build(sectors, routes, chunking);
        _planner = new StarmapStreamingPlanner(index, budget, lodBands);
    }

    public StarmapFramePlan? EvaluateFrame(Vector3 cameraPosition)
    {
        if (_planner is null)
        {
            return null;
        }

        var camera = new StarmapCameraState(
            Position: new MapPoint3(cameraPosition.x, cameraPosition.y, cameraPosition.z),
            ViewDistance: Mathf.Max(0f, viewDistance));

        LastFramePlan = _planner.PlanFrame(camera);
        return LastFramePlan;
    }
}
