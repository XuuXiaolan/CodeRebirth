using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Experimental.AI;
using UnityEngine;
using UnityEngine.AI;
using Unity.Jobs.LowLevel.Unsafe;
using System;

namespace CodeRebirth.src.MiscScripts;

public class FindPathJobWrapper
{
    public FindPathJobWrapper()
    {
        Job = new FindPathJob();
    }
    public FindPathJob Job;
}

public struct FindPathJob : IJob, IDisposable
{
    [NativeDisableContainerSafetyRestriction] private static NativeArray<NavMeshQuery> StaticThreadQueries;

    private const float MAX_ORIGIN_DISTANCE = 5;
    private const float MAX_ENDPOINT_DISTANCE = 1.5f;
    private const float MAX_ENDPOINT_DISTANCE_SQR = MAX_ENDPOINT_DISTANCE * MAX_ENDPOINT_DISTANCE;

    [ReadOnly] internal int AgentTypeID;
    [ReadOnly] internal int AreaMask;
    [ReadOnly] internal Vector3 Origin;
    [ReadOnly] internal Vector3 Destination;

    [ReadOnly, NativeSetThreadIndex] internal int ThreadIndex;

    [ReadOnly, NativeDisableContainerSafetyRestriction] internal NativeArray<NavMeshQuery> ThreadQueriesRef;

    [WriteOnly] internal NativeArray<PathQueryStatus> Status;
    [WriteOnly] internal NativeArray<float> PathLength;

    internal void Initialize(Vector3 origin, Vector3 destination, NavMeshAgent agent)
    {
        AgentTypeID = agent.agentTypeID;
        AreaMask = agent.areaMask;
        Origin = origin;
        Destination = destination;
        CreateStaticAllocations();
        ThreadQueriesRef = StaticThreadQueries;

        Status = new NativeArray<PathQueryStatus>(1, Allocator.Persistent);
        Status[0] = PathQueryStatus.InProgress;
        PathLength = new NativeArray<float>(1, Allocator.Persistent);
        PathLength[0] = float.MaxValue;
    }

    private static void CreateStaticAllocations()
    {
        var threadCount = JobsUtility.ThreadIndexCount;
        if (StaticThreadQueries.Length == threadCount)
        {
            return;
        }

        DisposeStaticAllocations();

        StaticThreadQueries = new(threadCount, Allocator.Persistent);
        for (var i = 0; i < StaticThreadQueries.Length; i++)
            StaticThreadQueries[i] = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.Persistent, Pathfinding.MAX_PATH_SIZE);

        Application.quitting += DisposeStaticAllocations;
    }

    public void Execute()
    {
        var query = ThreadQueriesRef[ThreadIndex];

        var originExtents = new Vector3(MAX_ORIGIN_DISTANCE, MAX_ORIGIN_DISTANCE, MAX_ORIGIN_DISTANCE);
        var origin = query.MapLocation(Origin, originExtents, AgentTypeID, AreaMask);
        if (!query.IsValid(origin.polygon))
        {
            Status[0] = PathQueryStatus.Failure;
            return;
        }

        var destinationExtents = new Vector3(MAX_ENDPOINT_DISTANCE, MAX_ENDPOINT_DISTANCE, MAX_ENDPOINT_DISTANCE);
        var destinationLocation = query.MapLocation(Destination, destinationExtents, AgentTypeID, AreaMask);
        if (!query.IsValid(destinationLocation))
        {
            Status[0] = PathQueryStatus.Failure;
            return;
        }

        query.BeginFindPath(origin, destinationLocation, AreaMask);

        PathQueryStatus status = PathQueryStatus.InProgress;
        while (status.GetStatus() == PathQueryStatus.InProgress)
            status = query.UpdateFindPath(int.MaxValue, out int _);

        if (status.GetStatus() != PathQueryStatus.Success)
        {
            Status[0] = status;
            return;
        }

        var pathNodes = new NativeArray<PolygonId>(Pathfinding.MAX_PATH_SIZE, Allocator.Temp);
        status = query.EndFindPath(out var pathNodesSize);
        query.GetPathResult(pathNodes);

        using var path = new NativeArray<NavMeshLocation>(Pathfinding.MAX_STRAIGHT_PATH, Allocator.Temp);
        var straightPathStatus = Pathfinding.FindStraightPath(query, Origin, Destination, pathNodes, pathNodesSize, path, out var pathSize);
        pathNodes.Dispose();
        if (straightPathStatus.GetStatus() != PathQueryStatus.Success)
        {
            Status[0] = status;
            return;
        }

        // Check if the end of the path is close enough to the target.
        var endPosition = path[pathSize - 1].position;
        var endDistance = (endPosition - Destination).sqrMagnitude;
        if (endDistance > MAX_ENDPOINT_DISTANCE_SQR)
        {
            Status[0] = PathQueryStatus.Failure;
            return;
        }

        var firstCorner = path[0];
        var distance = 0f;
        for (var i = 1; i < pathSize; i++)
        {
            distance += Vector3.Distance(path[i].position, path[i - 1].position);
        }
        PathLength[0] = distance;

        Status[0] = PathQueryStatus.Success;
    }

    private static void DisposeStaticAllocations()
    {
        foreach (var query in StaticThreadQueries)
            query.Dispose();

        StaticThreadQueries.Dispose();

        Application.quitting -= DisposeStaticAllocations;
    }

    public void Dispose()
    {
        Status.Dispose();
        PathLength.Dispose();
    }
}
