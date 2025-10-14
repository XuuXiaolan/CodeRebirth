using System.Collections;
using UnityEngine;

namespace CodeRebirth.src.MiscScripts;

public class SpookyFlicker : MonoBehaviour
{
    [field: Header("Targets")]
    [field: Tooltip("Primary light to flicker. If omitted, searches on this GameObject.")]
    [field: SerializeField]
    public Light TargetLight { get; private set; }

    [field: Tooltip("Optional renderers whose emission should follow the flicker (e.g., bulbs, signs).")]
    [field: SerializeField]
    public Renderer[] EmissiveRenderers { get; private set; } = [];

    [field: ColorUsage(true, true)]
    [field: SerializeField]
    public Color EmissionColor { get; private set; } = Color.white;

    [field: Header("Intensity")]
    [field: Tooltip("Nominal steady intensity when not flickering.")]
    [field: SerializeField]
    public float BaseIntensity { get; private set; } = 2.0f;

    [field: Tooltip("Intensity range used for quick micro-flickers (as a fraction of base).")]
    [field: Range(0f, 1f)]
    [field: SerializeField]
    public float MinFrac = 0.4f;

    [field: Range(0f, 2f)]
    [field: SerializeField]
    public float MaxFrac = 1.2f;

    [field: Tooltip("How fast intensity can change (higher = snappier).")]
    [field: SerializeField]
    public float IntensityLerpSpeed = 10f;

    [field: Header("Flicker Timing")]
    [field: Tooltip("Minimum seconds between micro-flicker samples.")]
    [field: SerializeField]
    public Vector2 MicroIntervalRange = new(0.02f, 0.08f);

    [field: Tooltip("Random Perlin noise scroll speed for organic variation.")]
    [field: SerializeField]
    public float NoiseSpeed = 6f;

    [field: Tooltip("Chance per second to trigger a dramatic dim event.")]
    [field: Range(0f, 1f)]
    [field: SerializeField]
    public float DimEventRate = 0.15f;

    [field: Tooltip("How deep a dim event goes (multiplier of base intensity).")]
    [field: Range(0f, 1f)]
    [field: SerializeField]
    public float DimFloorFrac = 0.08f;

    [field: Tooltip("Duration range for a dim event.")]
    [field: SerializeField]
    public Vector2 DimDurationRange = new(0.08f, 0.35f);

    [field: Tooltip("Chance per second to trigger a short blackout.")]
    [field: Range(0f, 1f)]
    [field: SerializeField]
    public float BlackoutEventRate = 0.05f;

    [field: Tooltip("Duration range for a blackout event.")]
    [field: SerializeField]
    public Vector2 BlackoutDurationRange = new(0.15f, 0.7f);

    [field: Header("Sound")]
    [field: Tooltip("Looping 'buzz/hum' source.")]
    [field: SerializeField]
    public AudioSource? LoopSource = null;

    [field: Tooltip("One-shot source for ticks/pops etc.")]
    [field: SerializeField]
    public AudioSource? OneShotSource = null;

    [field: Tooltip("Played when a dim or blackout kicks in.")]
    [field: SerializeField]
    public AudioClip[] DimOrBlackoutClips = [];

    [field: Tooltip("Played when recovering back to normal after a dim/blackout.")]
    [field: SerializeField]
    public AudioClip[] RecoverClips = [];

    [field: Tooltip("Random pitch range applied to one-shots.")]
    [field: SerializeField]
    public Vector2 OneShotPitchRange = new(0.95f, 1.05f);


    private float _targetIntensity, _currentIntensity, _noiseT = 0f;
    private MaterialPropertyBlock _mpb;
    private Coroutine? _loop = null;
    private bool _inEvent = false;

    private void Awake()
    {
        _mpb = new();
        _currentIntensity = BaseIntensity;
        _targetIntensity = BaseIntensity;
        ApplyIntensityImmediate(_currentIntensity);
    }

    private void OnEnable()
    {
        StartFlicker();
    }

    private void OnDisable()
    {
        StopFlicker();
    }

    private void StartFlicker()
    {
        if (_loop != null)
        {
            return;
        }

        EnsureLoopPlaying();
        _loop = StartCoroutine(FlickerLoop());
    }

    public void StopFlicker()
    {
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }

        _inEvent = false;
        _targetIntensity = BaseIntensity;
        ApplyIntensityImmediate(BaseIntensity);
        if (LoopSource != null && LoopSource.isPlaying)
        {
            LoopSource.Stop();
        }
    }

    private IEnumerator FlickerLoop()
    {
        float nextSampleTime = 0f;
        float time = 0f;
        float perlinOffset = Random.value * 1000f;

        while (enabled)
        {
            time += Time.deltaTime;

            if (!_inEvent)
            {
                if (Random.value < BlackoutEventRate * Time.deltaTime)
                {
                    yield return BlackoutEvent();
                    continue;
                }
                if (Random.value < DimEventRate * Time.deltaTime)
                {
                    yield return DimEvent();
                    continue;
                }
            }

            if (time >= nextSampleTime)
            {
                float next = Random.Range(MicroIntervalRange.x, MicroIntervalRange.y);
                nextSampleTime = time + next;

                _noiseT += NoiseSpeed * next;
                float n = Mathf.PerlinNoise(perlinOffset + _noiseT, 0f);

                float frac = Mathf.Lerp(MinFrac, MaxFrac, n);
                _targetIntensity = BaseIntensity * frac;

                if (Random.value < 0.1f)
                {
                    _targetIntensity *= Random.Range(0.7f, 1.0f);
                }

                if (_targetIntensity <= BaseIntensity * (MinFrac + 0.05f) && RecoverClips != null && RecoverClips.Length > 0)
                {
                    PlayOneShot(RecoverClips);
                }
            }

            _currentIntensity = Mathf.Lerp(_currentIntensity, _targetIntensity, 1f - Mathf.Exp(-IntensityLerpSpeed * Time.deltaTime));
            ApplyIntensity(_currentIntensity);
            EnsureLoopPlaying();
            yield return null;
        }
    }

    private IEnumerator DimEvent()
    {
        _inEvent = true;
        float duration = Random.Range(DimDurationRange.x, DimDurationRange.y);

        PlayOneShot(DimOrBlackoutClips);
        yield return TweenTo(BaseIntensity * DimFloorFrac, 0.02f, 0.04f);

        float timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;

            float trem = 1f + (Mathf.PerlinNoise(_noiseT * 3f, 1f) - 0.5f) * 0.15f;
            ApplyIntensity(BaseIntensity * DimFloorFrac * trem);
            yield return null;
        }

        PlayOneShot(RecoverClips);
        yield return TweenTo(BaseIntensity * Random.Range(0.85f, 1.1f), 0.05f, 0.12f);
        _inEvent = false;
    }

    private IEnumerator BlackoutEvent()
    {
        _inEvent = true;
        float duration = Random.Range(BlackoutDurationRange.x, BlackoutDurationRange.y);

        PlayOneShot(DimOrBlackoutClips);
        yield return TweenTo(0f, 0.01f, 0.03f);

        float savedLoopVol = 0f;
        if (LoopSource != null)
        {
            savedLoopVol = LoopSource.volume;
            LoopSource.volume = savedLoopVol * 0.35f;
        }

        float timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        if (LoopSource != null)
        {
            LoopSource.volume = savedLoopVol;
        }

        PlayOneShot(RecoverClips);
        yield return TweenTo(BaseIntensity * Random.Range(0.9f, 1.15f), 0.06f, 0.15f);

        _inEvent = false;
    }

    private IEnumerator TweenTo(float target, float minTime, float maxTime)
    {
        float duration = Random.Range(minTime, maxTime);
        float timeElapsed = 0f;
        float start = _currentIntensity;
        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            float step = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(timeElapsed / duration));
            float newIntensity = Mathf.Lerp(start, target, step);
            _currentIntensity = newIntensity;
            ApplyIntensity(newIntensity);
            yield return null;
        }

        _targetIntensity = target;
        _currentIntensity = target;
        ApplyIntensityImmediate(target);
    }

    private void EnsureLoopPlaying()
    {
        if (LoopSource == null)
        {
            return;
        }

        if (!LoopSource.isPlaying)
        {
            LoopSource.Play();
        }
    }

    private void PlayOneShot(AudioClip[] bank)
    {
        if (OneShotSource == null || bank == null || bank.Length == 0)
        {
            return;
        }

        AudioClip clip = bank[Random.Range(0, bank.Length)];
        if (clip == null)
        {
            return;
        }
        OneShotSource.pitch = Random.Range(OneShotPitchRange.x, OneShotPitchRange.y);
        OneShotSource.PlayOneShot(clip);
    }

    private void ApplyIntensity(float newIntensity)
    {
        TargetLight.intensity = Mathf.Max(0f, newIntensity);

        if (EmissiveRenderers != null && EmissiveRenderers.Length > 0)
        {
            float emiss = Mathf.Max(0f, newIntensity / Mathf.Max(0.0001f, BaseIntensity));
            foreach (Renderer emissiveRenderer in EmissiveRenderers)
            {
                if (emissiveRenderer == null)
                    continue;

                emissiveRenderer.GetPropertyBlock(_mpb);
                _mpb.SetColor("_EmissionColor", EmissionColor * emiss);
                emissiveRenderer.SetPropertyBlock(_mpb);
            }
        }
    }

    private void ApplyIntensityImmediate(float val)
    {
        ApplyIntensity(val);
        foreach (Renderer emissiveRenderer in EmissiveRenderers)
        {
            if (emissiveRenderer == null)
                continue;

            foreach (Material sharedMaterial in emissiveRenderer.sharedMaterials)
            {
                if (sharedMaterial == null)
                    continue;

                if (!sharedMaterial.IsKeywordEnabled("_EMISSION"))
                {
                    sharedMaterial.EnableKeyword("_EMISSION");
                }
            }
        }
    }
}
