using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manage the tracked space and the virtual scene
/// </summary>
public class EnvManager : MonoBehaviour
{
    [HideInInspector]
    public SimulationManager simulationManager;

    [HideInInspector]
    public Transform trackedSpace;

    [HideInInspector]
    public float roomX; // room size in x
    [HideInInspector]
    public float roomZ; // room size in z
    [HideInInspector]
    public Vector2[] roomCorners; // the 4 corners of the room

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }



    public void GetTrackedSpace()
    {
        trackedSpace = transform.Find("Tracked Space");
        this.roomX = this.trackedSpace.localScale.x;
        this.roomZ = this.trackedSpace.localScale.z;
        this.roomCorners = new Vector2[4];
        this.roomCorners[0] = new Vector2(roomX / 2, roomZ / 2);
        this.roomCorners[1] = new Vector2(roomX / 2, -roomZ / 2);
        this.roomCorners[2] = new Vector2(-roomX / 2, -roomZ / 2);
        this.roomCorners[3] = new Vector2(-roomX / 2, roomZ / 2);
    }

    public void UpdateTrackedSpaceDimensions(float x, float z)
    {
        trackedSpace.localScale = new Vector3(x, 1, z);
        this.roomX = trackedSpace.localScale.x;
        this.roomZ = trackedSpace.localScale.z;
        this.roomCorners[0] = new Vector2(roomX / 2, roomZ / 2);
        this.roomCorners[1] = new Vector2(roomX / 2, -roomZ / 2);
        this.roomCorners[2] = new Vector2(-roomX / 2, -roomZ / 2);
        this.roomCorners[3] = new Vector2(-roomX / 2, roomZ / 2);

        simulationManager.redirectionManager.resetTrigger.Initialize();
        if (simulationManager.redirectionManager.resetter != null)
            simulationManager.redirectionManager.resetter.Initialize();
    }
}
