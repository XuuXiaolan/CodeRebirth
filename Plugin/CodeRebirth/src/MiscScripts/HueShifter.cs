using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class HueShifter : MonoBehaviour
{
    public float Speed = 1;
    private Renderer rend;

    public void Start()
    {
        rend = GetComponent<Renderer>();
        rend.GetMaterial().color = Color.HSVToRGB(.34f, .84f, .67f);
    }

    public void Update()
    {
        float h, s, v;
        Color.RGBToHSV(rend.GetMaterial().color, out h, out s, out v);
        rend.GetMaterial().color = Color.HSVToRGB(h + Time.deltaTime * .25f, s, v);
    }
}