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
}
