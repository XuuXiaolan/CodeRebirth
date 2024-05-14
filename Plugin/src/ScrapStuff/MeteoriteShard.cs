using CodeRebirth.Misc;
using UnityEngine;

namespace CodeRebirth.ScrapStuff;
public class MeteoriteShard : GrabbableObject {
    private ParticleSystem[] particles; 
    private bool particlesOn;
    public override void Start() {
        base.Start();
        particles = GetComponentsInChildren<ParticleSystem>();

        GetComponent<ScrapValueSyncer>().SetScrapValue(scrapValue);
    }
    public void HandleParticles() {
        if (particlesOn || isPocketed) {
            foreach (ParticleSystem particle in particles) {
                particle.Stop();
                particle.Clear();
                this.transform.Find("MagicFire").GetComponentInChildren<ParticleSystem>().Stop();
                this.transform.Find("MagicFireBlue").GetComponentInChildren<ParticleSystem>().Stop();
                this.transform.Find("MagicFire").GetComponentInChildren<ParticleSystem>().Clear();
                this.transform.Find("MagicFireBlue").GetComponentInChildren<ParticleSystem>().Clear();
            }
            particlesOn = false;
        } else {
            foreach (ParticleSystem particle in particles) {
                particle.Play();
                if (particle.transform.parent.name == "MagicFire" || particle.transform.parent.name == "MagicFireBlue") {
                    this.transform.Find("MagicFire").GetComponentInChildren<ParticleSystem>().Play();
                    this.transform.Find("MagicFireBlue").GetComponentInChildren<ParticleSystem>().Play();
                }
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
        this.transform.GetComponentInChildren<Light>().enabled = false;
    }
    public override void EquipItem() {
        base.EquipItem();
        this.transform.GetComponentInChildren<Light>().enabled = true;
    }
}