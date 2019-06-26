using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotionManager : MonoBehaviour
{
    [HideInInspector]
    public SimulationManager simulationManager;
    [HideInInspector]
    public SimulatedWalker simulatedWalker;
    [HideInInspector]
    public KeyboardController keyboardController;

    [HideInInspector]
    public Transform targetWaypoint;
    [HideInInspector]
    public List<Vector2> waypoints;
    [HideInInspector]
    public int waypointIterator = 0;

    [Tooltip("How user movement is controlled.")]
    public MovementController MOVEMENT_CONTROLLER = MovementController.Tracker;

    [Tooltip("Tangent speed in auto-pilot mode")]
    public float speedReal;

    [Tooltip("Angular speed in auto-pilot mode")]
    public float angularSpeedReal;

    [SerializeField]
    public float DISTANCE_TO_WAYPOINT_THRESHOLD = 0.3f; // Distance requirement to trigger waypoint

    public enum MovementController { Keyboard, AutoPilot, Tracker };


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InstantiateSimulationPrefab()
    {
        Transform waypoint = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
        Destroy(waypoint.GetComponent<SphereCollider>());
        this.targetWaypoint = waypoint;
        waypoint.name = "Simulated Waypoint";
        waypoint.position = 1.2f * Vector3.up + 1000 * Vector3.forward;
        waypoint.localScale = 0.3f * Vector3.one;
        waypoint.GetComponent<Renderer>().material.color = new Color(0, 1, 0);
        waypoint.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(0, 0.12f, 0));
    }

    public void SetReferenceForSimulatedWalker()
    {
        if (simulatedWalker != null)
        {
            simulatedWalker.motionManager = this;
            simulatedWalker.simulationManager = this.simulationManager;
            simulatedWalker.redirectionManager = this.simulationManager.redirectionManager;
        }
    }

    public void SetReferenceForKeyboardController()
    {
        if (keyboardController != null)
        {
            keyboardController.motionManager = this;
            keyboardController.redirectionManager = this.simulationManager.redirectionManager;
        }
    }

    public void GetSimulatedWalker()
    {
        simulatedWalker = simulationManager.simulatedHead.GetComponent<SimulatedWalker>();
    }

    public void GetKeyboardController()
    {
        keyboardController = simulationManager.simulatedHead.GetComponent<KeyboardController>();
    }

    public void GetTargetWaypoint()
    {
        targetWaypoint = transform.Find("Target Waypoint").gameObject.transform;
    }
}
