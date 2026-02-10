using System.Collections;
using System.Collections.Generic;
using Mechanics;
using Microlight.MicroBar;
using UnityEngine;
using UnityEngine.UI;

public class DopamineManager : MonoBehaviour
{
    public static DopamineManager Instance;

    [SerializeField] MicroBar dopBar;

    [Header("Dependencies")]
    [SerializeField] NewScrollMechanic scrollMechanic;
    
    [SerializeField] bool constantDecOn = false;
    [SerializeField] float amtToConstDec = 1f;

    [Header("Dopamine Bar")]
    [SerializeField] float maxDopValue = 100f;
    
    [Header("Base Dopamine Values")]
    [SerializeField] float goldAmt = 50f;
    [SerializeField] float posAmt = 10f;
    [SerializeField] float negAmt = -10f;
    [SerializeField] float neutralAmt = 10f;

    [Header("Special Effect Durations")]
    [SerializeField] float fireEffectDuration = 5f;
    [SerializeField] float iceEffectDuration = 5f;
    [SerializeField] float snoopEffectDuration = 5f;
    [SerializeField] float catEffectDuration = 5f;

    [Header("Special Effect Multipliers")]
    [SerializeField] float fireDrainMultiplier = 1.5f;
    [SerializeField] float iceDrainMultiplier = 0.5f;
    [SerializeField] float snoopDrainMultiplier = 0.75f;
    [SerializeField] float iceValueMultiplier = 0.7f;

    [Header("Hold/Fast Scroll Deductions")]
    [SerializeField] float holdChargeDeductionMultiplier = 0.5f;
    [SerializeField] float fastScrollDeductionMultiplier = 0.5f;
    [SerializeField] float fastScrollThreshold = 30f;

    [Header("Nyan Cat Rainbow")]
    [SerializeField] bool enableRainbow = true;
    [SerializeField] Graphic[] rainbowTargets;
    [SerializeField] float rainbowCycleSpeed = 1f;
    [SerializeField] float rainbowSaturation = 1f;
    [SerializeField] float rainbowValue = 1f;

    [Header("Nyan Cat Heal")]
    [SerializeField] float catHealPercent = 0.15f;
    [SerializeField] bool allowCatHealStacking = true;
    [SerializeField] bool skipCatHealAnimation = true;

    [Header("Snoop Tint")]
    [SerializeField] bool enableSnoopTint = true;
    [SerializeField] Graphic[] snoopTintTargets;
    [SerializeField] Color snoopTintColor = new Color(0f, 0.35f, 0.1f, 1f);

    [Header("Fire Tint")]
    [SerializeField] bool enableFireTint = true;
    [SerializeField] Graphic[] fireTintTargets;
    [SerializeField] Color fireTintColor = new Color(1f, 0.5f, 0f, 1f);

    [Header("Ice Tint")]
    [SerializeField] bool enableIceTint = true;
    [SerializeField] Graphic[] iceTintTargets;
    [SerializeField] Color iceTintColor = new Color(0.2f, 0.6f, 1f, 1f);

    [Header("Space Invader Tint")]
    [SerializeField] bool enableSpaceInvaderTint = true;
    [SerializeField] Graphic[] spaceInvaderTintTargets;
    [SerializeField] Color spaceInvaderTintColor = new Color(0.6f, 0.25f, 0.8f, 1f);

    private float baseGoldAmt;
    private float basePosAmt;
    private float baseNegAmt;
    private float baseNeutralAmt;

    private bool holdChargeActive;
    private float pendingSpaceInvaderNegative;

    private struct ActiveEffect
    {
        public PostInfo.PostSpecial special;
        public float endTime;
    }

    private readonly List<ActiveEffect> activeEffects = new List<ActiveEffect>();
    private bool rainbowActive;
    private Color[] rainbowOriginalColors;
    private bool tintCached;
    private Color[] tintOriginalColors;
    private Coroutine catHealRoutine;
    //private float crRunning = false; 

    public void Awake()
    {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            baseGoldAmt = goldAmt;
            basePosAmt = posAmt;
            baseNegAmt = negAmt;
            baseNeutralAmt = neutralAmt;

            if (scrollMechanic == null)
            {
                scrollMechanic = FindFirstObjectByType<NewScrollMechanic>();
            }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void Start()
    {
        //instantiate microbar with max health 
        dopBar.Initialize(maxDopValue);
        StartCoroutine(ConstantlyDecrease()); 
    }

    //coroutine to constantly decrease dopamine by a small amount 
    IEnumerator ConstantlyDecrease()
    {
        //crRunning = true; 
        while(true)
        {
            yield return new WaitForSeconds(1f);
            if (constantDecOn)
            {
                DecreaseDopSlightly(amtToConstDec); 
            }
        }

    }

    public void DecreaseDopSlightly(float amount)
    {
        float drainMultiplier = GetDrainMultiplier();
        float deductionMultiplier = GetDeductionMultiplier();
        float finalAmount = amount * drainMultiplier * deductionMultiplier;
        Debug.Log($"dope change by {finalAmount} from {dopBar.CurrentValue}");
        dopBar.UpdateBar(dopBar.CurrentValue - finalAmount); 
        
    }

    public void TrackDopEffects(PostInfo.PostType type)
    {
        TrackDopEffects(type, PostInfo.PostSpecial.None);
    }

    public void TrackDopEffects(PostInfo.PostType type, PostInfo.PostSpecial special)
    {
        constantDecOn = false; 

        if (special != PostInfo.PostSpecial.None)
        {
            ApplySpecialEffect(special);
        }

        float baseAmount = GetBaseAmount(type);
        float amount = baseAmount * GetValueMultiplier();

        if (amount < 0f)
        {
            amount *= GetDeductionMultiplier();

            if (special == PostInfo.PostSpecial.SpaceInvader)
            {
                pendingSpaceInvaderNegative += amount;
                constantDecOn = true;
                return;
            }
        }

        changeDop(amount);
        constantDecOn = !HasActiveEffect(PostInfo.PostSpecial.Cat); 

        //OnStatsChanged?.Invoke(); // Notify UI
                                      
        
    }

    public void changeDop(float amount)
    {
        changeDop(amount, false);
    }

    public void changeDop(float amount, bool skipAnimation)
    {
        Debug.Log($"dope change by {amount} from {dopBar.CurrentValue}");
        dopBar.UpdateBar(dopBar.CurrentValue + amount, skipAnimation);
    }

    public void ApplyPendingSpaceInvaderNegative()
    {
        if (pendingSpaceInvaderNegative == 0f) return;
        changeDop(pendingSpaceInvaderNegative);
        pendingSpaceInvaderNegative = 0f;
    }

    public void SetHoldChargeActive(bool active)
    {
        holdChargeActive = active;
    }

    //public method to access the current value of the dopamine bar at any time 
    public float getCurrDop()
    {
        return dopBar.CurrentValue; 
    }

    // Update is called once per frame
    void Update()
    {
        CleanupExpiredEffects();
        UpdateVisualStates();
    }

    private float GetBaseAmount(PostInfo.PostType type)
    {
        switch (type)
        {
            case PostInfo.PostType.Gold:
                return baseGoldAmt;
            case PostInfo.PostType.Positive:
                return basePosAmt;
            case PostInfo.PostType.Negative:
                return baseNegAmt;
            case PostInfo.PostType.Neutral:
                return baseNeutralAmt;
            default:
                return 0f;
        }
    }

    private float GetDrainMultiplier()
    {
        float multiplier = 1f;
        foreach (var effect in activeEffects)
        {
            switch (effect.special)
            {
                case PostInfo.PostSpecial.Fire:
                    multiplier *= fireDrainMultiplier;
                    break;
                case PostInfo.PostSpecial.Ice:
                    multiplier *= iceDrainMultiplier;
                    break;
                case PostInfo.PostSpecial.Snoopdog:
                    multiplier *= snoopDrainMultiplier;
                    break;
            }
        }
        return multiplier;
    }

    private float GetValueMultiplier()
    {
        float multiplier = 1f;
        foreach (var effect in activeEffects)
        {
            if (effect.special == PostInfo.PostSpecial.Ice)
            {
                multiplier *= iceValueMultiplier;
            }
        }
        return multiplier;
    }

    private float GetDeductionMultiplier()
    {
        float multiplier = 1f;
        if (holdChargeActive)
        {
            multiplier *= holdChargeDeductionMultiplier;
        }

        if (IsFastScrolling())
        {
            multiplier *= fastScrollDeductionMultiplier;
        }

        return multiplier;
    }

    private bool IsFastScrolling()
    {
        if (scrollMechanic == null) return false;
        return Mathf.Abs(scrollMechanic.Inertia) >= fastScrollThreshold;
    }

    private void ApplySpecialEffect(PostInfo.PostSpecial special)
    {
        float duration = GetEffectDuration(special);
        if (duration <= 0f) return;

        var effect = new ActiveEffect
        {
            special = special,
            endTime = Time.time + duration
        };
        activeEffects.Add(effect);

        if (special == PostInfo.PostSpecial.Cat)
        {
            StartRainbow();
            constantDecOn = false;
            if (allowCatHealStacking)
            {
                StartCoroutine(CatHealRoutine(duration));
            }
            else if (catHealRoutine == null)
            {
                catHealRoutine = StartCoroutine(CatHealRoutine(duration));
            }
        }
    }

    private float GetEffectDuration(PostInfo.PostSpecial special)
    {
        switch (special)
        {
            case PostInfo.PostSpecial.Fire:
                return fireEffectDuration;
            case PostInfo.PostSpecial.Ice:
                return iceEffectDuration;
            case PostInfo.PostSpecial.Snoopdog:
                return snoopEffectDuration;
            case PostInfo.PostSpecial.Cat:
                return catEffectDuration;
            default:
                return 0f;
        }
    }

    private void CleanupExpiredEffects()
    {
        if (activeEffects.Count == 0) return;

        bool removedAny = false;
        float now = Time.time;
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (activeEffects[i].endTime <= now)
            {
                activeEffects.RemoveAt(i);
                removedAny = true;
            }
        }

        if (removedAny && rainbowActive && !HasActiveEffect(PostInfo.PostSpecial.Cat))
        {
            StopRainbow();
        }

        if (removedAny && !HasActiveEffect(PostInfo.PostSpecial.Cat))
        {
            constantDecOn = true;
        }

    }

    private bool HasActiveEffect(PostInfo.PostSpecial special)
    {
        foreach (var effect in activeEffects)
        {
            if (effect.special == special) return true;
        }
        return false;
    }

    private void StartRainbow()
    {
        if (!enableRainbow || rainbowTargets == null || rainbowTargets.Length == 0) return;
        if (rainbowActive) return;

        rainbowOriginalColors = new Color[rainbowTargets.Length];
        for (int i = 0; i < rainbowTargets.Length; i++)
        {
            rainbowOriginalColors[i] = rainbowTargets[i] != null ? rainbowTargets[i].color : Color.white;
        }

        rainbowActive = true;
    }

    private void UpdateRainbow()
    {
        if (!rainbowActive || !enableRainbow || rainbowTargets == null || rainbowTargets.Length == 0) return;

        float hue = Mathf.Repeat(Time.time * rainbowCycleSpeed, 1f);
        Color rainbow = Color.HSVToRGB(hue, rainbowSaturation, rainbowValue);

        for (int i = 0; i < rainbowTargets.Length; i++)
        {
            if (rainbowTargets[i] != null)
            {
                rainbowTargets[i].color = rainbow;
            }
        }
    }

    private void StopRainbow()
    {
        if (!rainbowActive) return;
        rainbowActive = false;
        RefreshBaseBarVisuals();
        tintCached = false;
    }

    private void UpdateVisualStates()
    {
        if (rainbowActive)
        {
            UpdateRainbow();
            return;
        }

        if (HasActiveEffect(PostInfo.PostSpecial.SpaceInvader) && enableSpaceInvaderTint)
        {
            ApplyTint(spaceInvaderTintColor, GetTintTargets(spaceInvaderTintTargets));
            return;
        }

        if (HasActiveEffect(PostInfo.PostSpecial.Fire) && enableFireTint)
        {
            ApplyTint(fireTintColor, GetTintTargets(fireTintTargets));
            return;
        }

        if (HasActiveEffect(PostInfo.PostSpecial.Ice) && enableIceTint)
        {
            ApplyTint(iceTintColor, GetTintTargets(iceTintTargets));
            return;
        }

        if (HasActiveEffect(PostInfo.PostSpecial.Snoopdog) && enableSnoopTint)
        {
            ApplyTint(snoopTintColor, GetTintTargets(snoopTintTargets));
            return;
        }

        RestoreTint(GetTintTargets(null));
    }

    private Graphic[] GetTintTargets(Graphic[] overrideTargets)
    {
        if (overrideTargets != null && overrideTargets.Length > 0) return overrideTargets;
        if (snoopTintTargets != null && snoopTintTargets.Length > 0) return snoopTintTargets;
        if (rainbowTargets != null && rainbowTargets.Length > 0) return rainbowTargets;
        return null;
    }

    private void CacheTintOriginalColors(Graphic[] targets)
    {
        if (tintCached) return;
        if (targets == null || targets.Length == 0) return;

        tintOriginalColors = new Color[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            tintOriginalColors[i] = targets[i] != null ? targets[i].color : Color.white;
        }
        tintCached = true;
    }

    private void ApplyTint(Color tintColor, Graphic[] targets)
    {
        if (targets == null || targets.Length == 0) return;
        CacheTintOriginalColors(targets);

        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].color = tintColor;
            }
        }
    }

    private void RestoreTint(Graphic[] targets)
    {
        if (!tintCached || targets == null || tintOriginalColors == null) return;

        for (int i = 0; i < targets.Length && i < tintOriginalColors.Length; i++)
        {
            if (targets[i] != null)
            {
                targets[i].color = tintOriginalColors[i];
            }
        }

        RefreshBaseBarVisuals();
    }

    private void RefreshBaseBarVisuals()
    {
        if (dopBar == null) return;
        dopBar.UpdateBar(dopBar.CurrentValue, true);
    }

    private IEnumerator CatHealRoutine(float duration)
    {
        if (duration <= 0f || catHealPercent <= 0f) yield break;

        float totalHeal = maxDopValue * catHealPercent;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float delta = Time.deltaTime;
            float healStep = (totalHeal / duration) * delta;
            changeDop(healStep, skipCatHealAnimation);
            elapsed += delta;
            yield return null;
        }

        if (!allowCatHealStacking)
        {
            catHealRoutine = null;
        }
    }
}
