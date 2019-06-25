using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Redirection;

public class ExperimentManager : MonoBehaviour {

    enum ExperimentChoice { FixedTrackedSpace, VaryingSizes, VaryingShapes };
    enum PathSeedChoice { Office, ExplorationSmall, ExplorationLarge, LongWalk, ZigZag };

    [SerializeField]
    ExperimentChoice condExperiment;

    List<ExperimentSetup> experimentSetups;
    int experimentIterator = 0;
    private bool experimentComplete = false;
    [HideInInspector]
    public bool experimentInProgress = false;

    [SerializeField]
    PathSeedChoice condPath;

    [SerializeField]
    float MAX_TRIALS = 10f;

    List<VirtualPathGenerator.PathSeed> pathSeeds = new List<VirtualPathGenerator.PathSeed>();
    List<TrackingSizeShape> trackingSizes = new List<TrackingSizeShape>();
    List<InitialConfiguration> initialConfigurations = new List<InitialConfiguration>();
    List<Vector3> gainScaleFactors = new List<Vector3>();

    float trialsForCurrentExperiment = 5;
    private float framesInExperiment = 0;


    public struct InitialConfiguration
    {
        public Vector2 initialPosition;
        public Vector2 initialForward;
        public bool isRandom;
        public InitialConfiguration(Vector2 initialPosition, Vector2 initialForward)
        {
            this.initialPosition = initialPosition;
            this.initialForward = initialForward;
            isRandom = false;
        }
        public InitialConfiguration(bool isRandom) // For Creating Random Configuration or just default of center/up
        {
            this.initialPosition = Vector2.zero;
            this.initialForward = Vector2.up;
            this.isRandom = isRandom;
        }
    }

    // ============== Structures =================
    struct TrackingSizeShape
    {
        public float x, z;
        public TrackingSizeShape(float x, float z)
        {
            this.x = x;
            this.z = z;
        }
    }

    struct ExperimentSetup
    {
        public System.Type redirector;
        public System.Type resetter;
        public VirtualPathGenerator.PathSeed pathSeed;
        public TrackingSizeShape trackingSizeShape;
        public InitialConfiguration initialConfiguration;
        public Vector3 gainScaleFactor;
        public ExperimentSetup(System.Type redirector, System.Type resetter, VirtualPathGenerator.PathSeed pathSeed, TrackingSizeShape trackingSizeShape, InitialConfiguration initialConfiguration, Vector3 gainScaleFactor)
        {
            this.redirector = redirector;
            this.resetter = resetter;
            this.pathSeed = pathSeed;
            this.trackingSizeShape = trackingSizeShape;
            this.initialConfiguration = initialConfiguration;
            this.gainScaleFactor = gainScaleFactor;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (motionManager.MOVEMENT_CONTROLLER == RedirectionManager.MovementController.Tracker)
            return;
        //framesGoneBy++;
        //if (firstUpdateRealTime == 0)
        //    firstUpdateRealTime = Time.realtimeSinceStartup;
        //if (Time.realtimeSinceStartup - firstUpdateRealTime > 1)
        //{
        //    Debug.Log("Frames Per Second: " + (framesGoneBy / 1.0f));
        //    firstUpdateRealTime = 0;
        //    framesGoneBy = 0;
        //}

        updateSimulatedWaypointIfRequired();

        // First Take Care of Snapshot, so the time it take to generate it doesn't effect newly beginning experiment
        if (takeScreenshot)
        {
            //Debug.Log("Frames In Experiment: " + framesInExperiment);
            framesInExperiment = 0;
            float start = Time.realtimeSinceStartup;
            redirectionManager.snapshotGenerator.TakeScreenshot(experimentDescriptorToString(getExperimentDescriptor(experimentSetups[experimentIterator - 1]))); // Snapshot pertains to the previous experiment
            Debug.Log("Time Spent For Snapshot Generation: " + (Time.realtimeSinceStartup - start));
            takeScreenshot = false;
            if (experimentIterator == experimentSetups.Count)
                Debug.Log("---------- EXPERIMENTS COMPLETE ----------");
        }
        //if (!experimentInProgress && ((Time.time - lastExperimentEndTime) / timeScale > EXPERIMENT_WAIT_TIME) && experimentIterator < experimentSetups.Count)
        if (!experimentInProgress && experimentIterator < experimentSetups.Count)
        {
            startNextExperiment();
            //experimentStartTime = Time.time;
        }
        //if (experimentInProgress && !userStartedWalking && ((Time.time - experimentStartTime) / timeScale > WALKING_WAIT_TIME))
        if (experimentInProgress && !userIsWalking)
        {
            userIsWalking = true;
            //// Allow Walking
            //UserController.allowWalking = true;
            // Start Logging
            redirectionManager.statisticsLogger.BeginLogging();
        }

        if (experimentInProgress && userIsWalking)
        {
            //Debug.Log("User At: " + redirectionManager.userHeadTransform.position.ToString("f4"));
            framesInExperiment++;
        }
    }

    public void Initialize()
    {
        redirectionManager.runInTestMode = runInSimulationMode;
        userIsWalking = !(redirectionManager.MOVEMENT_CONTROLLER == RedirectionManager.MovementController.AutoPilot);
        if (redirectionManager.MOVEMENT_CONTROLLER == RedirectionManager.MovementController.AutoPilot)
            DISTANCE_TO_WAYPOINT_THRESHOLD = 0.05f;// 0.0001f;

        if (redirectionManager.MOVEMENT_CONTROLLER != RedirectionManager.MovementController.Tracker)
        {
            InstantiateSimulationPrefab();
        }

        if (redirectionManager.MOVEMENT_CONTROLLER == RedirectionManager.MovementController.Tracker)
            return;


        // Setting Random Seed
        Random.InitState(VirtualPathGenerator.RANDOM_SEED);

        // Make sure VSync doesn't slow us down

        //Debug.Log("Application.targetFrameRate: " + Application.targetFrameRate);

        if (runAtFullSpeed && this.enabled)
        {
            //redirectionManager.topViewCamera.enabled = false;
            //drawVirtualPath = false;
            QualitySettings.vSyncCount = 0;
        }

        // Initialization
        experimentIterator = 0;
        //if (this.enabled)
        //    redirectionManager.userMovementManager.activateSimulatedWalker();

        if (redirectionManager.runInTestMode)
        {
            System.Type redirectorType = null;
            System.Type resetterType = null;
            switch (condAlgorithm)
            {
                case AlgorithmChoice.None:
                    redirectorType = typeof(NullRedirector);
                    break;
                case AlgorithmChoice.S2C:
                    redirectorType = typeof(S2CRedirector);
                    break;
                case AlgorithmChoice.S2O:
                    redirectorType = typeof(S2ORedirector);
                    break;
                case AlgorithmChoice.Zigzag:
                    redirectorType = typeof(ZigZagRedirector);
                    break;
                case AlgorithmChoice.MPC:
                    redirectorType = typeof(MPCRedirector);
                    break;
                case AlgorithmChoice.Q:
                    redirectorType = typeof(QLearningRedirector);
                    break;

            }
            switch (condReset)
            {
                case ResetChoice.None:
                    resetterType = typeof(NullResetter);
                    break;
                case ResetChoice.TwoOneTurn:
                    resetterType = typeof(TwoOneTurnResetter);
                    break;
            }

            switch (condExperiment)
            {
                case ExperimentChoice.FixedTrackedSpace:
                    setUpExperimentFixedTrackingArea(condPath, redirectorType, resetterType);
                    break;
                case ExperimentChoice.VaryingSizes:
                    setUpExperimentTrackingAreaSizePerformance(condPath, redirectorType, resetterType);
                    break;
                case ExperimentChoice.VaryingShapes:
                    setUpExperimentTrackingAreaShape(condPath, redirectorType, resetterType);
                    break;
            }

        }

        GenerateAllExperimentSetups();

        // Determine Initial Configurations If Random
        determineInitialConfigurations(ref experimentSetups);
    }

    void setUpExperimentFixedTrackingArea(PathSeedChoice pathSeedChoice, System.Type redirector, System.Type resetter)
    {
        // Initialize Values
        this.redirector = redirector;
        this.resetter = resetter;
        pathSeeds = new List<VirtualPathGenerator.PathSeed>();
        trackingSizes = new List<TrackingSizeShape>();
        initialConfigurations = new List<InitialConfiguration>();
        gainScaleFactors = new List<Vector3>();
        trialsForCurrentExperiment = pathSeedChoice == PathSeedChoice.LongWalk ? 1 : MAX_TRIALS;

        switch (pathSeedChoice)
        {
            case PathSeedChoice.Office:
                pathSeeds.Add(getPathSeedOfficeBuilding());
                break;
            case PathSeedChoice.ExplorationSmall:
                pathSeeds.Add(getPathSeedExplorationSmall());
                break;
            case PathSeedChoice.ExplorationLarge:
                pathSeeds.Add(getPathSeedExplorationLarge());
                break;
            case PathSeedChoice.LongWalk:
                pathSeeds.Add(getPathSeedLongCorridor());
                break;
            case PathSeedChoice.ZigZag:
                pathSeeds.Add(getPathSeedZigzag());
                break;
        }

        trackingSizes.Add(new TrackingSizeShape(redirectionManager.trackedSpace.localScale.x, redirectionManager.trackedSpace.localScale.z));

        initialConfigurations.Add(new InitialConfiguration(new Vector2(0, 0), new Vector2(0, 1)));
        gainScaleFactors.Add(Vector3.one);
    }

    void setUpExperimentTrackingAreaSizePerformance(PathSeedChoice pathSeedChoice, System.Type redirector, System.Type resetter)
    {
        // Initialize Values
        this.redirector = redirector;
        this.resetter = resetter;
        pathSeeds = new List<VirtualPathGenerator.PathSeed>();
        trackingSizes = new List<TrackingSizeShape>();
        initialConfigurations = new List<InitialConfiguration>();
        gainScaleFactors = new List<Vector3>();
        trialsForCurrentExperiment = pathSeedChoice == PathSeedChoice.LongWalk ? 1 : MAX_TRIALS;

        switch (pathSeedChoice)
        {
            case PathSeedChoice.Office:
                pathSeeds.Add(getPathSeedOfficeBuilding());
                break;
            case PathSeedChoice.ExplorationSmall:
                pathSeeds.Add(getPathSeedExplorationSmall());
                break;
            case PathSeedChoice.ExplorationLarge:
                pathSeeds.Add(getPathSeedExplorationLarge());
                break;
            case PathSeedChoice.LongWalk:
                pathSeeds.Add(getPathSeedLongCorridor());
                break;
            case PathSeedChoice.ZigZag:
                pathSeeds.Add(getPathSeedZigzag());
                break;
        }

        for (int i = 2; i <= 60; i += 1)
        {
            trackingSizes.Add(new TrackingSizeShape(i, i));
        }

        initialConfigurations.Add(new InitialConfiguration(new Vector2(0, 0), new Vector2(0, 1)));
        gainScaleFactors.Add(Vector3.one);
    }

    void setUpExperimentTrackingAreaShape(PathSeedChoice pathSeedChoice, System.Type redirector, System.Type resetter)
    {
        // Initialize Values
        this.redirector = redirector;
        this.resetter = resetter;
        pathSeeds = new List<VirtualPathGenerator.PathSeed>();
        trackingSizes = new List<TrackingSizeShape>();
        initialConfigurations = new List<InitialConfiguration>();
        gainScaleFactors = new List<Vector3>();
        trialsForCurrentExperiment = pathSeedChoice == PathSeedChoice.LongWalk ? 1 : MAX_TRIALS;

        switch (pathSeedChoice)
        {
            case PathSeedChoice.Office:
                pathSeeds.Add(getPathSeedOfficeBuilding());
                break;
            case PathSeedChoice.ExplorationSmall:
                pathSeeds.Add(getPathSeedExplorationSmall());
                break;
            case PathSeedChoice.ExplorationLarge:
                pathSeeds.Add(getPathSeedExplorationLarge());
                break;
            case PathSeedChoice.LongWalk:
                pathSeeds.Add(getPathSeedLongCorridor());
                break;
            case PathSeedChoice.ZigZag:
                pathSeeds.Add(getPathSeedZigzag());
                break;
        }

        for (int area = 100; area <= 200; area += 50)
        {
            for (float ratio = 1; ratio <= 2; ratio += 0.5f)
            {
                trackingSizes.Add(new TrackingSizeShape(Mathf.Sqrt(area) / Mathf.Sqrt(ratio), Mathf.Sqrt(area) * Mathf.Sqrt(ratio)));
            }
        }

        initialConfigurations.Add(new InitialConfiguration(new Vector2(0, 0), new Vector2(0, 1)));
        initialConfigurations.Add(new InitialConfiguration(new Vector2(0, 0), new Vector2(1, 0)));
        initialConfigurations.Add(new InitialConfiguration(new Vector2(0, 0), Vector2.one)); // HACK: THIS NON-NORMALIZED ORIENTATION WILL INDICATE DIAGONAL AND WILL BE FIXED LATER
        gainScaleFactors.Add(Vector3.one);
    }



    private void GenerateAllExperimentSetups()
    {
        // Here we generate the correspondign experiments
        experimentSetups = new List<ExperimentSetup>();
        foreach (VirtualPathGenerator.PathSeed pathSeed in pathSeeds)
        {
            foreach (TrackingSizeShape trackingSize in trackingSizes)
            {
                foreach (InitialConfiguration initialConfiguration in initialConfigurations)
                {
                    foreach (Vector3 gainScaleFactor in gainScaleFactors)
                    {
                        for (int i = 0; i < trialsForCurrentExperiment; i++)
                        {
                            experimentSetups.Add(new ExperimentSetup(redirector, resetter, pathSeed, trackingSize, initialConfiguration, gainScaleFactor));
                        }
                    }
                }
            }
        }
    }

    void startNextExperiment()
    {
        Debug.Log("---------- EXPERIMENT STARTED ----------");

        ExperimentSetup setup = experimentSetups[experimentIterator];

        printExperimentDescriptor(getExperimentDescriptor(setup));

        // Setting Gain Scale Factors
        //RedirectionManager.SCALE_G_T = setup.gainScaleFactor.x;
        //RedirectionManager.SCALE_G_R = setup.gainScaleFactor.y;
        //RedirectionManager.SCALE_G_C = setup.gainScaleFactor.z;

        // Enabling/Disabling Redirectors
        redirectionManager.UpdateRedirector(setup.redirector);
        redirectionManager.UpdateResetter(setup.resetter);

        // Setup Trail Drawing
        redirectionManager.trailDrawer.enabled = !runAtFullSpeed;

        // Enable Waypoint
        redirectionManager.targetWaypoint.gameObject.SetActive(true);

        // Resetting User and World Positions and Orientations
        this.transform.position = Vector3.zero;
        this.transform.rotation = Quaternion.identity;
        // ESSENTIAL BUG FOUND: If you set the user first and then the redirection recipient, then the user will be moved, so you have to make sure to do it afterwards!
        //Debug.Log("Target User Position: " + setup.initialConfiguration.initialPosition.ToString("f4"));
        redirectionManager.headTransform.position = Utilities.UnFlatten(setup.initialConfiguration.initialPosition, redirectionManager.headTransform.position.y);
        //Debug.Log("Result User Position: " + redirectionManager.userHeadTransform.transform.position.ToString("f4"));
        redirectionManager.headTransform.rotation = Quaternion.LookRotation(Utilities.UnFlatten(setup.initialConfiguration.initialForward), Vector3.up);

        // Set up Tracking Area Dimensions
        redirectionManager.UpdateTrackedSpaceDimensions(setup.trackingSizeShape.x, setup.trackingSizeShape.z);

        // Set up Virtual Path
        float sumOfDistances, sumOfRotations;
        waypoints = VirtualPathGenerator.generatePath(setup.pathSeed, setup.initialConfiguration.initialPosition, setup.initialConfiguration.initialForward, out sumOfDistances, out sumOfRotations);
        Debug.Log("sumOfDistances: " + sumOfDistances);
        Debug.Log("sumOfRotations: " + sumOfRotations);
        if (setup.redirector == typeof(ZigZagRedirector))
        {
            // Create Fake POIs
            Transform poiRoot = (new GameObject()).transform;
            poiRoot.name = "ZigZag Redirector Waypoints";
            poiRoot.localPosition = Vector3.zero;
            poiRoot.localRotation = Quaternion.identity;
            Transform poi0 = (new GameObject()).transform;
            poi0.localPosition = Vector3.zero;
            poi0.parent = poiRoot;
            List<Transform> zigzagRedirectorWaypoints = new List<Transform>();
            zigzagRedirectorWaypoints.Add(poi0);
            foreach (Vector2 waypoint in waypoints)
            {
                Transform poi = (new GameObject()).transform;
                poi.localPosition = Utilities.UnFlatten(waypoint);
                poi.parent = poiRoot;
                zigzagRedirectorWaypoints.Add(poi);
            }
            ((ZigZagRedirector)redirectionManager.redirector).waypoints = zigzagRedirectorWaypoints;
        }


        // Set First Waypoint Position and Enable It
        redirectionManager.targetWaypoint.position = new Vector3(waypoints[0].x, redirectionManager.targetWaypoint.position.y, waypoints[0].y);
        waypointIterator = 0;

        // POSTPONING THESE FOR SAFETY REASONS!
        //// Allow Walking
        //UserController.allowWalking = true;

        //// Start Logging
        //redirectionManager.redirectionStatistics.beginLogging();
        //redirectionManager.statisticsLogger.beginLogging();

        //lastExperimentRealStartTime = Time.realtimeSinceStartup;
        experimentInProgress = true;
    }

    void endExperiment()
    {
        //Debug.LogWarning("Last Experiment Length: " + (Time.realtimeSinceStartup - lastExperimentRealStartTime));

        ExperimentSetup setup = experimentSetups[experimentIterator];

        // Stop Trail Drawing
        redirectionManager.trailDrawer.enabled = false;

        // Delete Virtual Path
        // THIS CAN BE MADE OPTIONAL IF NECESSARY
        redirectionManager.trailDrawer.ClearTrail(TrailDrawer.VIRTUAL_TRAIL_NAME);

        // Disable Waypoint
        redirectionManager.targetWaypoint.gameObject.SetActive(true);

        // Disallow Walking
        userIsWalking = false;

        // Stop Logging
        redirectionManager.statisticsLogger.EndLogging();

        // Gather Summary Statistics
        redirectionManager.statisticsLogger.experimentResults.Add(redirectionManager.statisticsLogger.GetExperimentResultForSummaryStatistics(getExperimentDescriptor(setup)));

        // Log Sampled Metrics
        if (redirectionManager.statisticsLogger.logSampleVariables)
        {
            Dictionary<string, List<float>> oneDimensionalSamples;
            Dictionary<string, List<Vector2>> twoDimensionalSamples;
            redirectionManager.statisticsLogger.GetExperimentResultsForSampledVariables(out oneDimensionalSamples, out twoDimensionalSamples);
            redirectionManager.statisticsLogger.LogAllExperimentSamples(experimentDescriptorToString(getExperimentDescriptor(setup)), oneDimensionalSamples, twoDimensionalSamples);
        }

        // Take Snapshot In Next Frame (After User and Virtual Path Is Disabled)
        if (!runAtFullSpeed)
            takeScreenshot = true;

        // Prepared for new experiment
        experimentIterator++;
        //lastExperimentEndTime = Time.time;
        experimentInProgress = false;

        // Log All Summary Statistics To File
        if (experimentIterator == experimentSetups.Count)
        {
            if (averageTrialResults)
                redirectionManager.statisticsLogger.experimentResults = mergeTrialSummaryStatistics(redirectionManager.statisticsLogger.experimentResults);
            redirectionManager.statisticsLogger.LogExperimentSummaryStatisticsResultsSCSV(redirectionManager.statisticsLogger.experimentResults);
            Debug.Log("Last Experiment Complete");
            experimentComplete = true;
            if (redirectionManager.runInTestMode)
                Application.Quit();
        }

        // Disabling Redirectors
        redirectionManager.RemoveRedirector();
        redirectionManager.RemoveResetter();
    }
}
