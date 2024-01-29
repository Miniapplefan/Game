using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponsModel : SystemModel
{
    public Weapon weapon1;
    public Weapon weapon2;
    public Weapon weapon3;

    public WeaponsModel(int currentLvl) : base(currentLvl)
    {
    }

    public override void SetNameAndMaxLevel()
    {
        name = BodyInfo.systemID.Weapons;
        maxLevel = 12;
    }

    protected override void InitCommands()
    {
        weapon1 = new Weapon();
        weapon2 = new Weapon();
        weapon3 = new Weapon();
    }

    public void ExecuteWeapon1()
    {
        weapon1.Fire();
        Debug.Log("Fire 1");
    }

    public void ExecuteWeapon2()
    {
        weapon2.Fire();
        Debug.Log("Fire 2");
    }

    public void ExecuteWeapon3()
    {
        weapon3.Fire();
        Debug.Log("Fire 3");
    }
}
