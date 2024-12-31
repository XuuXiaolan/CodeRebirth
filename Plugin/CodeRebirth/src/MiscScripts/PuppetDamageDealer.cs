using System.Collections;
using System.Collections.Generic;
using CodeRebirth.src.Content.Enemies;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class PuppetDamageDealer : MonoBehaviour
{
    public bool isEnabled = false;
    public Puppeteer puppeteer = null!;
    private List<GameObject> puppeteers = new();
    public void OnTriggerStay(Collider other)
    {
        if (!isEnabled) return;
        if (other.gameObject.layer == 19 && other.gameObject.name.Contains("PuppeteerPuppet") && !puppeteers.Contains(other.gameObject))
        {
            puppeteers.Add(other.gameObject);
            StartCoroutine(ClearPuppet(other.gameObject));
        }
    }

    private IEnumerator ClearPuppet(GameObject gameObject)
    {
        gameObject.GetComponent<PuppeteersVoodoo>().Hit(1, puppeteer.transform.forward, null, false, -1);
        yield return new WaitForSeconds(1f);
        if (puppeteers.Contains(gameObject)) puppeteers.Remove(gameObject);
    }
}