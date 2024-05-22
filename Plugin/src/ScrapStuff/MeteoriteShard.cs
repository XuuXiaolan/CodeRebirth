using CodeRebirth.Misc;
using UnityEngine;

namespace CodeRebirth.ScrapStuff;
public class MeteoriteShard : GrabbableObject {
    ParticleSystem[] particles; 
    bool particlesOn;
    Light emittingLight;
    public override void Start() {
        base.Start();
        particles = GetComponentsInChildren<ParticleSystem>();
        emittingLight = GetComponentInChildren<Light>();

        GetComponent<ScrapValueSyncer>().SetScrapValue(scrapValue);
    }
    public void HandleParticles() {
        if (particlesOn || isPocketed) {
            foreach (ParticleSystem particle in particles) {
                particle.Stop();
                particle.Clear();
            }
            particlesOn = false;
        } else {
            foreach (ParticleSystem particle in particles) {
                particle.Play();
            }
            particlesOn = true;
        }
    }
    public override void ItemActivate(bool used, bool buttonDown = true) {
        base.ItemActivate(used, buttonDown);
        HandleParticles();
    }
    public override void PocketItem() {
        base.PocketItem();
        HandleParticles();
        ToggleEmittingLight(false);
    }
    public override void EquipItem() {
        base.EquipItem();
        ToggleEmittingLight(true);
    }

    void ToggleEmittingLight(bool toggle)
    {
        emittingLight.enabled = toggle;
    }
}