using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace DunGen
{
	public delegate void DungenCharacterDelegate(DungenCharacter character);
	public delegate void CharacterTileChangedEvent(DungenCharacter character, Tile previousTile, Tile newTile);

	/// <summary>
	/// Contains information about the dungeon the character is in
	/// </summary>
	[AddComponentMenu("DunGen/Character")]
	public class DungenCharacter : MonoBehaviour
	{
		#region Statics

		public static event DungenCharacterDelegate CharacterAdded;
		public static event DungenCharacterDelegate CharacterRemoved;

		public static ReadOnlyCollection<DungenCharacter> AllCharacters { get; private set; }
		private static readonly List<DungenCharacter> allCharacters = new List<DungenCharacter>();

		static DungenCharacter()
		{
			AllCharacters = new ReadOnlyCollection<DungenCharacter>(allCharacters);
		}

		#endregion

		public Tile CurrentTile
		{
			get
			{
				if (overlappingTiles == null || overlappingTiles.Count == 0)
					return null;
				else
					return overlappingTiles[overlappingTiles.Count - 1];
			}
		}
		public event CharacterTileChangedEvent OnTileChanged;

		private List<Tile> overlappingTiles;


		protected virtual void OnEnable()
		{
			if (overlappingTiles == null)
				overlappingTiles = new List<Tile>();

			allCharacters.Add(this);

			if (CharacterAdded != null)
				CharacterAdded(this);
		}

		protected virtual void OnDisable()
		{
			allCharacters.Remove(this);

			if (CharacterRemoved != null)
				CharacterRemoved(this);
		}

		internal void ForceRecheckTile()
		{
			overlappingTiles.Clear();

			foreach (var tile in FindObjectsOfType<Tile>())
				if (tile.Placement.Bounds.Contains(transform.position))
				{
					OnTileEntered(tile);
					break;
				}
		}

		protected virtual void OnTileChangedEvent(Tile previousTile, Tile newTile) { }

		internal void OnTileEntered(Tile tile)
		{
			if (overlappingTiles.Contains(tile))
				return;

			var previousTile = CurrentTile;
			overlappingTiles.Add(tile);

			if (CurrentTile != previousTile)
			{
				OnTileChanged?.Invoke(this, previousTile, CurrentTile);
				OnTileChangedEvent(previousTile, CurrentTile);
			}
		}

		internal void OnTileExited(Tile tile)
		{
			if (!overlappingTiles.Contains(tile))
				return;

			var previousTile = CurrentTile;
			overlappingTiles.Remove(tile);

			if (CurrentTile != previousTile)
			{
				OnTileChanged?.Invoke(this, previousTile, CurrentTile);
				OnTileChangedEvent(previousTile, CurrentTile);
			}
		}
	}
}
