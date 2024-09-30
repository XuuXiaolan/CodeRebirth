using System;
using System.Collections.Generic;
using CodeRebirth.src.Util;

namespace CodeRebirth.src.Content;

// PER HOST SAVE, VALUES ARE SYNCED FROM HOST, ONLY EDITABLE ON HOST.
class CodeRebirthSave(string fileName) : SaveableData(fileName)
{
	public static CodeRebirthSave Current = null!;


	public Dictionary<ulong, CodeRebirthLocalSave> PlayerData { get; private set; } = [];
	
	public override void Save()
	{
		EnsureHost();
		base.Save();
	}

	private void EnsureHost()
	{
		if (!CodeRebirthUtils.Instance.IsHost && !CodeRebirthUtils.Instance.IsServer) throw new InvalidOperationException("Only the host should save CodeRebirthSave.");
	}
}

// PER PLAYER
class CodeRebirthLocalSave
{

}