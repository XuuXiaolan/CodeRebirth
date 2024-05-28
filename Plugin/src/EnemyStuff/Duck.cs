using System;
using System.Collections;
using System.Diagnostics;
using CodeRebirth.Misc;
using CodeRebirth.src;
using CodeRebirth.src.EnemyStuff;
using GameNetcodeStuff;
using Unity.Mathematics;
using UnityEngine;

namespace CodeRebirth.EnemyStuff;
public class Duck : QuestMasterAI
{
    public override void Start() { // Animations and sounds arent here yet so you might get bugs probably lol.
        base.Start();
        if (!IsHost) return;
        creatureVoice.volume = 0.5f;
    }

    protected override void DoCompleteQuest(QuestCompletion reason) {
        base.DoCompleteQuest(reason);
        creatureVoice.volume = 0.25f;
    }
    private IEnumerator QuestSucceedSequence() {
        yield return StartAnimation(Animations.startSucceedQuest);
    }
    private IEnumerator QuestFailSequence(PlayerControllerB failure)
    {
        yield return StartAnimation(Animations.startFailQuest);
        failure.DamagePlayer(500, true, true, CauseOfDeath.Strangulation, 0, false, default);
        creatureSFX.PlayOneShot(questAfterFailClip);
    }

    IEnumerator StartAnimation(Animations animation, int layerIndex = 0, string stateName = "Walking Animation")
    {
        yield return new WaitUntil(() => !creatureSFX.isPlaying);
        DoAnimationClientRpc(animation.ToAnimationName());
        yield return new WaitUntil(() => creatureAnimator.GetCurrentAnimatorStateInfo(layerIndex).IsName(stateName));
    }
}