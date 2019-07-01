using UnityEngine;
using System.Collections.Generic;
using Redirection;
using System;

public class SimulationManager : MonoBehaviour {

    [HideInInspector]
    public RedirectionManager redirectionManager;
    [HideInInspector]
    public MotionManager motionManager;
    [HideInInspector]
    public EnvManager envManager;

    [SerializeField]
    bool runInSimulationMode = false;
    [SerializeField]
    public bool averageTrialResults = false;

    [SerializeField]
    public bool runAtFullSpeed = false;

    [HideInInspector]
    public bool takeScreenshot = false;

    [Tooltip("Use simulated framerate in auto-pilot mode")]
    public bool useManualTime = false;

    [Tooltip("Target simulated framerate in auto-pilot mode")]
    public float targetFPS = 60;

    [HideInInspector]
    public bool userIsWalking = false;

    [HideInInspector]
    public SnapshotGenerator snapshotGenerator;
    [HideInInspector]
    public StatisticsLogger statisticsLogger;

    [HideInInspector]
    public TrailDrawer trailDrawer;
    [HideInInspector]
    public Transform simulatedHead;
    [HideInInspector]
    public HeadFollower bodyHeadFollower;

    
    [HideInInspector]
    public string startTimeOfProgram;

    private float simulatedTime = 0;


    private GridEnvironment rasterization;

    private void Awake()
    {
        // Here we do initialize all managers add pass their reference to their sub-component

        startTimeOfProgram = System.DateTime.Now.ToString("yyyy MM dd HH:mm:ss");

        GetRedirectionManager();
        GetMotionManager();
        GetEnvManager();

        GetSimulatedHead();
        GetBodyHeadFollower();
        GetTrailDrawer();
        GetSnapshotGenerator();
        GetStatisticsLogger();

        SetReferenceForRedirectionManager();
        SetReferenceForMotionManager();
        SetReferenceForEnvManager();
        SetReferenceForTrailDrawer();
        SetReferenceForStatisticsLogger();
        SetReferenceForBodyHeadFollower();

        // Redirection Manager
        this.redirectionManager.GetRedirector();
        this.redirectionManager.GetResetter();
        this.redirectionManager.GetResetTrigger();
        this.redirectionManager.GetBody();
        this.redirectionManager.SetReferenceForRedirector();
        this.redirectionManager.SetReferenceForResetter();
        this.redirectionManager.SetReferenceForResetTrigger();
        this.redirectionManager.SetBodyReferenceForResetTrigger();

        // Motion Manager
        this.motionManager.GetSimulatedWalker();
        this.motionManager.GetKeyboardController();
        this.motionManager.SetReferenceForSimulatedWalker();
        this.motionManager.SetReferenceForKeyboardController();

        // Env Manager
        this.envManager.GetTrackedSpace();

        // Q-Learning
        GetRasterization();
        rasterization.simulationManager = this;
        rasterization.redirectionManager = this.redirectionManager;
    }

    private void GetMotionManager()
    {
        this.motionManager = this.gameObject.GetComponent<MotionManager>();
    }

    private void GetEnvManager()
    {
        this.envManager = this.gameObject.GetComponent<EnvManager>();
    }

    // Use this for initialization
    void Start () {
        simulatedTime = 0;
        this.redirectionManager.Initialize();

        // Motion
        this.redirectionManager.runInTestMode = runInSimulationMode;
        userIsWalking = !(motionManager.MOVEMENT_CONTROLLER == MotionManager.MovementController.AutoPilot);
        if (motionManager.MOVEMENT_CONTROLLER == MotionManager.MovementController.AutoPilot)
            motionManager.DISTANCE_TO_WAYPOINT_THRESHOLD = 0.05f;// 0.0001f;

        if (motionManager.MOVEMENT_CONTROLLER == MotionManager.MovementController.Tracker)
            return;
        else
        {
            motionManager.InstantiateSimulationPrefab();
            redirectionManager.headTransform = simulatedHead;
        }

        // Setting Random Seed
        UnityEngine.Random.InitState(VirtualPathGenerator.RANDOM_SEED);

        // Make sure VSync doesn't slow us down

        //Debug.Log("Application.targetFrameRate: " + Application.targetFrameRate);

        if (runAtFullSpeed && this.enabled)
        {
            //redirectionManager.topViewCamera.enabled = false;
            //drawVirtualPath = false;
            QualitySettings.vSyncCount = 0;
        }

    }

    // Update is called once per frame
    void Update()
    {
        if ((this.redirectionManager.currState.pos - Utilities.FlattenedPos3D(this.motionManager.targetWaypoint.position)).magnitude < this.motionManager.DISTANCE_TO_WAYPOINT_THRESHOLD)
        {
            if (this.motionManager.waypointIterator == this.motionManager.waypoints.Count - 1)
            {
                Application.Quit();
            }
            else
            {
                this.motionManager.waypointIterator++;
                this.motionManager.targetWaypoint.position = new Vector3(this.motionManager.waypoints[
                    this.motionManager.waypointIterator].x, 
                    this.motionManager.targetWaypoint.position.y, 
                    this.motionManager.waypoints[this.motionManager.waypointIterator].y);
            }
        }

    }

    // LateUpdate is called after all Update functions have been called
    // LateUpdate is called every frame, if the Behaviour is enabled.
    void LateUpdate()
    {
        simulatedTime += 1.0f / targetFPS;

        //if (MOVEMENT_CONTROLLER == MovementController.AutoPilot)
        //    simulatedWalker.WalkUpdate();

        // Redirection Part
        this.redirectionManager.Run();
    }

    void SetReferenceForRedirectionManager()
    {
        if (redirectionManager != null)
        {
            redirectionManager.simulationManager = this;
        }
    }

    void SetReferenceForMotionManager()
    {
        if (motionManager != null)
        {
            motionManager.simulationManager = this;
        }
    }

    void SetReferenceForEnvManager()
    {
        if (envManager != null)
        {
            envManager.simulationManager = this;
        }
    }

    void SetReferenceForTrailDrawer()
    {
        if (trailDrawer != null)
        {
            trailDrawer.simulationManager = this;
        }
    }

    void SetReferenceForStatisticsLogger()
    {
        if (statisticsLogger != null)
        {
            statisticsLogger.simulationManager = this;
        }
    }

    void SetReferenceForBodyHeadFollower()
    {
        if (bodyHeadFollower != null)
        {
            bodyHeadFollower.simulationManager = this;
        }
    }

    void GetRedirectionManager()
    {
        redirectionManager = this.gameObject.GetComponent<RedirectionManager>();
    }

    void GetTrailDrawer()
    {
        trailDrawer = this.gameObject.GetComponent<TrailDrawer>();
    }


    void GetSnapshotGenerator()
    {
        snapshotGenerator = this.gameObject.GetComponent<SnapshotGenerator>();
    }

    void GetStatisticsLogger()
    {
        statisticsLogger = this.gameObject.GetComponent<StatisticsLogger>();
    }



    void GetBodyHeadFollower()
    {
        bodyHeadFollower = redirectionManager.body.GetComponent<HeadFollower>();
    }

    void GetSimulatedHead()
    {
        simulatedHead = transform.Find("Simulated User").Find("Head");
    }




    public float GetDeltaTime()
    {
        if (useManualTime)
            return 1.0f / targetFPS;
        else
            return Time.deltaTime;
    }

    public float GetTime()
    {
        if (useManualTime)
            return simulatedTime;
        else
            return Time.time;
    }

    

    public void ResetEpisode()
    {
        this.trailDrawer.OnDisable();

        // Resetting User and World Positions and Orientations
        this.transform.position = Vector3.zero;
        this.transform.rotation = Quaternion.identity;

        this.redirectionManager.headTransform.position = Utilities.UnFlatten(Vector2.zero, this.redirectionManager.headTransform.position.y);
        this.redirectionManager.headTransform.rotation = Quaternion.LookRotation(Utilities.UnFlatten(Vector2.zero), Vector3.up);

        this.trailDrawer.OnEnable();
    }

    public void GetRasterization()
    {
        rasterization = GameObject.Find("GridEnv").GetComponent<GridEnvironment>();
    }
}
