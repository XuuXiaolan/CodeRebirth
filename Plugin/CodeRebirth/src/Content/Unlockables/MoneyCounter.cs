using System.Collections;
using System.Linq;
using Dawn;
using Dawn.Utils;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Events;

namespace CodeRebirth.src.Content.Unlockables;

public class MoneyCounter : NetworkSingleton<MoneyCounter>, IHittable
{
    [SerializeField]
    private NetworkAnimator _networkAnimator;

    [SerializeField]
    private Animator _animator;

    [SerializeField]
    private AudioSource _audioSource;

    [SerializeField]
    private AudioClip _onSpawnSound;

    [SerializeField]
    private AudioClip _onHitSound;

    [SerializeField]
    private AudioClip _onCoinGetSound;

    [SerializeField]
    private AudioClip _onOneWheelTurn;

    [SerializeField]
    private AudioClip _onTenWheelTurn;

    [SerializeField]
    private AudioClip _onHundredWheelTurn;

    [SerializeField]
    private Transform HundredWheel;
    [SerializeField]
    private Transform TenWheel;
    [SerializeField]
    private Transform OneWheel;

    [SerializeField]
    private Renderer _renderer;

    [SerializeField]
    private UnityEvent OnGoingDebt = new();

    [SerializeField]
    private UnityEvent OnLeavingDebt = new();

    private NetworkVariable<int> _totalMoneyStored = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private static NamespacedKey _moneyKey = NamespacedKey.From("code_rebirth", "money_stored");
    private static readonly int OnHitHash = Animator.StringToHash("hit"); // Trigger
    private static readonly int CoinGetHash = Animator.StringToHash("coinGet"); // Trigger

    public override void OnNetworkPostSpawn()
    {
        base.OnNetworkPostSpawn();
        _audioSource.PlayOneShot(_onSpawnSound);

        if (!IsServer)
        {
            return;
        }

        PersistentDataContainer? contract = DawnLib.GetCurrentContract();
        if (contract != null && contract.TryGet(_moneyKey, out int value))
        {
            Plugin.ExtendedLogging($"Loading money from contract: {value}");
            _totalMoneyStored.Value = value;
        }
        UpdateVisuals(0, _totalMoneyStored.Value);
    }

    private Quaternion _hundredBase;
    private Quaternion _tenBase;
    private Quaternion _oneBase;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        InitPatches();
        _hundredBase = HundredWheel.localRotation;
        _tenBase = TenWheel.localRotation;
        _oneBase = OneWheel.localRotation;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        RemovePatches();
    }

    private void InitPatches()
    {
        On.StartOfRound.AutoSaveShipData += SaveMoneyToContract;
        On.HUDManager.ApplyPenalty += CalculateCoinPenalty;
    }

    private void RemovePatches()
    {
        On.StartOfRound.AutoSaveShipData -= SaveMoneyToContract;
        On.HUDManager.ApplyPenalty -= CalculateCoinPenalty;
    }

    private void CalculateCoinPenalty(On.HUDManager.orig_ApplyPenalty orig, HUDManager self, int playersDead, int bodiesInsured)
    {
        orig(self, playersDead, bodiesInsured);
        if (!NetworkManager.Singleton.IsServer || playersDead < StartOfRound.Instance.allPlayerScripts.Count(x => x.isPlayerControlled))
        {
            return;
        }

        Plugin.ExtendedLogging($"Applying 40 coin max penalty on all team wipe");
        RemoveMoney(40);
    }

    private void SaveMoneyToContract(On.StartOfRound.orig_AutoSaveShipData orig, StartOfRound self)
    {
        orig(self);
        if (!NetworkManager.Singleton.IsServer)
        {
            return;
        }

        Plugin.ExtendedLogging($"Saving money to contract: {_totalMoneyStored.Value}");
        DawnLib.GetCurrentContract()?.Set(_moneyKey, _totalMoneyStored.Value);
    }

    public void AddMoney(int amount)
    {
        if (amount == 0)
            return;

        if (amount < 0)
        {
            RemoveMoney(-amount);
            return;
        }

        int oldValue = _totalMoneyStored.Value;
        _totalMoneyStored.Value += amount;
        _networkAnimator.SetTrigger(CoinGetHash);
        PlaySoundServerRpc(1);
        UpdateVisuals(oldValue, _totalMoneyStored.Value);
    }

    public int MoneyStored()
    {
        return _totalMoneyStored.Value;
    }

    public void RemoveMoney(int amount)
    {
        if (amount == 0)
            return;

        if (amount < 0)
        {
            AddMoney(-amount);
            return;
        }

        int oldValue = _totalMoneyStored.Value;
        _totalMoneyStored.Value -= amount;
        if (oldValue >= 0 && _totalMoneyStored.Value < 0)
        {
            _totalMoneyStored.Value -= 150;
        }
        UpdateVisuals(oldValue, _totalMoneyStored.Value);
    }

    private void UpdateVisuals(int oldValue, int newValue)
    {
        if (StartOfRound.Instance.inShipPhase)
        {
            Plugin.ExtendedLogging($"Saving money to contract: {_totalMoneyStored.Value}");
            DawnLib.GetCurrentContract()?.Set(_moneyKey, _totalMoneyStored.Value);
        }

        UpdateVisualsClientRpc(oldValue, newValue);
    }

    private Coroutine? _spinRoutine;

    [ClientRpc]
    private void UpdateVisualsClientRpc(int oldValue, int newValue)
    {
        if (newValue < 0 && oldValue >= 0)
        {
            newValue = 0;
            GoToDebtMode();
        }
        else if (oldValue < 0 && newValue >= 0)
        {
            LeaveDebt();
        }

        int hundreds = newValue / 100;
        int tens = (newValue % 100) / 10;
        int ones = newValue % 10;

        if (_spinRoutine != null)
        {
            StopCoroutine(_spinRoutine);
        }

        float duration = 10f;
        if (Mathf.Abs(newValue - oldValue) <= 10)
        {
            duration = 2.5f;
        }
        else if (Mathf.Abs(newValue - oldValue) <= 50f)
        {
            duration = 5f;
        }
        _spinRoutine = StartCoroutine(SpinWheels(hundreds, tens, ones, duration));
    }

    private void LeaveDebt()
    {
        OnLeavingDebt.Invoke();
        Material material = _renderer.GetSharedMaterial();
        material.SetColor(EmissiveColor, Color.black);
    }

    private static readonly int EmissiveColor = Shader.PropertyToID("_EmissiveColor");
    private void GoToDebtMode()
    {
        OnGoingDebt.Invoke();
        Material material = _renderer.GetSharedMaterial();
        material.SetColor(EmissiveColor, new Color(50f, 0f, 0f, 1f));
    }

    private IEnumerator SpinWheels(int targetHundred, int targetTen, int targetOne, float duration)
    {
        int currentHundred = GetDigitFromWheel(HundredWheel, _hundredBase);
        int currentTen = GetDigitFromWheel(TenWheel, _tenBase);
        int currentOne = GetDigitFromWheel(OneWheel, _oneBase);

        int currentValue = currentHundred * 100 + currentTen * 10 + currentOne;
        int destinationValue = targetHundred * 100 + targetTen * 10 + targetOne;

        if (currentValue == destinationValue)
        {
            _spinRoutine = null;
            yield break;
        }

        int direction = (destinationValue > currentValue) ? 1 : -1;
        int steps = Mathf.Abs(destinationValue - currentValue);

        float stepDuration = Mathf.Max(0.1f, duration / steps);

        float angleHundred = AngleForDigitRaw(currentHundred);
        float angleTen = AngleForDigitRaw(currentTen);
        float angleOne = AngleForDigitRaw(currentOne);

        HundredWheel.localRotation = _hundredBase * Quaternion.AngleAxis(angleHundred, Vector3.right);
        TenWheel.localRotation = _tenBase * Quaternion.AngleAxis(angleTen, Vector3.right);
        OneWheel.localRotation = _oneBase * Quaternion.AngleAxis(angleOne, Vector3.right);

        for (int i = 0; i < steps; i++)
        {
            bool moveTen = false;
            bool moveHundred = false;

            if (direction > 0)
            {
                if (currentOne == 9)
                {
                    currentOne = 0;
                    moveTen = true;
                }
                else
                {
                    _audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                    _audioSource.PlayOneShot(_onOneWheelTurn);
                    currentOne++;
                }

                if (moveTen)
                {
                    if (currentTen == 9)
                    {
                        currentTen = 0;
                        moveHundred = true;
                    }
                    else
                    {
                        _audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                        _audioSource.PlayOneShot(_onTenWheelTurn);
                        currentTen++;
                    }

                    if (moveHundred)
                    {
                        _audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
                        _audioSource.PlayOneShot(_onHundredWheelTurn);
                        currentHundred++;
                    }
                }
            }
            else
            {
                if (currentOne == 0)
                {
                    currentOne = 9;
                    moveTen = true;
                }
                else
                {
                    currentOne--;
                }

                if (moveTen)
                {
                    if (currentTen == 0)
                    {
                        currentTen = 9;
                        moveHundred = true;
                    }
                    else
                    {
                        currentTen--;
                    }

                    if (moveHundred)
                    {
                        currentHundred--;
                    }
                }
            }

            float startHundred = angleHundred;
            float startTen = angleTen;
            float startOne = angleOne;

            angleOne += direction * 36f;
            if (moveTen)
            {
                angleTen += direction * 36f;
            }
            if (moveHundred)
            {
                angleHundred += direction * 36f;
            }

            float endHundred = angleHundred;
            float endTen = angleTen;
            float endOne = angleOne;

            yield return AnimateTick(startHundred, endHundred, startTen, endTen, startOne, endOne, stepDuration);
        }

        SetWheelDigit(HundredWheel, _hundredBase, targetHundred);
        SetWheelDigit(TenWheel, _tenBase, targetTen);
        SetWheelDigit(OneWheel, _oneBase, targetOne);

        _spinRoutine = null;
    }

    private IEnumerator AnimateTick(float startHundred, float endHundred, float startTen, float endTen, float startOne, float endOne, float tickDuration)
    {
        float elapsed = 0f;
        while (elapsed < tickDuration)
        {
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / tickDuration);
            float e = u * u * (3f - 2f * u);

            HundredWheel.localRotation = _hundredBase * Quaternion.AngleAxis(Mathf.Lerp(startHundred, endHundred, e), Vector3.right);
            TenWheel.localRotation = _tenBase * Quaternion.AngleAxis(Mathf.Lerp(startTen, endTen, e), Vector3.right);
            OneWheel.localRotation = _oneBase * Quaternion.AngleAxis(Mathf.Lerp(startOne, endOne, e), Vector3.right);

            yield return null;
        }

        HundredWheel.localRotation = _hundredBase * Quaternion.AngleAxis(endHundred, Vector3.right);
        TenWheel.localRotation = _tenBase * Quaternion.AngleAxis(endTen, Vector3.right);
        OneWheel.localRotation  = _oneBase * Quaternion.AngleAxis(endOne, Vector3.right);
    }

    private int GetDigitFromWheel(Transform wheel, Quaternion baseRot)
    {
        Quaternion rel = Quaternion.Inverse(baseRot) * wheel.localRotation;

        Vector3 f = rel * Vector3.forward;

        float angle = Mathf.Atan2(-f.y, f.z) * Mathf.Rad2Deg;
        if (angle < 0f)
        {
            angle += 360f;
        }

        int digit = Mathf.RoundToInt(angle / 36f) % 10;
        if (digit < 0)
        {
            digit += 10;
        }
        return digit;
    }

    private void SetWheelDigit(Transform wheel, Quaternion baseRot, int digit)
    {
        float angle = 36f * digit;
        wheel.localRotation = baseRot * Quaternion.AngleAxis(angle, Vector3.right);
    }

    private float AngleForDigitRaw(int digit)
    {
        return 36f * digit;
    }

    public bool Hit(int force, Vector3 hitDirection, PlayerControllerB? playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
    {
        if (playerWhoHit == null)
        {
            return false;
        }

        HitServerRpc(force);
        PlaySoundServerRpc(0);
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void HitServerRpc(int force)
    {
        _networkAnimator.SetTrigger(OnHitHash);
        RemoveMoney(force);
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlaySoundServerRpc(int index)
    {
        PlaySoundClientRpc(index);
    }

    [ClientRpc]
    private void PlaySoundClientRpc(int index)
    {
        switch (index)
        {
            case 0:
                _audioSource.PlayOneShot(_onHitSound);
                break;
            case 1:
                _audioSource.PlayOneShot(_onCoinGetSound);
                break;
        }
    }
}