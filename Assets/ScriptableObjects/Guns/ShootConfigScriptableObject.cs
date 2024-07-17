using UnityEngine;

[CreateAssetMenu(fileName = "Shoot Config", menuName = "Guns/Shoot Config", order = 2)]
public class ShootConfigScriptableObject : ScriptableObject
{
    public int powerRequired = 1;
    public LayerMask HitMask;
    public Vector3 Spread = new Vector3(0.1f, 0.1f, 0.1f);
    public bool isBurst = true;
    public int burst_numShots = 3;
    public float burst_delayBetweenShots = 0.2f;
    public float fireRate = 0.25f;
    public float recoil = 100f;
    public float impactForce = 10f;
    public float heatPerShot = 2f;
}
