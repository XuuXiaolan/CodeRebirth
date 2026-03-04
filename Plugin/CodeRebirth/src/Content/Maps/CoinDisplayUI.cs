using System.Collections;
using System.Collections.Generic;
using Dawn.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CodeRebirth.src.Content.Maps;
public class CoinDisplayUI : Singleton<CoinDisplayUI>
{
    [field: SerializeField]
    public Image Image { get; private set; }

    [field: SerializeField]
    public TextMeshProUGUI CoinTMP { get; private set; }

    [field: SerializeField]
    public TextMeshProUGUI CoinChangeTMP { get; private set; }

    [field: SerializeField]
    public float MinDistanceRequired { get; private set; } = 10f;
    internal static List<Transform> PointsOfInterest = new();

    private Coroutine? fadingRoutine = null;
    internal Coroutine? editCoinRoutine = null;
    private Vector3 TMPChangeOriginalPosition = Vector3.zero;

    private void Start()
    {
        TMPChangeOriginalPosition = ((RectTransform)CoinChangeTMP.transform).anchoredPosition3D;
    }

    private void Update()
    {
        if (fadingRoutine != null || editCoinRoutine != null)
        {
            return;
        }

        foreach (Transform point in PointsOfInterest)
        {
            if (Vector3.Distance(point.position, GameNetworkManager.Instance.localPlayerController.transform.position) <= MinDistanceRequired)
            {
                if (Image.color.a == 1)
                {
                    return;
                }
                fadingRoutine = StartCoroutine(FadeIn(false));
                point.gameObject.SetActive(true);
                return;
            }
        }

        if (Image.color.a == 1)
        {
            fadingRoutine = StartCoroutine(FadeOut());
        }
    }

    private IEnumerator FadeOut()
    {
        float alpha = 1f;
        Image.color = new Color(Image.color.r, Image.color.g, Image.color.b, alpha);
        CoinTMP.color = new Color(CoinTMP.color.r, CoinTMP.color.g, CoinTMP.color.b, alpha);
        float duration = 0.25f;
        while (duration > 0)
        {
            alpha -= Time.deltaTime * 4f;
            Image.color = new Color(Image.color.r, Image.color.g, Image.color.b, alpha);
            CoinTMP.color = new Color(CoinTMP.color.r, CoinTMP.color.g, CoinTMP.color.b, alpha);
            duration -= Time.deltaTime;
            yield return null;
        }
        Image.color = new Color(Image.color.r, Image.color.g, Image.color.b, 0);
        CoinTMP.color = new Color(CoinTMP.color.r, CoinTMP.color.g, CoinTMP.color.b, 0);
        fadingRoutine = null;
    }

    internal IEnumerator EditCoinAmount(int newValue, int oldValue)
    {
        if (Image.color.a == 0)
        {
            yield return StartCoroutine(FadeIn(true));
        }

        int changedAmount = newValue - oldValue;
        if (changedAmount > 0)
        {
            CoinChangeTMP.color = Color.green;
        }
        else
        {
            CoinChangeTMP.color = Color.red;
        }

        int extraDigits = CoinTMP.text.Length - 1;
        ((RectTransform)CoinChangeTMP.transform).anchoredPosition3D = TMPChangeOriginalPosition + new Vector3(25f * extraDigits, 0f, 0f);
        CoinChangeTMP.text = changedAmount.ToString();
        bool goSlowly = changedAmount <= 10;
        while (changedAmount != 0)
        {
            if (changedAmount > 0)
            {
                CoinTMP.text = (int.Parse(CoinTMP.text) + 1).ToString();
                CoinChangeTMP.text = "+" + (int.Parse(CoinChangeTMP.text) - 1).ToString();
                changedAmount--;
            }
            else if (changedAmount < 0)
            {
                CoinTMP.text = (int.Parse(CoinTMP.text) - 1).ToString();
                CoinChangeTMP.text = (int.Parse(CoinChangeTMP.text) + 1).ToString();
                changedAmount++;
            }
            extraDigits = CoinTMP.text.Length - 1;
            ((RectTransform)CoinChangeTMP.transform).anchoredPosition3D = TMPChangeOriginalPosition + new Vector3(25f * extraDigits, 0f, 0f);
            yield return new WaitForSeconds(0.05f * (goSlowly ? 5 : 1));
        }

        yield return new WaitForSeconds(0.25f);

        StartCoroutine(FadeOut());
        float duration = 0.25f;
        float alpha = 1f;
        float decreasingHeight = 0f;
        while (duration > 0)
        {
            decreasingHeight += Time.deltaTime * 60f * 4f;
            alpha -= Time.deltaTime * 4f;
            CoinChangeTMP.color = new Color(CoinChangeTMP.color.r, CoinChangeTMP.color.g, CoinChangeTMP.color.b, alpha);
            ((RectTransform)CoinChangeTMP.transform).anchoredPosition3D = TMPChangeOriginalPosition + new Vector3(25f * extraDigits, -decreasingHeight, 0f);
            duration -= Time.deltaTime;
            yield return null;
        }
        CoinChangeTMP.color = new Color(CoinChangeTMP.color.r, CoinChangeTMP.color.g, CoinChangeTMP.color.b, 0);
        editCoinRoutine = null;
    }

    private IEnumerator FadeIn(bool includeCoinChangeTMP)
    {
        float alpha = 0f;
        Image.color = new Color(Image.color.r, Image.color.g, Image.color.b, alpha);
        CoinTMP.color = new Color(CoinTMP.color.r, CoinTMP.color.g, CoinTMP.color.b, alpha);
        if (includeCoinChangeTMP)
        {
            CoinChangeTMP.color = new Color(CoinChangeTMP.color.r, CoinChangeTMP.color.g, CoinChangeTMP.color.b, alpha);
        }
        float duration = 0.25f;
        while (duration > 0)
        {
            alpha += Time.deltaTime * 4f;
            Image.color = new Color(Image.color.r, Image.color.g, Image.color.b, alpha);
            CoinTMP.color = new Color(CoinTMP.color.r, CoinTMP.color.g, CoinTMP.color.b, alpha);
            if (includeCoinChangeTMP)
            {
                CoinChangeTMP.color = new Color(CoinChangeTMP.color.r, CoinChangeTMP.color.g, CoinChangeTMP.color.b, alpha);
            }
            duration -= Time.deltaTime;
            yield return null;
        }
        Image.color = new Color(Image.color.r, Image.color.g, Image.color.b, 1);
        CoinTMP.color = new Color(CoinTMP.color.r, CoinTMP.color.g, CoinTMP.color.b, 1);
        if (includeCoinChangeTMP)
        {
            CoinChangeTMP.color = new Color(CoinChangeTMP.color.r, CoinChangeTMP.color.g, CoinChangeTMP.color.b, 1);
        }
        fadingRoutine = null;
    }
}