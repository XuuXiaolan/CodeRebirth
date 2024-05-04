using UnityEngine;

namespace CodeRebirth.ScrapStuff;
public class MeteoriteShard : GrabbableObject {
    private ParticleSystem[] particles; 
    private int count;
    public override void Start() {
        base.Start();
        particles = GetComponentsInChildren<ParticleSystem>();
    }
    public override void ItemActivate(bool used, bool buttonDown = true) {
        base.ItemActivate(used, buttonDown);
        if (count%2 == 0) {
            foreach (ParticleSystem particle in particles) {
                particle.Stop();
                particle.Clear();
            }
        } else {
            foreach (ParticleSystem particle in particles) {
                particle.Play();
            }
        }
        count++;
    }
}