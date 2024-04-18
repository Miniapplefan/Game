using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SiphonModel : SystemModel
{
    public SiphonTarget siphonTarget;
    public bool extended = false;
    public Transform head;

    float extendedTime = 5;
    float retractTime = 3;
    float currentTimer = 0;

    float maxSiphonDistance = 2;
    int siphonLayerMask = ~((1 << 6) | (1 << 7));

    public float dollars = 0;

    public SiphonModel(int currentLvl, Transform h) : base(currentLvl)
    {
        head = h;
    }

    public override void SetNameAndMaxLevel()
    {
        name = BodyInfo.systemID.Siphon;
        maxLevel = 6;
    }

    protected override void InitCommands()
    {  
    }

    bool isLookingAtSiphonTarget()
    {
        RaycastHit hit;
        if (Physics.Raycast(head.position, head.forward, out hit, maxSiphonDistance, siphonLayerMask))
        {
            Debug.Log(hit.transform.gameObject.layer);
            if (hit.transform.gameObject.GetComponent<SiphonTarget>() != null)
            {
                //Debug.DrawRay(head.position, head.forward * hit.distance, Color.red);
                return true;
            }
            else
            {
                return false;
            }
        }
        return false;
    }

    SiphonTarget currentlyLookedAtSiphonTarget()
    {
        RaycastHit hit;
        if (Physics.Raycast(head.position, head.forward, out hit, maxSiphonDistance, siphonLayerMask))
        {
            Debug.Log(hit.transform.gameObject.layer);
            if (hit.transform.gameObject.GetComponent<SiphonTarget>() != null)
            {
                return hit.transform.gameObject.GetComponent<SiphonTarget>();
            }
            else
            {
                return null;
            }
        }
        return null;
    }

    public void NotSiphoning()
    {
        currentTimer = 0;
    }

    public int getSiphoningRate()
    {
        return currentLevel / currentLevelWithoutDamage;
    }

    public void addDollars(float amount)
    {
        dollars += amount;
    }

    public void ToggleSiphon()
    {
        if (extended)
        {
            Debug.Log("extended");
            if(currentTimer >= retractTime)
            {
                extended = false;
                siphonTarget.notBeingSiphoned();
                siphonTarget.siphoner = null;
                siphonTarget = null;
                currentTimer = 0;
                Debug.Log("Siphon Retracted");
            }
            else
            {
                currentTimer += Time.deltaTime;
                Debug.Log("Retracting Siphon");
            }
        }
        else
        {
            Debug.Log("retracted");
            if (isLookingAtSiphonTarget())
            {
                Debug.Log("looking at valid target");
                if (currentTimer >= extendedTime)
                {
                    extended = true;
                    siphonTarget = currentlyLookedAtSiphonTarget();
                    siphonTarget.siphoner = this;
                    siphonTarget.beingSiphoned();
                    currentTimer = 0;
                    Debug.Log("Siphon Extended");
                }
                else
                {
                    currentTimer += Time.deltaTime;
                    Debug.Log("Extending Siphon");
                }
            }
            Debug.Log("no valid target");
        }
    }
}
