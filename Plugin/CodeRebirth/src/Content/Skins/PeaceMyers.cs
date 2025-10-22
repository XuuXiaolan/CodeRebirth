using CodeRebirth.src.Content.Enemies;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.Content.Skins;
public class PeaceMyers : MonoBehaviour
{
    private PeaceKeeper peaceKeeper = null!;

    private float originalChasingSpeed = 0f; 

    public void Awake()
    {
        peaceKeeper = this.transform.parent.GetComponent<PeaceKeeper>();
        originalChasingSpeed = peaceKeeper._chasingSpeed;

        peaceKeeper._walkingSpeed *= 1.5f;
        peaceKeeper.enemyHP = 9999;
    }

    public void Update()
    {
        if (peaceKeeper.isEnemyDead)
            return;

        peaceKeeper._chasingSpeed = peaceKeeper._walkingSpeed;
        peaceKeeper._shootingSpeed = peaceKeeper._walkingSpeed;
        peaceKeeper._damageInterval = 0f;
        if (peaceKeeper._gunParticleSystemGO.activeSelf)
        {
            peaceKeeper._gunParticleSystemGO.SetActive(false);
        }

        if (peaceKeeper.targetPlayer == null)
            return;

        PlayerControllerB targetPlayer = peaceKeeper.targetPlayer;
        float dotProduct = Vector3.Dot(targetPlayer.gameplayCamera.transform.forward, (peaceKeeper.transform.position - targetPlayer.gameplayCamera.transform.position).normalized);
        Debug.Log($"dotProduct: {dotProduct}");
        if (dotProduct <= 0f)
        {
            peaceKeeper._shootingSpeed = originalChasingSpeed * 2f;
            peaceKeeper._chasingSpeed = originalChasingSpeed * 2f;
        }
    }
}