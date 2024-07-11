using UnityEngine;

namespace CodeRebirth.Misc;
public class MoveStunnedParticle : MonoBehaviour
{
    public GameObject ParticleObject = null!;
    public GameObject ParticleSocket = null!;

    void Update()
    {
        ParticleObject.transform.position = ParticleSocket.transform.position;
    }

    public void ShowStunnedParticle(){
        ParticleObject.SetActive(true);
    }
    public void HideStunnedParticle(){
        ParticleObject.SetActive(false);
    }
}
