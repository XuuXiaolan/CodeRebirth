using Unity.Netcode;

namespace CodeRebirth.src.Content.Maps;

public class Money : NetworkBehaviour
{
    internal int value = 0;

    public void Start()
    {
        value = 1;
    }

    public void Update()
    {
        if (NetworkObject.IsSpawned && IsServer && StartOfRound.Instance.firingPlayersCutsceneRunning)
        {
            NetworkObject.Despawn();
        }
    }
}