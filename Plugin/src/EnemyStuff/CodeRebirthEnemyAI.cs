using System;
using System.Collections.Generic;
using System.Text;
using static CodeRebirth.EnemyStuff.Duck;
using Unity.Netcode;
using CodeRebirth.Misc;
using System.Diagnostics;

namespace CodeRebirth.src.EnemyStuff
{
    public abstract class CodeRebirthEnemyAI : EnemyAI
    {
        [Conditional("DEBUG")]
        public void LogIfDebugBuild(object text)
        {
            Plugin.Logger.LogInfo(text);
        }

        [ClientRpc]
        public void DoAnimationClientRpc(string triggerName)
        {
            LogIfDebugBuild(triggerName);
            creatureAnimator.SetTrigger(triggerName);
        }

        public void ToggleEnemySounds(bool toggle)
        {
            creatureSFX.enabled = toggle;
            creatureVoice.enabled = toggle;
        }
    }
}
