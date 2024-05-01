namespace DunGen
{
	public readonly struct ProposedConnection
	{
		public DungeonProxy ProxyDungeon { get; }
		public TileProxy PreviousTile { get; }
		public TileProxy NextTile { get; }
		public DoorwayProxy PreviousDoorway { get; }
		public DoorwayProxy NextDoorway { get; }


		public ProposedConnection(DungeonProxy proxyDungeon, TileProxy previousTile, TileProxy nextTile, DoorwayProxy previousDoorway, DoorwayProxy nextDoorway)
		{
			ProxyDungeon = proxyDungeon;
			PreviousTile = previousTile;
			NextTile = nextTile;
			PreviousDoorway = previousDoorway;
			NextDoorway = nextDoorway;
		}
	}
}
