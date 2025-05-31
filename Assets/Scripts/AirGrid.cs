using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AirGrid : MonoBehaviour
{
    [SerializeField] private BoxCollider airCollider;
    [SerializeField] private bool drawDebug = false;
    public GameObject debugPos;
    public float startingTemp = 21f;

    private Vector3Int gridDimensions;
    private Vector3 origin;
    public float[,,] temperatureGrid;
    int[] dx = { 1, -1, 0, 0, 0, 0 };
    int[] dy = { 0, 0, 1, -1, 0, 0 };
    int[] dz = { 0, 0, 0, 0, 1, -1 };
    private Queue<Vector3Int> modifiedCubes = new Queue<Vector3Int>();
    private HashSet<Vector3Int> modifiedCubesSet = new HashSet<Vector3Int>(); // For faster Contains checks
    private int diffusionStepCounter = 0;
    private int diffusionInterval;
    [SerializeField] private int cubesToProcessPerFrame = 20; // Adjust this value
    public float diffusionRate = 10;
    public float diffusionThreshold = 0.1f;
    public HeatMaterialScriptableObject heatMaterial;

    public HeatMaterialScriptableObject structuralHeatMaterial;

    private HeatContainer structuralHeatContainer;

    void Start()
    {
        InitializeAirGrid();
    }

    void FixedUpdate()
    {
        diffusionStepCounter++;
        if (diffusionStepCounter >= diffusionInterval)
        {
            diffusionStepCounter = 0;
            PerformDiffusionStep();
        }
    }

    void InitializeAirGrid()
    {
        if (airCollider == null)
        {
            Debug.LogError("AirGridInitializer: No BoxCollider assigned.");
            return;
        }

        // Step 1: Get the world size
        Vector3 worldSize = Vector3.Scale(airCollider.size, airCollider.transform.lossyScale);

        // Step 2: Round to integers for grid dimensions
        gridDimensions = new Vector3Int(
            Mathf.CeilToInt(worldSize.x),
            Mathf.CeilToInt(worldSize.y),
            Mathf.CeilToInt(worldSize.z)
        );

        // Step 3: Determine the origin (minimum corner of the collider in world space)
        Vector3 localMinCorner = airCollider.center - airCollider.size * 0.5f;
        origin = airCollider.transform.TransformPoint(localMinCorner);

        // Step 4: Allocate the grid
        temperatureGrid = new float[gridDimensions.x, gridDimensions.y, gridDimensions.z];

        // Step 5: Initialize temperature values (e.g. ambient temp)
        float ambientTemperature = startingTemp;
        for (int x = 0; x < gridDimensions.x; x++)
            for (int y = 0; y < gridDimensions.y; y++)
                for (int z = 0; z < gridDimensions.z; z++)
                {
                    temperatureGrid[x, y, z] = ambientTemperature;
                }

        createStruturalHeatContainer();
        Debug.Log($"Air grid initialized: {gridDimensions.x} x {gridDimensions.y} x {gridDimensions.z}");
    }

    void createStruturalHeatContainer()
    {
        var structuralObject = new GameObject("Structure");
        structuralObject.AddComponent<BoxCollider>();
        structuralObject.AddComponent<HeatContainer>();
        var structuralHeatCont = structuralObject.GetComponent<HeatContainer>();
        structuralHeatCont.heatMat = structuralHeatMaterial;
        structuralHeatCont.InitFromHeatMaterialSO();
        structuralHeatCont.mass = structuralHeatCont.GetColliderVolume(airCollider) * structuralHeatMaterial.mass;
        structuralHeatCont.currentTemperature = startingTemp;

        structuralHeatContainer = structuralHeatCont;
    }

    public Vector3Int WorldToVoxel(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - origin; // Account for the grid's origin
        int x = Mathf.FloorToInt(localPos.x / 1);
        int y = Mathf.FloorToInt(localPos.y / 1);
        int z = Mathf.FloorToInt(localPos.z / 1);
        return GetPooledVector3Int(x, y, z);
    }

    public Vector3 VoxelToWorldCenter(Vector3Int voxel)
    {
        return origin + new Vector3(
            (voxel.x + 0.5f) * 1,
            (voxel.y + 0.5f) * 1,
            (voxel.z + 0.5f) * 1
        );
    }

    public List<Vector3Int> GetCollidingAirCubes(Collider otherCollider)
    {
        List<Vector3Int> collidingCubes = new List<Vector3Int>();

        Bounds otherBounds = otherCollider.bounds;

        // Convert the min and max world bounds of the other collider to voxel coordinates
        Vector3Int minVoxel = WorldToVoxel(otherBounds.min);
        Vector3Int maxVoxel = WorldToVoxel(otherBounds.max);

        // Clamp the voxel range to ensure it's within the bounds of our air grid
        int minX = Mathf.Max(0, minVoxel.x);
        int minY = Mathf.Max(0, minVoxel.y);
        int minZ = Mathf.Max(0, minVoxel.z);
        int maxX = Mathf.Min(gridDimensions.x - 1, maxVoxel.x);
        int maxY = Mathf.Min(gridDimensions.y - 1, maxVoxel.y);
        int maxZ = Mathf.Min(gridDimensions.z - 1, maxVoxel.z);

        // Iterate over the potential range of colliding voxels
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    // Get the world position of the center of the current air cube
                    Vector3 airCubeCenter = VoxelToWorldCenter(GetPooledVector3Int(x, y, z));
                    // Since each air cube is 1x1x1, its extents are 0.5 in each direction
                    Vector3 airCubeHalfExtents = Vector3.one * 0.5f;
                    Bounds airCubeBounds = new Bounds(airCubeCenter, airCubeHalfExtents * 2); // Size is 2 * halfExtents

                    // Check if the bounds of the other collider intersect with the bounds of the current air cube
                    if (otherBounds.Intersects(airCubeBounds))
                    {
                        collidingCubes.Add(GetPooledVector3Int(x, y, z));
                    }
                }
            }
        }


        ReturnToPool(minVoxel);
        ReturnToPool(maxVoxel);
        return collidingCubes;
    }

    public float GetTemperature(int x, int y, int z)
    {
        return temperatureGrid[x, y, z];
    }

    void IncreaseHeat(int x, int y, int z, float amount)
    {
        Vector3Int modifiedVoxel = GetPooledVector3Int(x, y, z);
        if (!modifiedCubesSet.Contains(modifiedVoxel))
        {
            modifiedCubes.Enqueue(modifiedVoxel);
            modifiedCubesSet.Add(modifiedVoxel);
        }
        ReturnToPool(modifiedVoxel);

        float temperatureChange = amount / (1 * heatMaterial.specificHeatCapacity);
        temperatureGrid[x, y, z] += temperatureChange;
    }

    public void ApplyNewtonsLawOfCooling(HeatContainer otherContainer, int numCubes, int x, int y, int z)
    {
        // Get the temperatures of both containers
        float airTemperature = GetTemperature(x, y, z);
        float bodyTemperature = otherContainer.GetTemperature();

        // Calculate the temperature difference
        float temperatureDifference = bodyTemperature - airTemperature;

        // Mech overheating case
        if (otherContainer.coolingModel != null && otherContainer.coolingModel.isOverheated)
        {
            // Mech is overheated and should dissipate heat below ambient temperature until it reaches its minimumTemperature
            float minTemperature = otherContainer.coolingModel.minimumTemperature;
            //temperatureDifference = bodyTemperature - Mathf.Min(airTemperature, minTemperature); // Allow cooling down to minTemperature

            // Apply Newton's law: heatTransfer = coolingConstant * (temp difference) * dissipationRate * Time.deltaTime
            float heatTransfer = otherContainer.GetCoolingConstant(heatMaterial) * (otherContainer.dissipationRate) * Time.deltaTime;

            // Calculate the temperature change for the body and air based on their specific heat capacity and mass
            float bodyTempChange = heatTransfer / (otherContainer.mass * otherContainer.specificHeatCapacity);
            float airTempChange = heatTransfer / (1 * heatMaterial.specificHeatCapacity);

            // Exchange heat
            otherContainer.IncreaseHeat(this, -heatTransfer / numCubes);
            IncreaseHeat(x, y, z, heatTransfer / numCubes);

            // Update temperatures
            // bodyContainer.currentTemperature -= bodyTempChange;
            // airContainer.currentTemperature += airTempChange;

            // Clamp the body temperature to the minimumTemperature to prevent overcooling below the limit
            otherContainer.currentTemperature = Mathf.Max(otherContainer.currentTemperature, minTemperature);

        }
        else
        {
            if (Mathf.Abs(temperatureDifference) > 1f)  // Only transfer heat if there's a significant difference
            {
                // Determine which way the heat flows
                //dissipationRateFromAirCurrents
                float heatTransfer = otherContainer.GetCoolingConstant(heatMaterial) * otherContainer.dissipationRateFromAirCurrents * Mathf.Abs(temperatureDifference) * Time.deltaTime;

                //					Debug.Log(heatTransfer);

                // If the Objects is hotter than the Air, heat should flow from the Object to the Air
                if (temperatureDifference > 0)
                {
                    // Mechs have active cooling which allows them to dissipate heat faster into the air
                    heatTransfer *= otherContainer.dissipationRate;
                    // Heat flows from Object to Air
                    float bodyTempChange = heatTransfer / (otherContainer.mass * otherContainer.specificHeatCapacity);
                    float airTempChange = heatTransfer / (1 * heatMaterial.specificHeatCapacity);

                    // Exchange heat
                    otherContainer.IncreaseHeat(this, -heatTransfer / numCubes);
                    IncreaseHeat(x, y, z, heatTransfer / numCubes);

                    // // Update temperatures
                    // bodyContainer.currentTemperature -= bodyTempChange;
                    // airContainer.currentTemperature += airTempChange;
                }
                else
                {
                    // Heat flows from Air to Object (Air is hotter)
                    float bodyTempChange = heatTransfer / (otherContainer.mass * otherContainer.specificHeatCapacity);
                    float airTempChange = heatTransfer / (1 * heatMaterial.specificHeatCapacity);

                    // Exchange heat
                    otherContainer.IncreaseHeat(this, heatTransfer / numCubes);
                    IncreaseHeat(x, y, z, -heatTransfer / numCubes);

                    // // Update temperatures
                    // bodyContainer.currentTemperature += bodyTempChange;
                    // airContainer.currentTemperature -= airTempChange;
                }

                // Ensure temperatures remain within realistic bounds
                //otherContainer.currentTemperature = Mathf.Max(otherContainer.currentTemperature, otherContainer.ambientTemperature);  // Prevent Mechs from cooling below ambient unless overheated
                temperatureGrid[x, y, z] = Mathf.Max(GetTemperature(x, y, z), 0);  // Prevent Air from going below 0 (or any desired minimum)
            }
        }

        // Optionally handle any extreme heat conditions (such as additional cooling effects)
        if (otherContainer.coolingModel != null)
        {
            otherContainer.coolingModel.HandleHeatExtremes(otherContainer.currentTemperature, otherContainer.maxTemperature);
        }
    }

    public void PerformDiffusionStep()
    {
        diffusionStepCounter = Random.Range(5, 10);
        Queue<Vector3Int> cubesToDiffuseNext = new Queue<Vector3Int>();
        HashSet<Vector3Int> nextCubesSet = new HashSet<Vector3Int>(); // To avoid duplicates in the next step

        int processedCount = 0;

        while (modifiedCubes.Count > 0 && processedCount < cubesToProcessPerFrame)
        {
            Vector3Int currentVoxel = modifiedCubes.Dequeue();
            int x = currentVoxel.x;
            int y = currentVoxel.y;
            int z = currentVoxel.z;

            float currentTemp = GetTemperature(x, y, z);

            for (int i = 0; i < 6; i++)
            {
                int nx = x + dx[i];
                int ny = y + dy[i];
                int nz = z + dz[i];

                if (nx >= 0 && nx < gridDimensions.x && ny >= 0 && ny < gridDimensions.y && nz >= 0 && nz < gridDimensions.z)
                {
                    float neighborTemp = GetTemperature(nx, ny, nz);
                    float tempDifference = Mathf.Abs(currentTemp - neighborTemp);

                    if (tempDifference > diffusionThreshold)
                    {
                        float heatTransfer = diffusionRate * tempDifference; // Adjust the rate as needed

                        // Transfer heat (simplified - you might want to consider specific heat)
                        if (currentTemp > neighborTemp)
                        {
                            temperatureGrid[x, y, z] -= heatTransfer * 0.5f;
                            temperatureGrid[nx, ny, nz] += heatTransfer * 0.5f;
                        }
                        else
                        {
                            temperatureGrid[x, y, z] += heatTransfer * 0.5f;
                            temperatureGrid[nx, ny, nz] -= heatTransfer * 0.5f;
                        }

                        Vector3Int neighborVoxel = GetPooledVector3Int(nx, ny, nz);
                        if (!nextCubesSet.Contains(neighborVoxel))
                        {
                            cubesToDiffuseNext.Enqueue(neighborVoxel);
                            nextCubesSet.Add(neighborVoxel);
                        }
                        ReturnToPool(neighborVoxel);
                    }
                }
                else
                {
                    float structureTemp = structuralHeatContainer.GetTemperature();
                    float tempDifference = Mathf.Abs(currentTemp - structureTemp);

                    if (tempDifference > diffusionThreshold)
                    {
                        float heatTransfer = diffusionRate * tempDifference; // Adjust the rate as needed

                        // Transfer heat (simplified - you might want to consider specific heat)
                        if (currentTemp > structureTemp)
                        {
                            temperatureGrid[x, y, z] -= heatTransfer * 0.5f;
                            structuralHeatContainer.IncreaseHeat(this, heatTransfer * 0.5f);
                        }
                        else
                        {
                            temperatureGrid[x, y, z] += heatTransfer * 0.5f;
                            structuralHeatContainer.IncreaseHeat(this, -heatTransfer * 0.5f);
                        }

                        if (!nextCubesSet.Contains(currentVoxel))
                        {
                            cubesToDiffuseNext.Enqueue(currentVoxel);
                            nextCubesSet.Add(currentVoxel);
                        }
                    }
                }
            }
        }

        while (modifiedCubes.Count > 0)
        {
            cubesToDiffuseNext.Enqueue(modifiedCubes.Dequeue());
        }
        modifiedCubes = cubesToDiffuseNext;
    }

    private Stack<Vector3Int> vector3IntPool = new Stack<Vector3Int>();

    private Vector3Int GetPooledVector3Int(int x, int y, int z)
    {
        if (vector3IntPool.Count > 0)
        {
            var vec = vector3IntPool.Pop();
            vec.x = x; vec.y = y; vec.z = z;
            return vec;
        }
        return new Vector3Int(x, y, z);
    }

    private void ReturnToPool(Vector3Int vec)
    {
        vector3IntPool.Push(vec);
    }

    void OnDrawGizmos()
    {
        if (!drawDebug || temperatureGrid == null) return;

        Gizmos.color = new Color(0f, 0.5f, 1f, 0.1f);
        for (int x = 0; x < gridDimensions.x; x++)
            for (int y = 0; y < gridDimensions.y; y++)
                for (int z = 0; z < gridDimensions.z; z++)
                {
                    Vector3 worldPos = origin + new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);
                    Gizmos.color = new Color(1f * (temperatureGrid[x, y, z] / 100), 0f, 1f / (temperatureGrid[x, y, z] / 100), 0.5f * temperatureGrid[x, y, z] / 200);
                    Gizmos.DrawCube(worldPos, Vector3.one);
                }

        if (debugPos != null)
        {
            Collider debugCollider = debugPos.GetComponent<Collider>();
            if (debugCollider != null)
            {
                List<Vector3Int> colliding = GetCollidingAirCubes(debugCollider);
                Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
                foreach (var voxel in colliding)
                {
                    Gizmos.DrawCube(VoxelToWorldCenter(voxel), Vector3.one);
                }
            }
        }
    }
}
