using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DunGen
{
	public enum TilePlacementResult
	{
		None,

		// Retry Codes
		NoFromDoorway,
		NoTilesWithMatchingDoorway,
		NoValidTile,
		TemplateIsNull,
		NoMatchingDoorwayInTile,
		TileIsColliding,
		NewTileIsNull,
		OutOfBounds,
	}
}
