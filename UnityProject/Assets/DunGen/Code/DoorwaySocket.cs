using System;
using UnityEngine;

namespace DunGen
{
	/// <summary>
	/// LEGACY - This used to determine which doorways could connect
	/// It now exists only to allow DunGen to attempt to update to the new system
	/// </summary>
	public enum DoorwaySocketType
	{
		Default,
		Large,
		Vertical,
	}

	public delegate bool SocketConnectionDelegate(DoorwaySocket a, DoorwaySocket b);

	/// <summary>
	/// A class used to determine which doorways can connect to one another
	/// </summary>
	[CreateAssetMenu(fileName = "New Doorway Socket", menuName = "DunGen/Doorway Socket", order = 700)]
	public class DoorwaySocket : ScriptableObject
	{
		public Vector2 Size { get { return size; } }

		[SerializeField]
		private Vector2 size = new Vector2(1, 2);


		#region Static Methods

		[Obsolete("Use DoorwayPairFinder.CustomConnectionRules instead")]
		public static SocketConnectionDelegate CustomSocketConnectionDelegate = null;

		/// <summary>
		/// Checks if two doorway sockets can connect
		/// </summary>
		public static bool CanSocketsConnect(DoorwaySocket a, DoorwaySocket b)
		{
#pragma warning disable 0618
			if (CustomSocketConnectionDelegate != null)
				return CustomSocketConnectionDelegate(a, b);
			else
				return a == b; // By default, sockets can only connect if they match
#pragma warning restore 0618
		}

		#endregion
	}
}
