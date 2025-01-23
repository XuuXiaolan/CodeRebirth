## Version 0.0.5
- Reverted an unintentional change to the plugin's GUID string.

## Version 0.0.4
- Made the plugin GUID public for convenient hard dependency setup.

## Version 0.0.3
- Renamed the Plugin class to PathfindingLibPlugin.

## Version 0.0.2
- Replaced the icon with a new placeholder that will totally not stay indefinitely...

## Version 0.0.1
Initial version. Public-facing API includes:
- `FindPathJob`: A simple job to find a valid path for an agent to traverse between a start and end position.
- `JobPools`: A static class providing pooled `FindPathJob` instances that can be reused by any API users.
- `NavMeshLock`: Provides methods to prevent crashes when running pathfinding off the main thread.
- `PathfindingJobSharedResources`: A static class that provides a `NativeArray<NavMeshQuery>` that can be passed to a job to access a thread-specific instance of `NavMeshQuery`.
- `AgentExtensions.GetAgentPathOrigin(this NavMeshAgent)`: Gets the position that paths originating from an agent should start from. This avoids pathing failure when crossing links.
- `Pathfinding.FindStraightPath(...)`: Gets a straight path from the result of a NavMeshQuery.
