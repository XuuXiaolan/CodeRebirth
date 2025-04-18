using System.Linq;
using UnityEngine;

namespace CodeRebirth.src.Content.Items;
public class WrittenDocument : GrabbableObject
{
    [Tooltip("In ascending order from first, to second, etc")]
    [SerializeField]
    private GameObject[] _orderedPages = [];

    [SerializeField]
    private AudioSource _pageSource = null!;

    [SerializeField]
    private AudioClip[] _pageFlipSounds = [];

    private int activePage = 0;
    public override void ItemActivate(bool used, bool buttonDown = true)
    {
        base.ItemActivate(used, buttonDown);
        if (_orderedPages.Length <= 1)
            return;

        _pageSource.PlayOneShot(_pageFlipSounds[UnityEngine.Random.Range(0, _pageFlipSounds.Length)]);
        if (_orderedPages.Where(x => x.activeSelf).Count() == 1)
        {
            foreach (var page in _orderedPages)
            {
                page.SetActive(true);
            }
            return;
        }

        _orderedPages[activePage].SetActive(false);
        activePage++;
    }
}