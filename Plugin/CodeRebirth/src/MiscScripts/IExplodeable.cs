using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public interface IExplodeable // Always have this in a component on the enemy/maphazards layer
{
    public abstract void OnExplosion(int force, Vector3 explosionPosition, float distanceToExplosion);
}