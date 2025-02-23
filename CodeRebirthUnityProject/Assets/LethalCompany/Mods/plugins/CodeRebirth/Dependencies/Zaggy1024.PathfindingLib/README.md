# PathfindingLib

A library for Lethal Company mods (and probably mods for any other game using Unity's AI Navigation package) to run pathfinding off the main thread.

A high-level API is provided to allow convenient calculation of single paths off the main thread.

Synchronization functions are provided to prevent modifications to the navmesh while searching for a path on a non-main thread. If such a thread reads from the navmesh without synchronizing first, the engine may access invalid memory and crash. See the [Threading Safety](#threading-safety) section.

### This readme is a work in progress!

Feel free to submit suggestions for clarifications or improvements as issues or pull requests.

## Usage

In order to run pathfinding off the main thread, the library provides a pre-built job called `FindPathJob` that can be used to calculate a path that the provided `NavMeshAgent` can traverse from a starting position to an ending position.

To do so, you will need to request a job from the pool of jobs:

```cs
var pooledJob = JobPools.GetFindPathJob();
```

Then you will need to initialize the job with the data it needs to run off the main thread:

```cs
pooledJob.Job.Initialize(origin, destination, agent);
```

If you are simply finding a path from the agent to a destination, it is recommended to use the `GetPathOrigin()` extension method to determine where such a path should begin, ensuring that the path succeeds even if the agent is on a navmesh link:

```cs
var origin = agent.GetPathOrigin();
pooledJob.Job.Initialize(origin, destination, agent);
```

After the job is initialized, you should schedule your job via `ScheduleByRef()` to run whenever there are job threads available, then store the job to check the status later. When scheduling, it is preferred to ensure that your job runs after any previous jobs you have scheduled for the agent:

```cs
var previousJobHandle = default(JobHandle);
for (var i = 0; i < destinations.Length; i++) {
    var pooledJob = JobPools.GetFindPathJob();
    var origin = agent.GetPathOrigin();
    pooledJob.Job.Initialize(origin, destinations[i], agent);
    previousJobHandle = pooledJob.Job.ScheduleByRef(previousJobHandle);
    pathJobs[i] = pooledJob;    // Store the job to query status later
}
```

Then, you can check the status using `GetStatus()`, and check the path length using `GetPathLength()`:

```cs
var result = pooledJob.Job.GetStatus().GetResult(); // GetResult() removes detail flags from the status
if (result != PathQueryStatus.InProgress) {
    var pathReachedDestination = result == PathQueryStatus.Success;
    if (pathReachedDestination) {
        var pathLength = pooledJob.Job.GetPathLength();
        // Use the result of the job here
    }
}
```

In order to not leak jobs, ensure that you release the job back to the pool when you don't need it anymore:

```cs
JobPools.ReleaseFindPathJob(pooledJob);
```

If you are checking a large number of paths, it may be preferable for you to implement your own job to run through an array of destinations, rather than scheduling multiple individual `FindPathJob` instances. The code in `FindPathJob` can be adapted to implement `IJobFor` instead of `IJob`, and only grow arrays as necessary to fit the input data. However, you will need to ensure that you use the safeties outlined in the section on [Threading Safety](#threading-safety).

## Threading Safety

If you choose to implement your own Unity Job to calculate a path off the main thread instead of using the provided `FindPathJob`, any calls to `NavMeshQuery` methods should be preceded by a call to `NavMeshLock.BeginRead()`, which will block the thread until the navmesh is safe to read without causing crashes. Then, on all code branches following the start of the read, `NavMeshLock.EndRead()` must be called, or the main thread will be deadlocked when it reaches the next AI update.

Here is a simple excerpt of how this can be handled:

```cs
// Block until the navmesh is safely readable.
NavMeshLock.BeginRead();

var status = query.BeginFindPath(origin, destination, areaMask);
if (status.GetStatus() == PathQueryStatus.Failure)
{
    // We are returning early. Release our read lock on the navmesh to unblock
    // the main thread.
    NavMeshLock.EndRead();
    return status;
}

while (status.GetStatus() == PathQueryStatus.InProgress)
    status = query.UpdateFindPath(int.MaxValue, out int _);

status = query.EndFindPath(out var pathNodesSize);

if (status.GetStatus() != PathQueryStatus.Success)
{
    // Another early return, release the lock.
    NavMeshLock.EndRead();
    return status;
}

var pathNodes = new NativeArray<PolygonId>(pathNodesSize, Allocator.Temp);
query.GetPathResult(pathNodes);

// Release the lock once we are done with all code that needs access to our
// NavMeshQuery.
NavMeshLock.EndRead();
```

Note that while the read lock is held, the main thread cannot advance past the start of the AIUpdate subsystem. This happens fairly early in the frame, so there may not be enough time between the start of the frame and the AIUpdate subsystem for a long path to complete and free up the navmesh locks. This will result in a slight delay in a game frame, and can potentially by reducing the number of iterations calculated in the call to `NavMeshQuery.UpdateFindPath()`:

```cs
while (status.GetStatus() == PathQueryStatus.InProgress)
{
    NavMeshLock.EndRead();
    NavMeshLock.BeginRead();
    status = query.UpdateFindPath(128, out int _);
}
```

This should allow the main thread to resume sooner each frame if there are jobs running. However, calculating the entire path in a frame seems to usually only take up to 0.6ms on the high end, so generally it is not very noticeable when the main thread is delayed by this.
