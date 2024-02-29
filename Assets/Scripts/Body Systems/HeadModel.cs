using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadModel : SystemModel
{
    public int health;

    public delegate void HeadEventHandler();
    public event HeadEventHandler RaiseDeath;

    public HeadModel(int currentLvl) : base(currentLvl)
    {
        health = currentLvl * 10;
    }

    public override void Damage(int amount)
    {
        health -= amount;

        if(health <= 0)
        {
            RaiseDeath?.Invoke();
        }
    }

    public override void SetNameAndMaxLevel()
    {
        name = BodyInfo.systemID.Head;
        maxLevel = 5;
    }

    protected override void InitCommands()
    {
    }
}
