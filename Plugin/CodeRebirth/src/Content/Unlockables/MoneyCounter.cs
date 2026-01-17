using System.Collections;
using Dawn.Utils;
using GameNetcodeStuff;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace CodeRebirth.src.Content.Unlockables;

public class MoneyCounter : NetworkSingleton<MoneyCounter>, IHittable
{
    [SerializeField]
    private NetworkAnimator _networkAnimator;

    [SerializeField]
    private Animator _animator;

    [SerializeField]
    private Transform HundredWheel;
    [SerializeField]
    private Transform TenWheel;
    [SerializeField]
    private Transform OneWheel;

    private NetworkVariable<int> _totalMoneyStored = new(379, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public override void OnNetworkPostSpawn()
    {
        base.OnNetworkPostSpawn();

        UpdateVisuals(0, _totalMoneyStored.Value);
    }

    private Quaternion _hundredBase;
    private Quaternion _tenBase;
    private Quaternion _oneBase;

    private void Awake()
    {
        _hundredBase = HundredWheel.localRotation;
        _tenBase = TenWheel.localRotation;
        _oneBase = OneWheel.localRotation;
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
        _networkAnimator.SetTrigger("coinGet");
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
        _totalMoneyStored.Value = Mathf.Max(0, _totalMoneyStored.Value - amount);
        UpdateVisuals(oldValue, _totalMoneyStored.Value);
    }

    private void UpdateVisuals(int oldValue, int newValue)
    {
        UpdateVisualsClientRpc(oldValue, newValue);
    }

    private Coroutine? _spinRoutine;

    [ClientRpc]
    private void UpdateVisualsClientRpc(int oldValue, int newValue)
    {
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
                        currentTen++;
                    }

                    if (moveHundred)
                    {
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
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void HitServerRpc(int force)
    {
        _networkAnimator.SetTrigger("hit");
        RemoveMoney(force);
    }
}