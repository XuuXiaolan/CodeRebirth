using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace CodeRebirth.MapStuff;
public class CorruptionManager : NetworkBehaviour {
    public DecalProjector corruptionProjector = null!;
    public DecalProjector crimsonProjector = null!;
    public DecalProjector hallowProjector = null!;

    private DecalProjector activeProjector = null!;
    private System.Random random = new System.Random(69);
    public void Start() {
        if (StartOfRound.Instance != null) {
            random = new System.Random(StartOfRound.Instance.randomMapSeed + 85);
        }

        switch (random.Next(0, 3)) {
            case 0:
                corruptionProjector.gameObject.SetActive(true);
                activeProjector = corruptionProjector;
                break;
            case 1:
                crimsonProjector.gameObject.SetActive(true);
                activeProjector = crimsonProjector;
                break;
            case 2:
                hallowProjector.gameObject.SetActive(true);
                activeProjector = hallowProjector;
                break;
        }
    }

    public void Update() {
        if (activeProjector == null) return;

        activeProjector.gameObject.transform.localScale += new Vector3(Time.deltaTime * 0.3f, Time.deltaTime * 0.3f, Time.deltaTime * 0.3f);
    }
}