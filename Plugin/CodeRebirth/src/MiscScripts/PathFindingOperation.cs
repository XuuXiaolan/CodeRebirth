using System;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.AI;
using UnityEngine.Pool;
using Unity.Jobs;
using System.Linq;
using System.Collections.Generic;

namespace CodeRebirth.src.MiscScripts;

public class FindPathThroughTeleportsOperation : PathfindingOperation
{
    FindPathJobWrapper[] FindEntrancePointJobs;
    FindPathJobWrapper[] FindDestinationJobs;
    EntranceTeleport[] entranceTeleports;

    public override void Dispose()
    {
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
        Plugin.ExtendedLogging("Starting FindPathThroughTeleportsOperation");
        entranceTeleports = entrancePoints.ToArray();
        FindEntrancePointJobs = new FindPathJobWrapper[entranceTeleports.Length];
        FindDestinationJobs = new FindPathJobWrapper[entranceTeleports.Length];
        JobHandle previousJob = default;
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
        }
    }

    public bool TryGetClosestEntranceTeleport(out EntranceTeleport? entranceTeleport)
    {
        float bestDistance = float.MaxValue;
        entranceTeleport = null;
        for (int i = 0; i < FindEntrancePointJobs.Length; i++)
        {
            if (entranceTeleports[i] == null) continue;
            var statusOfEntranceJob = FindEntrancePointJobs[i].Job.Status[0].GetStatus();
            var statusOfDestinationJob = FindDestinationJobs[i].Job.Status[0].GetStatus();
            if (statusOfEntranceJob == PathQueryStatus.InProgress)
            {
                Plugin.ExtendedLogging($"Entrance job in progress: {i}");
                return false;
            }
            if (statusOfDestinationJob == PathQueryStatus.InProgress)
            {
                Plugin.ExtendedLogging($"destination job in progress: {i}");
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
            if (sum < bestDistance)
            {
                entranceTeleport = entranceTeleports[i];
                bestDistance = sum;
            }
        }
        Dispose();
        Plugin.ExtendedLogging($"Found closest entrance teleport: {entranceTeleport} and is entrance outside: {entranceTeleport?.isEntranceToBuilding}");
        return true;
    }

    public override bool HasDisposed()
    {
        return entranceTeleports.Length == 0;
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