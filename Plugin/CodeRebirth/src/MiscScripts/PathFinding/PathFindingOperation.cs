using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;
using UnityEngine.Pool;
using Unity.Jobs;
using System.Linq;
using System.Collections.Generic;

namespace CodeRebirth.src.MiscScripts.PathFinding;

public class FindPathThroughTeleportsOperation : PathfindingOperation
{
    FindPathJobWrapper? FindDirectPathToDestinationJob;
    EntranceTeleport[] entranceTeleports;
    FindPathJobWrapper[] FindEntrancePointJobs;
    FindPathJobWrapper[] FindDestinationJobs;

    public override void Dispose()
    {
        if (FindDirectPathToDestinationJob != null)
        {
            FindPathJobPool.Release(FindDirectPathToDestinationJob);
            FindDirectPathToDestinationJob = null;
        }
        foreach (var jobWrapper in FindEntrancePointJobs)
        {
            FindPathJobPool.Release(jobWrapper);
        }
        foreach (var jobWrapper in FindDestinationJobs)
        {
            FindPathJobPool.Release(jobWrapper);
        }
        entranceTeleports = [];
        FindEntrancePointJobs = [];
        FindDestinationJobs = [];
    }

    public FindPathThroughTeleportsOperation(IEnumerable<EntranceTeleport> entrancePoints, Vector3 startPos, Vector3 endPos, NavMeshAgent agent)
    {
        // Plugin.ExtendedLogging("Starting FindPathThroughTeleportsOperation");
        
        FindDirectPathToDestinationJob = FindPathJobPool.Get();
        FindDirectPathToDestinationJob.Job.Initialize(startPos, endPos, agent);
        JobHandle previousJob = FindDirectPathToDestinationJob.Job.ScheduleByRef();

        entranceTeleports = entrancePoints.ToArray();
        FindEntrancePointJobs = new FindPathJobWrapper[entranceTeleports.Length];
        FindDestinationJobs = new FindPathJobWrapper[entranceTeleports.Length];
        for (int i = 0; i < entranceTeleports.Length; i++)
        {
            if (entranceTeleports[i] == null) continue;
            FindPathJobWrapper findEntrancePointJob = FindPathJobPool.Get();
            FindPathJobWrapper findDestinationJob = FindPathJobPool.Get();
            findEntrancePointJob.Job.Initialize(startPos, entranceTeleports[i].entrancePoint.position, agent);
            findDestinationJob.Job.Initialize(entranceTeleports[i].exitPoint.position, endPos, agent);
            previousJob = findEntrancePointJob.Job.ScheduleByRef(previousJob);
            previousJob = findDestinationJob.Job.ScheduleByRef(previousJob);
            FindEntrancePointJobs[i] = findEntrancePointJob;
            FindDestinationJobs[i] = findDestinationJob;
            // Plugin.ExtendedLogging($"Started job {startPos} -> {entranceTeleports[i].entrancePoint.position}, {entranceTeleports[i].exitPoint.position} -> {endPos}");
        }
    }

    public bool TryGetShortestPath(out bool foundPath, out EntranceTeleport? entranceTeleport)
    {
        float bestDistance = float.MaxValue;
        foundPath = false;
        entranceTeleport = null;

        if (FindDirectPathToDestinationJob == null)
            return false;

        var statusOfDirectPathJob = FindDirectPathToDestinationJob.Job.Status[0].GetStatus();
        if (statusOfDirectPathJob == PathQueryStatus.InProgress)
        {
            // Plugin.ExtendedLogging("Direct path job in progress");
            return false;
        }
        if (statusOfDirectPathJob == PathQueryStatus.Success)
        {
            // Plugin.ExtendedLogging("Direct path job success with length: " + FindDirectPathToDestinationJob.Job.PathLength[0]);
            bestDistance = FindDirectPathToDestinationJob.Job.PathLength[0];
            foundPath = true;
        }
        // Plugin.ExtendedLogging("Starting TryGetShortestPath with this many entrances: " + entranceTeleports.Length);
        for (int i = 0; i < FindEntrancePointJobs.Length; i++)
        {
            if (entranceTeleports[i] == null) continue;
            var statusOfEntranceJob = FindEntrancePointJobs[i].Job.Status[0].GetStatus();
            var statusOfDestinationJob = FindDestinationJobs[i].Job.Status[0].GetStatus();
            // Plugin.ExtendedLogging($"Entrance job status: {statusOfEntranceJob} and destination job status: {statusOfDestinationJob}");
            if (statusOfEntranceJob == PathQueryStatus.InProgress)
            {
                // Plugin.ExtendedLogging($"Entrance job in progress: {i}");
                return false;
            }
            if (statusOfDestinationJob == PathQueryStatus.InProgress)
            {
                // Plugin.ExtendedLogging($"destination job in progress: {i}");
                return false;
            }
            if (statusOfEntranceJob == PathQueryStatus.Failure)
            {
                continue;
            }
            if (statusOfDestinationJob == PathQueryStatus.Failure)
            {
                continue;
            }
            float pathLengthForEntrance = FindEntrancePointJobs[i].Job.PathLength[0];
            float pathLengthForPoint = FindDestinationJobs[i].Job.PathLength[0];
            float sum = pathLengthForPoint + pathLengthForEntrance;
            // Plugin.ExtendedLogging($"Found combined total path for {entranceTeleports[i]} with length: {sum} with entrance length: {pathLengthForEntrance} and destination length: {pathLengthForPoint}");
            if (sum < bestDistance)
            {
                entranceTeleport = entranceTeleports[i];
                bestDistance = sum;
                foundPath = true;
            }
        }
        Dispose();
        // Plugin.ExtendedLogging($"Found closest entrance teleport: {entranceTeleport} and is entrance outside: {entranceTeleport?.isEntranceToBuilding}");
        return true;
    }

    public override bool HasDisposed()
    {
        return FindDirectPathToDestinationJob == null;
    }
}

public abstract class PathfindingOperation : IDisposable
{
    public static readonly ObjectPool<FindPathJobWrapper> FindPathJobPool = new(() => new FindPathJobWrapper(), actionOnDestroy: j => j.Job.Dispose());
    public abstract void Dispose();
    public abstract bool HasDisposed();
    ~PathfindingOperation()
    {
        Dispose();
    }
}