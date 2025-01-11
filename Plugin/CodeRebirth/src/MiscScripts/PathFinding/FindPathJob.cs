using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Experimental.AI;
using UnityEngine;
using UnityEngine.AI;
using Unity.Jobs.LowLevel.Unsafe;
using System;
using System.Collections.Concurrent;

namespace CodeRebirth.src.MiscScripts.PathFinding;

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
    private static ConcurrentDictionary<int, object> RunningThreads = [];
    [NativeDisableContainerSafetyRestriction] private static NativeArray<NavMeshQuery> StaticThreadQueries;

    private const float MAX_ORIGIN_DISTANCE = 5;
    private const float MAX_ENDPOINT_DISTANCE = 1.5f;
    private const float MAX_ENDPOINT_DISTANCE_SQR = MAX_ENDPOINT_DISTANCE * MAX_ENDPOINT_DISTANCE;

    [ReadOnly, NativeDisableContainerSafetyRestriction] internal NativeArray<NavMeshQuery> ThreadQueriesRef;

    [ReadOnly] internal int AgentTypeID;
    [ReadOnly] internal int AreaMask;
    [ReadOnly] internal Vector3 Origin;
    [ReadOnly] internal Vector3 Destination;

    [ReadOnly, NativeSetThreadIndex] internal int ThreadIndex;

    [WriteOnly] internal NativeArray<PathQueryStatus> Status;
    [WriteOnly] internal NativeArray<float> PathLength;

    internal void Initialize(Vector3 origin, Vector3 destination, NavMeshAgent agent)
    {
        CreateQueries();
        ThreadQueriesRef = StaticThreadQueries;

        AgentTypeID = agent.agentTypeID;
        AreaMask = agent.areaMask;
        Origin = origin;
        Destination = destination;

        Status = new NativeArray<PathQueryStatus>(1, Allocator.Persistent);
        Status[0] = PathQueryStatus.InProgress;
        PathLength = new NativeArray<float>(1, Allocator.Persistent);
        PathLength[0] = float.MaxValue;
    }

    private static void CreateQueries()
    {
        var threadCount = JobsUtility.ThreadIndexCount;
        if (StaticThreadQueries.Length >= threadCount)
            return;

        Application.quitting -= DisposeQueries;

        var newQueries = new NativeArray<NavMeshQuery>(threadCount, Allocator.Persistent);
        for (var i = 0; i < StaticThreadQueries.Length; i++)
            newQueries[i] = StaticThreadQueries[i];
        for (var i = StaticThreadQueries.Length; i < threadCount; i++)
            newQueries[i] = new NavMeshQuery(NavMeshWorld.GetDefaultWorld(), Allocator.Persistent, Pathfinding.MAX_PATH_SIZE);
        StaticThreadQueries.Dispose();
        StaticThreadQueries = newQueries;

        Application.quitting += DisposeQueries;
    }

    private static void DisposeQueries()
    {
        foreach (var query in StaticThreadQueries)
            query.Dispose();

        StaticThreadQueries.Dispose();

        Application.quitting -= DisposeQueries;
    }

    private class ScopeGuard(Action action) : IDisposable
    {
        private Action? action = action;
        public void Dispose()
        {
            action?.Invoke();
            action = null;
        }
    }

    public void Execute()
    {
        var query = ThreadQueriesRef[ThreadIndex];
        var threadIndex = ThreadIndex;
        if (!RunningThreads.TryAdd(threadIndex, new object()))
        {
            Plugin.Logger.LogError($"Using {threadIndex} twice");
        }

        using var queryScopeGuard = new ScopeGuard(() =>
        {
            if (!RunningThreads.TryRemove(threadIndex, out _))
                Plugin.Logger.LogError($"Thread not running ?!?!?! {threadIndex}");
        });

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

        var status = query.BeginFindPath(origin, destinationLocation, AreaMask);
        if (status.GetStatus() == PathQueryStatus.Failure)
        {
            Status[0] = PathQueryStatus.Failure;
            return;
        }

        while (status.GetStatus() == PathQueryStatus.InProgress)
        {
            status = query.UpdateFindPath(int.MaxValue, out int _);
        }

        status = query.EndFindPath(out var pathNodesSize);
        if (status.GetStatus() != PathQueryStatus.Success)
        {
            Status[0] = PathQueryStatus.Failure;
            return;
        }

        var pathNodes = new NativeArray<PolygonId>(pathNodesSize, Allocator.Temp);
        query.GetPathResult(pathNodes);

        using var path = new NativeArray<NavMeshLocation>(Pathfinding.MAX_STRAIGHT_PATH, Allocator.Temp);
        var straightPathStatus = Pathfinding.FindStraightPath(query, Origin, Destination, pathNodes, pathNodesSize, path, out var pathSize);
        pathNodes.Dispose();

        if (straightPathStatus.GetStatus() != PathQueryStatus.Success)
        {
            Status[0] = straightPathStatus;
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

        var distance = 0f;
        for (var i = 1; i < pathSize; i++)
        {
            distance += Vector3.Distance(path[i - 1].position, path[i].position);
        }
        PathLength[0] = distance;

        Status[0] = PathQueryStatus.Success;
    }

    public void Dispose()
    {
        Status.Dispose();
        PathLength.Dispose();
    }
}
