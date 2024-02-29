using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class GunSelector : MonoBehaviour
{
    [SerializeField]
    private GunType Gun1;
    [SerializeField]
    private GameObject Gun1Parent;
    [SerializeField]
    private GunType Gun2;
    [SerializeField]
    private GameObject Gun2Parent;
    [SerializeField]
    private GunType Gun3;
    [SerializeField]
    private GameObject Gun3Parent;
    [SerializeField]
    private List<GunDataScriptableObject> Guns;
    [SerializeField]
    private Rigidbody weapon;

    [Space]
    [Header("Runtime Filled")]
    public Gun ActiveGun1;
    public Gun ActiveGun2;
    public Gun ActiveGun3;

    private void Start()
    {
        ActiveGun1 = CreateGun(Gun1, Gun1Parent);
        ActiveGun2 = CreateGun(Gun2, Gun2Parent);
        ActiveGun3 = CreateGun(Gun3, Gun3Parent);
        Debug.Log("created guns");
    }

    private Gun CreateGun(GunType type, GameObject slot) 
    {

        //Guns[index].Spawn(slot, this);
        //return Guns[index];
        GunDataScriptableObject gunData = Guns.Find(gun => gun.type == type);

        //if (gun == null)
        //{
        //    Debug.LogError($"No GunScriptableObject found for GunType: {gun}");
        //    return null;
        //}

        //gun.Spawn(slot, this);
        //return gun;

        GameObject gunObject = new GameObject(gunData.GunName);
        Gun gun = gunObject.AddComponent<Gun>();
        gun.gunData = gunData;
        gun.SetParent(slot, weapon);
        return gun;
    }
}

