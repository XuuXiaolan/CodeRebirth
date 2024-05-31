using Unity.Netcode;
using System.Diagnostics;
using GameNetcodeStuff;
using UnityEngine;

namespace CodeRebirth.src.EnemyStuff
{
    public abstract class CodeRebirthEnemyAI : EnemyAI
    {
        public override void Start()
        {
            base.Start();
            LogIfDebugBuild(enemyType.enemyName + " Spawned.");
        }
        
        [Conditional("DEBUG")]
        public void LogIfDebugBuild(object text)
        {
            Plugin.Logger.LogInfo(text);
        }

        [ClientRpc]
        public void DoAnimationClientRpc(string triggerName)
        {
            DoAnimationOnLocalClient(triggerName);
        }

        public void DoAnimationOnLocalClient(string triggerName)
        {
            LogIfDebugBuild(triggerName);
            creatureAnimator.SetTrigger(triggerName);
        }

        public void ToggleEnemySounds(bool toggle)
        {
            creatureSFX.enabled = toggle;
            creatureVoice.enabled = toggle;
        }
        [ClientRpc]
        public void ChangeSpeedClientRpc(float speed)
        {
            ChangeSpeedOnLocalClient(speed);
        }

        public void ChangeSpeedOnLocalClient(float speed)
        {
            agent.speed = speed;
        }
        public bool FindClosestPlayerInRange(float range) {
            PlayerControllerB closestPlayer = null;
            float minDistance = float.MaxValue;

            foreach (PlayerControllerB player in StartOfRound.Instance.allPlayerScripts) {
                bool onSight = player.IsSpawned && player.isPlayerControlled && !player.isPlayerDead && !player.isInHangarShipRoom && EnemyHasLineOfSightToPosition(player.transform.position, 60f, range);
                if (!onSight) continue;

                float distance = Vector3.Distance(transform.position, player.transform.position);
                bool closer = distance < minDistance;
                if (!closer) continue;

                minDistance = distance;
                closestPlayer = player;
            }
            if (closestPlayer == null) return false;

            targetPlayer = closestPlayer;
            return true;
        }

        public bool EnemyHasLineOfSightToPosition(Vector3 pos, float width = 60f, float range = 20f, float proximityAwareness = 5f) {
            if (eye == null) {
                _ = transform;
            } else {
                _ = eye;
            }

            if (Vector3.Distance(eye.position, pos) >= range || Physics.Linecast(eye.position, pos, StartOfRound.Instance.collidersAndRoomMaskAndDefault)) return false;

            Vector3 to = pos - eye.position;
            return Vector3.Angle(eye.forward, to) < width || Vector3.Distance(transform.position, pos) < proximityAwareness;
        }
    }
}
