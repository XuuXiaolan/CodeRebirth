using UnityEngine;

namespace CodeRebirth.src.MiscScripts;
public class RainbowMaterialShift : MonoBehaviour
{
    [SerializeField]
    private Renderer _renderer = null!;
    [SerializeField]
    private float _speed = 1f;

    [Range(0f, 1f)]
    [SerializeField]
    private float _alpha = 1f;

    private Material _material = null!;
    private float _currentColorHSV = 0f;
    private System.Random _colourRandom = new();
    private static int counter = 0;
    private static readonly int ColorHash = Shader.PropertyToID("_Color");

    private void Awake()
    {
        counter++;
        _material = _renderer.material;
        _colourRandom = new System.Random(StartOfRound.Instance.randomMapSeed + StartOfRound.Instance.allPlayerScripts.Length + counter);
        _currentColorHSV = (float)_colourRandom.NextDouble();
    }

    private void Update()
    {
        _currentColorHSV += Time.deltaTime * _speed;
        if (_currentColorHSV > 1f)
        {
            _currentColorHSV -= 1f;
        }
        Color color = Color.HSVToRGB(_currentColorHSV, 1f, 1f);
        var newColor = new Color(color.r, color.g, color.b, _alpha);

        _material.SetColor(ColorHash, newColor); // alternatively .color might also just work
    }
}