using Redirection;
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

    enum PathSeedChoice { Line, Office, ExplorationSmall, ExplorationLarge, LongWalk, ZigZag };

    [SerializeField]
    VirtualPathGenerator.PathSeedChoice condPath;

    VirtualPathGenerator.PathSeed currentPathSeed;

    [Tooltip("How user movement is controlled.")]
    public MovementController MOVEMENT_CONTROLLER = MovementController.Tracker;

    [Tooltip("Tangent speed in auto-pilot mode")]
    public float speedReal;

    [Tooltip("Angular speed in auto-pilot mode")]
    public float angularSpeedReal;

    [SerializeField]
    public float DISTANCE_TO_WAYPOINT_THRESHOLD = 0.3f; // Distance requirement to trigger waypoint

    public enum MovementController { Keyboard, AutoPilot, Tracker };

    Vector2 initialPosition;
    Vector2 initialForward;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Initialize()
    {
        this.initialPosition = Vector2.zero;
        this.initialForward = Vector2.up;

        // Initialize according to the motion type
        if (this.MOVEMENT_CONTROLLER == MotionManager.MovementController.AutoPilot)
            this.DISTANCE_TO_WAYPOINT_THRESHOLD = 0.05f;// 0.0001f;

        if (this.MOVEMENT_CONTROLLER == MotionManager.MovementController.Tracker)
            return;
        else
        {
            InstantiateSimulationPrefab();
            simulationManager.redirectionManager.headTransform = simulationManager.simulatedHead;
            Debug.Log("First waypoint initialized");
        }

        switch (condPath)
        {
            case VirtualPathGenerator.PathSeedChoice.Office:
                currentPathSeed = VirtualPathGenerator.getPathSeedOfficeBuilding();
                break;
            case VirtualPathGenerator.PathSeedChoice.ExplorationSmall:
                currentPathSeed = VirtualPathGenerator.getPathSeedExplorationSmall();
                break;
            case VirtualPathGenerator.PathSeedChoice.ExplorationLarge:
                currentPathSeed = VirtualPathGenerator.getPathSeedExplorationLarge();
                break;
            case VirtualPathGenerator.PathSeedChoice.LongWalk:
                currentPathSeed = VirtualPathGenerator.getPathSeedLongCorridor();
                break;
            case VirtualPathGenerator.PathSeedChoice.ZigZag:
                currentPathSeed = VirtualPathGenerator.getPathSeedZigzag();
                break;
        }

        float sumOfDistances, sumOfRotations;
        this.waypoints = VirtualPathGenerator.generatePath(currentPathSeed, initialPosition, initialForward, out sumOfDistances, out sumOfRotations);
        Debug.Log("MOTION sumOfDistances: " + sumOfDistances);
        Debug.Log("MOTION sumOfRotations: " + sumOfRotations);

        // Set First Waypoint Position and Enable It
        this.targetWaypoint.position = new Vector3(this.waypoints[0].x, this.targetWaypoint.position.y, this.waypoints[0].y);
        this.waypointIterator = 0;
    }

    public void UpdateWaypoint(RedirectionManager.State userCurrState)
    {
        if ((userCurrState.pos - Utilities.FlattenedPos3D(this.targetWaypoint.position)).magnitude < this.DISTANCE_TO_WAYPOINT_THRESHOLD)
        {
            if (this.waypointIterator == this.waypoints.Count - 1)
            {
                // This is the last target
                if (this.simulationManager.simuMode == SimulationManager.SimuMode.Test)
                {
                    this.simulationManager.simuEnded = true;
                    Debug.Log("User arrived at destination.");
                }
                else
                {
                    this.simulationManager.episodeEnded = true;
                    // Stop when meets the max episode
                }


            }
            else
            {
                this.waypointIterator++;
                this.targetWaypoint.position = new Vector3(
                    this.waypoints[this.waypointIterator].x,
                    this.targetWaypoint.position.y,
                    this.waypoints[this.waypointIterator].y);
            }
        }
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
}
