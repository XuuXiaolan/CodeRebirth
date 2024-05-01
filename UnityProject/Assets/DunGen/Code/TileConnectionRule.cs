using System;

namespace DunGen
{
	public sealed class TileConnectionRule
	{
		/// <summary>
		/// The result of evaluating a TileConnectionRule
		/// </summary>
		public enum ConnectionResult
		{
			/// <summary>
			/// The connection is allowed
			/// </summary>
			Allow,
			/// <summary>
			/// The connection is not allowed
			/// </summary>
			Deny,
			/// <summary>
			/// Let any lower priority rules decide whether this connection is allowed or not
			/// </summary>
			Passthrough,
		}

		public delegate ConnectionResult CanTilesConnectDelegate(Tile previousTile, Tile nextTile, Doorway previousDoorway, Doorway nextDoorway);
		public delegate ConnectionResult TileConnectionDelegate(ProposedConnection connection);

		/// <summary>
		/// This rule's prioty. Higher priority rules are evaluated first. Lower priority rules are
		/// only evaluated if the delegate returns 'Passthrough' as the result
		/// </summary>
		public int Priority = 0;

		/// <summary>
		/// The delegate to evaluate to determine if two tiles can connect using a given doorway pairing.
		/// Returning 'Passthrough' will allow lower priority rules to evaluate. If no rule handles the connection,
		/// the default method is used (only matching doorways are allowed to connect).
		/// </summary>
		[Obsolete("Use ConnectionDelegate instead")]
		public CanTilesConnectDelegate Delegate;

		/// <summary>
		/// The delegate to evaluate to determine if two tiles can connect using a given doorway pairing.
		/// Returning 'Passthrough' will allow lower priority rules to evaluate. If no rule handles the connection,
		/// the default method is used (only matching doorways are allowed to connect).
		/// </summary>
		public TileConnectionDelegate ConnectionDelegate;


		[Obsolete("Use the constructor that takes a delegate of type 'TileConnectionDelegate' instead")]
		public TileConnectionRule(CanTilesConnectDelegate connectionDelegate, int priority = 0)
		{
			Delegate = connectionDelegate;
			Priority = priority;
		}

		public TileConnectionRule(TileConnectionDelegate connectionDelegate, int priority = 0)
		{
			ConnectionDelegate = connectionDelegate;
			Priority = priority;
		}
	}
}
