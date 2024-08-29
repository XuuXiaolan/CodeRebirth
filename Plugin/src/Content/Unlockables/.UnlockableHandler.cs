using CodeRebirth.src.Util.AssetLoading;
using CodeRebirth.src.Util;
using UnityEngine;
using LethalLib.Modules;
using LethalLib.Extras;

namespace CodeRebirth.src.Content.Unlockables;
public class UnlockableHandler : ContentHandler<UnlockableHandler> {
	public class ShockwaveBotAssets(string bundleName) : AssetBundleLoader<ShockwaveBotAssets>(bundleName) {
		[LoadFromBundle("ShockwaveBotUnlockable.asset")]
		public UnlockableItemDef ShockWaveBotUnlockable { get; private set; } = null!;
	}

	public ShockwaveBotAssets ShockwaveBot { get; private set; } = null!;

    public UnlockableHandler() {
		if (false) RegisterShockWaveGal();
	}

    private void RegisterShockWaveGal() {
        ShockwaveBot = new ShockwaveBotAssets("shockwavebotassets");
        LethalLib.Modules.Unlockables.RegisterUnlockable(ShockwaveBot.ShockWaveBotUnlockable, 999, StoreType.ShipUpgrade);
    }
}