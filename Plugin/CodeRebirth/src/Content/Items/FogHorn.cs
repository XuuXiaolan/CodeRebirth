using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class FogHorn : GrabbableObject
{
    public AudioSource audioSource = null!;
    public AudioClip useSound = null!;
    public AudioClip failSound = null!;

    private NetworkVariable<int> timesUsed = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

	public override int GetItemDataToSave()
	{
		base.GetItemDataToSave();
		return timesUsed.Value;
	}

	public override void LoadItemSaveData(int saveData)
	{
		base.LoadItemSaveData(saveData);
		timesUsed.Value = saveData;
	}

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        TryActivateFogHornServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void TryActivateFogHornServerRpc()
    {
        int failChance = 5 + timesUsed.Value * 5;
        if (Random.Range(0, 100) < failChance)
        {
            DoSucceedOrFailClientRpc(false);
            return;
        }

        WeatherRegistry.WeatherController.ChangeCurrentWeather(LevelWeatherType.None);
        timesUsed.Value++;
        DoSucceedOrFailClientRpc(true);
    }

    [ClientRpc]
    private void DoSucceedOrFailClientRpc(bool succeed)
    {
        if (succeed)
        {
            HUDManager.Instance.ShakeCamera(ScreenShakeType.VeryStrong);
            audioSource.PlayOneShot(useSound);
        }
        else
        {
            audioSource.PlayOneShot(failSound);
        }
    }
}