using System.Collections.Generic;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class InstrumentPlayer : GrabbableObject
{
    public AudioSource audioPlayer = null!;

    public static List<InstrumentPlayer> instrumentPlayers = new();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        instrumentPlayers.Add(this);
    }

    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (audioPlayer.isPlaying)
        {
            audioPlayer.Stop();
            return;
        }
        AudioSource? audioSourcePlaying = null;
        foreach (var instrumentPlayer in instrumentPlayers)
        {
            Plugin.ExtendedLogging($"Instrument Player: {instrumentPlayer.name}");
            if (!instrumentPlayer.audioPlayer.isPlaying) continue;
            audioSourcePlaying = instrumentPlayer.audioPlayer;
            break;
        }

        if (audioSourcePlaying != null)
        {
            float time = audioSourcePlaying.time;
            Plugin.ExtendedLogging($"Starting at Time: {time}");
            audioPlayer.Play();
            audioPlayer.time = time;
        }
        else
        {
            audioPlayer.Play();
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        instrumentPlayers.Remove(this);
    }
}