using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using static Lean.Pool.LeanGameObjectPool;

public class CoolingModel : SystemModel
{
    public Rigidbody rb;
    public float currentHeat = 0;
    float maxHeatMultiplier = 10f;

    public bool isOverheated = false;
    public bool delayElapsed = false;
    private float coolingStartDelay = 0.1f;
    private float coolingStartDelayTemp = 0;
    private float coolingAmountMultiplier = 0.2f;
    private float coolingAmountMultiplierPassive = 0.1f;
    private float coolingAmountMultiplierOverheated = 3f;


    public delegate void CoolingEventHandler();
    public event CoolingEventHandler RaiseIncreasedHeat;
    public event CoolingEventHandler RaiseOverheated;
    public event CoolingEventHandler RaiseCooledDownFromOverheat;

    public CoolingModel(int currentLvl, Rigidbody r) : base(currentLvl)
    {
        rb = r;
    }

    public override void SetNameAndMaxLevel()
    {
        name = BodyInfo.systemID.Cooling;
        maxLevel = 12;
    }

    protected override void InitCommands()
    {
    }

    public float getMaxHeat()
    {
        return currentLevelWithoutDamage * maxHeatMultiplier;
    }

    public float getCoolingAmount()
    {
        return currentLevel * coolingAmountMultiplier;
    }

    public float getPassiveCoolingAmount()
    {
        return currentLevelWithoutDamage * coolingAmountMultiplierPassive;
    }

    public void IncreaseHeat(object e, float amount)
    {
        ResetCooldown();
        if (!isOverheated)
        {
            currentHeat = Mathf.Clamp(currentHeat += amount, 0, getMaxHeat());
            if (currentHeat >= getMaxHeat())
            {
                Debug.Log("Overheated!");
                isOverheated = true;
                RaiseOverheated?.Invoke();
            }
        }
        RaiseIncreasedHeat?.Invoke();
    }

    private void DecreaseCoolingStartDelay()
    {
        if (coolingStartDelayTemp > 0)
        {
            coolingStartDelayTemp -= Time.deltaTime;
        }
        else
        {
            delayElapsed = true;
        }
    }

    public void PassiveCooldown()
    {
        if (currentHeat > 0)
        {
            currentHeat = Mathf.Clamp(currentHeat -= getPassiveCoolingAmount() * Time.deltaTime, 0, getMaxHeat());
        }
        if (currentHeat <= 0)
        {
            RaiseCooledDownFromOverheat?.Invoke();
            isOverheated = false;
        }
    }

    public void Cooldown()
    {
        if (delayElapsed)
        {
            if (currentHeat > 0)
            {
                currentHeat = Mathf.Clamp(currentHeat -= getCoolingAmount() * Time.deltaTime, 0, getMaxHeat());
                //Debug.Log("Normal Cooldown");
            }
            else
            {
                RaiseCooledDownFromOverheat?.Invoke();
                isOverheated = false;
            }
        }
        else
        {
            DecreaseCoolingStartDelay();
        }
    }

    public void CooldownOverheated()
    {
        if (currentHeat > 0)
        {
            currentHeat = Mathf.Clamp(currentHeat -= (coolingAmountMultiplierOverheated * currentLevelWithoutDamage) * Time.deltaTime, 0, getMaxHeat());
            //Debug.Log("Overheat Cooldown");
        }
        else
        {
            RaiseCooledDownFromOverheat?.Invoke();
            isOverheated = false;
        }
    }

    public void ResetCooldown()
    {
        delayElapsed = false;
        coolingStartDelayTemp = coolingStartDelay;
    }

    //public IEnumerator DecreaseHeatCoroutine()
    //{
    //    yield return new WaitForSeconds(coolingStartDelay);

    //    while (currentHeat > 0)
    //    {
    //        currentHeat -= getCoolingAmount() * Time.deltaTime;
    //        yield return null;
    //    }

    //    // Optional: Do something when heat reaches zero
    //    //Debug.Log("Heat has reached zero!");
    //    //isCoolingDown = false;
    //    RaiseCooledDownFromOverheat?.Invoke();
    //}

}
