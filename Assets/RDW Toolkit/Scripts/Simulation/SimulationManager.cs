using UnityEngine;
using System.Collections.Generic;
using Redirection;
using System;

public class SimulationManager : MonoBehaviour {

    public enum SimuMode { Test, Learn, Experiment };

    [SerializeField]
    public SimuMode simuMode;

    [HideInInspector]
    public RedirectionManager redirectionManager;
    [HideInInspector]
    public MotionManager motionManager;
    [HideInInspector]
    public EnvManager envManager;

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
    [HideInInspector]
    public bool simuEnded;

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
        this.redirectionManager.headTransform = this.simulatedHead;

        // Motion Manager
        this.motionManager.GetSimulatedWalker();
        this.motionManager.GetKeyboardController();
        this.motionManager.SetReferenceForSimulatedWalker();
        this.motionManager.SetReferenceForKeyboardController();

        // Env Manager
        this.envManager.GetTrackedSpace();
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

        // Setting Random Seed
        UnityEngine.Random.InitState(VirtualPathGenerator.RANDOM_SEED);

        // Make sure VSync doesn't slow us down

        //Debug.Log("Application.targetFrameRate: " + Application.targetFrameRate);

        if (this.runAtFullSpeed && this.enabled)
        {
            //redirectionManager.topViewCamera.enabled = false;
            //drawVirtualPath = false;
            QualitySettings.vSyncCount = 0;
        }

        // Start Simulation
        // Setup Trail Drawing
        this.trailDrawer.enabled = !this.runAtFullSpeed;

        // Enable Waypoint
        userIsWalking = !(motionManager.movementController == MotionManager.MovementController.AutoPilot);
        this.motionManager.Initialize();

        switch (this.simuMode)
        {
            case SimuMode.Test:
                GameObject.FindWithTag("LearningUI").SetActive(false);
                break;
            case SimuMode.Learn:
                GameObject.FindWithTag("LearningUI").SetActive(true);
                GridEnvironment grid_env = GameObject.Find("GridEnv").GetComponent(typeof(GridEnvironment)) as GridEnvironment;
                grid_env.Initialize();
                break;
            case SimuMode.Experiment:
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        

    }

    // LateUpdate is called after all Update functions have been called
    // LateUpdate is called every frame, if the Behaviour is enabled.
    void LateUpdate()
    {
        if (!this.simuEnded)
        {
            simulatedTime += 1.0f / targetFPS;

            // Motion part
            if (this.motionManager.movementController == MotionManager.MovementController.AutoPilot)
            {
                this.motionManager.UpdateWaypoint(this.redirectionManager.currState);
            }

            // Redirection Part
            this.redirectionManager.Run();

            if (!userIsWalking)
            {
                userIsWalking = true;
                //// Allow Walking
                //UserController.allowWalking = true;
                // Start Logging
                statisticsLogger.BeginLogging();
            }
        }
        
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

    
    // For learning
    public void ResetEpisode()
    {
        this.trailDrawer.OnDisable();

        // Resetting User and World Positions and Orientations
        this.transform.position = Vector3.zero;
        this.transform.rotation = Quaternion.identity;

        this.redirectionManager.headTransform.position = Utilities.UnFlatten(Vector2.zero, this.redirectionManager.headTransform.position.y);
        this.redirectionManager.headTransform.rotation = Quaternion.LookRotation(Utilities.UnFlatten(Vector2.zero), Vector3.up);

        // Reset waypoints for auto pilot
        if (this.motionManager.movementController == MotionManager.MovementController.AutoPilot)
        {
            this.motionManager.targetWaypoint.position = new Vector3(motionManager.waypoints[0].x, motionManager.targetWaypoint.position.y, motionManager.waypoints[0].y);
            this.motionManager.waypointIterator = 0;
        }

        this.trailDrawer.OnEnable();

    }

    public void EndRound()
    {
        this.userIsWalking = false;
        if (this.simuMode == SimuMode.Test)
        {
            this.simuEnded = true;
            Debug.Log("User arrived at destination.");
        }
        else if (this.simuMode == SimulationManager.SimuMode.Learn)
        {
            this.ResetEpisode();
            this.statisticsLogger.EndLogging();
            // Stop when meets the max episode
            GridEnvironment grid_env = GameObject.Find("GridEnv").GetComponent(typeof(GridEnvironment)) as GridEnvironment;
            grid_env.done = true;

            if (grid_env.episodeCount == grid_env.episodeMax)
            {
                this.simuEnded = true;
                if (grid_env.saveQTable)
                {
                    ((QLearningAgent)grid_env.agent).SaveQTable("Assets/Resources/qtable.txt");
                }

                // Log All Summary Statistics To File
                this.statisticsLogger.LogExperimentSummaryStatisticsResultsSCSV(this.statisticsLogger.experimentResults);
                Debug.Log("Statistics complete");

            }
        }
    }

}
