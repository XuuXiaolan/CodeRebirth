using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace CodeRebirth.src.MiscScripts;
public class WaitForTimeBeforeEvent : MonoBehaviour
{
    [SerializeField]
    private UnityEvent _afterWaitingEvent = new();

    public void WaitAndDoEvent(float time)
    {
        StartCoroutine(WaitAndDo(time));
    }

    private IEnumerator WaitAndDo(float time)
    {
        yield return new WaitForSeconds(time);
        _afterWaitingEvent.Invoke();
    }
}