using CodeRebirth.src.Util.Extensions;
using GameNetcodeStuff;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public delegate void OnFinishTalking();
public class DuckUI : NetworkBehaviour
{
    public ItemUI itemUI = null!;
    [SerializeField] private TextMeshProUGUI uiText = null!;
    [SerializeField] private Transform duckTransform = null!;
    public float bobDuration = 1f;   // Duration of one bob cycle
    public float talkSpeed = 0.1f;   // Speed of text typing
    public CanvasGroup canvasGroup = null!;

    private string fullText = string.Empty;
    private Vector3 originalDuckPos;
    private Vector3 originalLocalDuckPos;
    [HideInInspector] public bool isTalking = false;
    private float bobTimer = 0f;
    private AnimationCurve bobCurve = null!;
    private Coroutine? duckCoroutine = null;
    public static DuckUI Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        this.gameObject.transform.localPosition = Vector3.zero;
        this.SetTextManually("");
        this.SetUIVisible(false, true);
        originalDuckPos = duckTransform.position;
        originalLocalDuckPos = duckTransform.localPosition;

        //create da bob curve
        bobCurve = new AnimationCurve();
        bobCurve.AddKey(new Keyframe(0f, 0f, 0f, 10f));
        bobCurve.AddKey(new Keyframe(0.25f, 1f, 0f, 0f));
        bobCurve.AddKey(new Keyframe(0.5f, 0f, 10f, 0f));
        bobCurve.AddKey(new Keyframe(0.75f, -1f, 0f, 0f));
        bobCurve.AddKey(new Keyframe(1f, 0f, -10f, 0f));


        //smooth da bob curve
        for (int i = 0; i < bobCurve.keys.Length; i++)
        {
            AnimationCurveUtil.SetKeyTangentMode(bobCurve, i, AnimationCurveUtil.TangentMode.Smooth);
        }
    }

    public void Update()
    {
        if (isTalking)
        {
            duckTransform.position = originalDuckPos; // what the fuck
            bobTimer += Time.deltaTime * 2.2f;
            if (bobTimer > bobDuration)
            {
                bobTimer = 0f;
            }

            float curveValue = bobCurve.Evaluate(bobTimer / bobDuration);
            float newY = Mathf.Lerp(140f, 150f, (curveValue + 1) / 2);
            float adjustedY = originalLocalDuckPos.y + (newY - 145f);
            duckTransform.localPosition = new Vector3(originalLocalDuckPos.x, adjustedY, originalLocalDuckPos.z);
        }
    }

    public void SetTextManually(string text)
    {
        uiText.text = text;
    }

    public void SetUIVisible(bool visible, bool instant = false)
    {
        if (instant)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
        }
        else
        {
            StartCoroutine(FadeUI(visible));
        }
    }

    private IEnumerator FadeUI(bool visible)
    {
        float startAlpha = canvasGroup.alpha;
        float endAlpha = visible ? 1 : 0;
        float elapsedTime = 0f;

        while (elapsedTime < 0.3f)
        {
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, elapsedTime / 0.3f);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
    }

    public void StartTalking(string text, float talkspeed, PlayerControllerB targetPlayer, bool isGlobal = false, OnFinishTalking? onFinishTalking = null)
    {
        if (targetPlayer == null) return;

        if (!targetPlayer.IsLocalPlayer() && !isGlobal) return;

        if (duckCoroutine != null)
        {
            StopCoroutine(duckCoroutine);
            SetTextManually("");
        }

        talkSpeed = talkspeed;
        fullText = text;
        isTalking = true;
        bobTimer = 0f;

        duckCoroutine = StartCoroutine(DuckTalkCoroutine(onFinishTalking));
    }

    private IEnumerator DuckTalkCoroutine(OnFinishTalking? onFinishTalking)
    {
        uiText.text = "";
        foreach (char c in fullText)
        {
            uiText.text += c;
            yield return new WaitForSeconds(talkSpeed);
        }

        // Reset duck position and stop bobbing after talking
        duckTransform.localPosition = originalLocalDuckPos;
        isTalking = false;
        duckCoroutine = null;
        yield return new WaitForSeconds(0.8f);
        onFinishTalking?.Invoke();
    }
}