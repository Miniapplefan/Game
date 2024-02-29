using System.Collections;
using System.Collections.Generic;
using Lean.Pool;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public GunDataScriptableObject gunData;

    private Rigidbody weapon;
    private ParticleSystem shootSystem;
    private float lastShootTime;
    private GameObject weaponSlotLocation;

    public void SetParent(GameObject parent, Rigidbody weap)
    {
        weaponSlotLocation = parent;
        GameObject model = Instantiate(gunData.ModelPrefab);
        model.transform.SetParent(weaponSlotLocation.transform, false);
        model.transform.localPosition = gunData.SpawnPoint;
        model.transform.localRotation = Quaternion.Euler(gunData.SpawnRotation);

        shootSystem = model.GetComponentInChildren<ParticleSystem>();
        weapon = weap;
    }

    // Start is called before the first frame update
    void Start()
    {
        lastShootTime = 0;

        GameObject model = Instantiate(gunData.ModelPrefab);
        model.transform.SetParent(weaponSlotLocation.transform, false);
        model.transform.localPosition = gunData.SpawnPoint;
        model.transform.localRotation = Quaternion.Euler(gunData.SpawnRotation);

        shootSystem = model.GetComponentInChildren<ParticleSystem>();
    }

    public bool Shoot()
    {
        //Debug.Log(Model.transform.position);
        if (Time.time > gunData.shootConfig.FireRate + lastShootTime)
        {
            lastShootTime = Time.time;
            shootSystem.Play();
            Vector3 shootDirection = shootSystem.transform.forward
                + new Vector3(
                    Random.Range(
                        -gunData.shootConfig.Spread.x,
                        gunData.shootConfig.Spread.x
                    ),
                    Random.Range(
                        -gunData.shootConfig.Spread.y,
                        gunData.shootConfig.Spread.y
                    ),
                    Random.Range(
                        -gunData.shootConfig.Spread.z,
                        gunData.shootConfig.Spread.z
                    )
                );
            shootDirection.Normalize();
            weapon.AddRelativeForce(weapon.transform.up * gunData.shootConfig.recoil, ForceMode.Impulse);

            if (Physics.Raycast(
                    shootSystem.transform.position,
                    shootDirection,
                    out RaycastHit hit,
                    float.MaxValue,
                    gunData.shootConfig.HitMask
                ))
            {
                StartCoroutine(
                    PlayTrail(
                        shootSystem.transform.position,
                        hit.point,
                        hit
                    )
                );
                ManageHit(hit);
            }
            else
            {
                StartCoroutine(
                    PlayTrail(
                        shootSystem.transform.position,
                        shootSystem.transform.position + (shootDirection * gunData.trailConfig.MissDistance),
                        new RaycastHit()
                    )
                );
            }
            return true;
        }
        return false;
    }

    private void ManageHit(RaycastHit hit)
    {
        Rigidbody hitRb = hit.collider.GetComponent<Rigidbody>();
        LimbToSystemLinker limb = hit.collider.GetComponent<LimbToSystemLinker>();

        if (hitRb != null)
        {
            Debug.Log("hit rb");
            Vector3 impulse = shootSystem.transform.forward * gunData.shootConfig.impactForce;
            hitRb.AddForce(impulse, ForceMode.Impulse);
        }

        if(limb != null)
        {
            Debug.Log("hit limb");
            limb.TakeDamage(new DamageInfo(gunData.shootConfig.heatPerShot));
        }
    }

    private IEnumerator PlayTrail(Vector3 StartPoint, Vector3 EndPoint, RaycastHit Hit)
    {
        //TrailRenderer instance = TrailPool.Get();
        //instance.gameObject.SetActive(true);
        //instance.transform.position = StartPoint;
        TrailRenderer instance = LeanPool.Spawn(CreateTrail());
        instance.transform.position = StartPoint;
        yield return null; // avoid position carry-over from last frame if reused

        instance.emitting = true;

        float distance = Vector3.Distance(StartPoint, EndPoint);
        float remainingDistance = distance;
        while (remainingDistance > 0)
        {
            instance.transform.position = Vector3.Lerp(
                StartPoint,
                EndPoint,
                Mathf.Clamp01(1 - (remainingDistance / distance))
            );
            remainingDistance -= gunData.trailConfig.SimulationSpeed * Time.deltaTime;

            yield return null;
        }

        instance.transform.position = EndPoint;

        if (Hit.collider != null)
        {
            //SurfaceManager.Instance.HandleImpact(
            //    Hit.transform.gameObject,
            //    EndPoint,
            //    Hit.normal,
            //    ImpactType,
            //    0
            //);
        }

        yield return new WaitForSeconds(gunData.trailConfig.Duration);
        yield return null;
        instance.emitting = false;
        instance.gameObject.SetActive(false);
        //TrailPool.Release(instance);
        LeanPool.Despawn(instance);
    }

    private TrailRenderer CreateTrail()
    {
        GameObject instance = new GameObject("Bullet Trail");
        TrailRenderer trail = instance.AddComponent<TrailRenderer>();
        trail.colorGradient = gunData.trailConfig.Color;
        trail.material = gunData.trailConfig.Material;
        trail.widthCurve = gunData.trailConfig.WidthCurve;
        trail.time = gunData.trailConfig.Duration;
        trail.minVertexDistance = gunData.trailConfig.MinVertexDistance;

        trail.emitting = false;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        return trail;
    }
}
