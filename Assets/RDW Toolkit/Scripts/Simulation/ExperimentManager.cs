using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Redirection;

public class ExperimentManager : MonoBehaviour {

    [HideInInspector]
    public SimulationManager simulationManager;

    enum ExperimentChoice { FixedTrackedSpace, VaryingSizes, VaryingShapes };


    [SerializeField]
    ExperimentChoice condExperiment;

    List<ExperimentSetup> experimentSetups;
    int experimentIterator = 0;
    private bool experimentComplete = false;
    [HideInInspector]
    public bool experimentInProgress = false;

    [SerializeField]
    public bool onlyRandomizeForward = true;
    [SerializeField]
    VirtualPathGenerator.PathSeedChoice condPath;

    [SerializeField]
    float MAX_TRIALS = 10f;

    List<VirtualPathGenerator.PathSeed> pathSeeds = new List<VirtualPathGenerator.PathSeed>();
    List<TrackingSizeShape> trackingSizes = new List<TrackingSizeShape>();
    List<InitialConfiguration> initialConfigurations = new List<InitialConfiguration>();
    List<Vector3> gainScaleFactors = new List<Vector3>();

    float trialsForCurrentExperiment = 5;
    private float framesInExperiment = 0;

    // Experiment Variables
    System.Type redirector = null;
    System.Type resetter = null;

    // ============== Structures =================
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

    public void Initialize()
    {
        // Initialization
        experimentIterator = 0;
        System.Type redirectorType = simulationManager.redirectionManager.redirectorType;
        System.Type resetterType = simulationManager.redirectionManager.resetterType;
        
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

        GenerateAllExperimentSetups();

        // Determine Initial Configurations If Random
        determineInitialConfigurations(ref experimentSetups);
    }

    // Update is called once per frame
    void Update()
    {
        if (simulationManager.motionManager.movementController == MotionManager.MovementController.Tracker)
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
        if (simulationManager.takeScreenshot)
        {
            //Debug.Log("Frames In Experiment: " + framesInExperiment);
            framesInExperiment = 0;
            float start = Time.realtimeSinceStartup;
            simulationManager.snapshotGenerator.TakeScreenshot(experimentDescriptorToString(getExperimentDescriptor(experimentSetups[experimentIterator - 1]))); // Snapshot pertains to the previous experiment
            Debug.Log("Time Spent For Snapshot Generation: " + (Time.realtimeSinceStartup - start));
            simulationManager.takeScreenshot = false;
            if (experimentIterator == experimentSetups.Count)
                Debug.Log("---------- EXPERIMENTS COMPLETE ----------");
        }
        
        if (!experimentInProgress && experimentIterator < experimentSetups.Count)
        {
            startNextExperiment();
            //experimentStartTime = Time.time;
        }
        //if (experimentInProgress && !userStartedWalking && ((Time.time - experimentStartTime) / timeScale > WALKING_WAIT_TIME))
        if (experimentInProgress && !simulationManager.userIsWalking)
        {
            simulationManager.userIsWalking = true;
            //// Allow Walking
            //UserController.allowWalking = true;
            // Start Logging
            simulationManager.statisticsLogger.BeginLogging();
        }

        if (experimentInProgress && simulationManager.userIsWalking)
        {
            //Debug.Log("User At: " + redirectionManager.userHeadTransform.position.ToString("f4"));
            framesInExperiment++;
        }
    }

    

    void setUpExperimentFixedTrackingArea(VirtualPathGenerator.PathSeedChoice pathSeedChoice, System.Type redirector, System.Type resetter)
    {
        // Initialize Values
        this.redirector = redirector;
        this.resetter = resetter;
        pathSeeds = new List<VirtualPathGenerator.PathSeed>();
        trackingSizes = new List<TrackingSizeShape>();
        initialConfigurations = new List<InitialConfiguration>();
        gainScaleFactors = new List<Vector3>();
        trialsForCurrentExperiment = pathSeedChoice == VirtualPathGenerator.PathSeedChoice.LongWalk ? 1 : MAX_TRIALS;

        switch (pathSeedChoice)
        {
            case VirtualPathGenerator.PathSeedChoice.Office:
                pathSeeds.Add(VirtualPathGenerator.getPathSeedOfficeBuilding());
                break;
            case VirtualPathGenerator.PathSeedChoice.ExplorationSmall:
                pathSeeds.Add(VirtualPathGenerator.getPathSeedExplorationSmall());
                break;
            case VirtualPathGenerator.PathSeedChoice.ExplorationLarge:
                pathSeeds.Add(VirtualPathGenerator.getPathSeedExplorationLarge());
                break;
            case VirtualPathGenerator.PathSeedChoice.LongWalk:
                pathSeeds.Add(VirtualPathGenerator.getPathSeedLongCorridor());
                break;
            case VirtualPathGenerator.PathSeedChoice.ZigZag:
                pathSeeds.Add(VirtualPathGenerator.getPathSeedZigzag());
                break;
        }

        trackingSizes.Add(new TrackingSizeShape(simulationManager.envManager.trackedSpace.localScale.x, simulationManager.envManager.trackedSpace.localScale.z));

        initialConfigurations.Add(new InitialConfiguration(new Vector2(0, 0), new Vector2(0, 1)));
        gainScaleFactors.Add(Vector3.one);
    }

    void setUpExperimentTrackingAreaSizePerformance(VirtualPathGenerator.PathSeedChoice pathSeedChoice, System.Type redirector, System.Type resetter)
    {
        // Initialize Values
        this.redirector = redirector;
        this.resetter = resetter;
        pathSeeds = new List<VirtualPathGenerator.PathSeed>();
        trackingSizes = new List<TrackingSizeShape>();
        initialConfigurations = new List<InitialConfiguration>();
        gainScaleFactors = new List<Vector3>();
        trialsForCurrentExperiment = pathSeedChoice == VirtualPathGenerator.PathSeedChoice.LongWalk ? 1 : MAX_TRIALS;

        switch (pathSeedChoice)
        {
            case VirtualPathGenerator.PathSeedChoice.Office:
                pathSeeds.Add(VirtualPathGenerator.getPathSeedOfficeBuilding());
                break;
            case VirtualPathGenerator.PathSeedChoice.ExplorationSmall:
                pathSeeds.Add(VirtualPathGenerator.getPathSeedExplorationSmall());
                break;
            case VirtualPathGenerator.PathSeedChoice.ExplorationLarge:
                pathSeeds.Add(VirtualPathGenerator.getPathSeedExplorationLarge());
                break;
            case VirtualPathGenerator.PathSeedChoice.LongWalk:
                pathSeeds.Add(VirtualPathGenerator.getPathSeedLongCorridor());
                break;
            case VirtualPathGenerator.PathSeedChoice.ZigZag:
                pathSeeds.Add(VirtualPathGenerator.getPathSeedZigzag());
                break;
        }

        for (int i = 2; i <= 60; i += 1)
        {
            trackingSizes.Add(new TrackingSizeShape(i, i));
        }

        initialConfigurations.Add(new InitialConfiguration(new Vector2(0, 0), new Vector2(0, 1)));
        gainScaleFactors.Add(Vector3.one);
    }

    void setUpExperimentTrackingAreaShape(VirtualPathGenerator.PathSeedChoice pathSeedChoice, System.Type redirector, System.Type resetter)
    {
        // Initialize Values
        this.redirector = redirector;
        this.resetter = resetter;
        pathSeeds = new List<VirtualPathGenerator.PathSeed>();
        trackingSizes = new List<TrackingSizeShape>();
        initialConfigurations = new List<InitialConfiguration>();
        gainScaleFactors = new List<Vector3>();
        trialsForCurrentExperiment = pathSeedChoice == VirtualPathGenerator.PathSeedChoice.LongWalk ? 1 : MAX_TRIALS;

        switch (pathSeedChoice)
        {
            case VirtualPathGenerator.PathSeedChoice.Office:
                pathSeeds.Add(VirtualPathGenerator.getPathSeedOfficeBuilding());
                break;
            case VirtualPathGenerator.PathSeedChoice.ExplorationSmall:
                pathSeeds.Add(VirtualPathGenerator.getPathSeedExplorationSmall());
                break;
            case VirtualPathGenerator.PathSeedChoice.ExplorationLarge:
                pathSeeds.Add(VirtualPathGenerator.getPathSeedExplorationLarge());
                break;
            case VirtualPathGenerator.PathSeedChoice.LongWalk:
                pathSeeds.Add(VirtualPathGenerator.getPathSeedLongCorridor());
                break;
            case VirtualPathGenerator.PathSeedChoice.ZigZag:
                pathSeeds.Add(VirtualPathGenerator.getPathSeedZigzag());
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
        simulationManager.redirectionManager.UpdateRedirector(setup.redirector);
        simulationManager.redirectionManager.UpdateResetter(setup.resetter);

        // Setup Trail Drawing
        simulationManager.trailDrawer.enabled = !simulationManager.runAtFullSpeed;

        // Enable Waypoint
        simulationManager.motionManager.targetWaypoint.gameObject.SetActive(true);

        // Resetting User and World Positions and Orientations
        this.transform.position = Vector3.zero;
        this.transform.rotation = Quaternion.identity;
        // ESSENTIAL BUG FOUND: If you set the user first and then the redirection recipient, then the user will be moved, so you have to make sure to do it afterwards!
        //Debug.Log("Target User Position: " + setup.initialConfiguration.initialPosition.ToString("f4"));
        simulationManager.redirectionManager.headTransform.position = Utilities.UnFlatten(setup.initialConfiguration.initialPosition, simulationManager.redirectionManager.headTransform.position.y);
        //Debug.Log("Result User Position: " + redirectionManager.userHeadTransform.transform.position.ToString("f4"));
        simulationManager.redirectionManager.headTransform.rotation = Quaternion.LookRotation(Utilities.UnFlatten(setup.initialConfiguration.initialForward), Vector3.up);

        // Set up Tracking Area Dimensions
        simulationManager.envManager.UpdateTrackedSpaceDimensions(setup.trackingSizeShape.x, setup.trackingSizeShape.z);

        // Set up Virtual Path
        float sumOfDistances, sumOfRotations;
        simulationManager.motionManager.waypoints = VirtualPathGenerator.generatePath(setup.pathSeed, setup.initialConfiguration.initialPosition, setup.initialConfiguration.initialForward, out sumOfDistances, out sumOfRotations);
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
            foreach (Vector2 waypoint in simulationManager.motionManager.waypoints)
            {
                Transform poi = (new GameObject()).transform;
                poi.localPosition = Utilities.UnFlatten(waypoint);
                poi.parent = poiRoot;
                zigzagRedirectorWaypoints.Add(poi);
            }
            ((ZigZagRedirector)simulationManager.redirectionManager.redirector).waypoints = zigzagRedirectorWaypoints;
        }


        // Set First Waypoint Position and Enable It
        simulationManager.motionManager.targetWaypoint.position = new Vector3(simulationManager.motionManager.waypoints[0].x, simulationManager.motionManager.targetWaypoint.position.y, simulationManager.motionManager.waypoints[0].y);
        simulationManager.motionManager.waypointIterator = 0;

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
        simulationManager.trailDrawer.enabled = false;

        // Delete Virtual Path
        // THIS CAN BE MADE OPTIONAL IF NECESSARY
        simulationManager.trailDrawer.ClearTrail(TrailDrawer.VIRTUAL_TRAIL_NAME);

        // Disable Waypoint
        simulationManager.motionManager.targetWaypoint.gameObject.SetActive(true);

        // Disallow Walking
        simulationManager.userIsWalking = false;

        // Stop Logging
        simulationManager.statisticsLogger.EndLogging();

        // Gather Summary Statistics
        simulationManager.statisticsLogger.experimentResults.Add(simulationManager.statisticsLogger.GetExperimentResultForSummaryStatistics(getExperimentDescriptor(setup)));

        // Log Sampled Metrics
        if (simulationManager.statisticsLogger.logSampleVariables)
        {
            Dictionary<string, List<float>> oneDimensionalSamples;
            Dictionary<string, List<Vector2>> twoDimensionalSamples;
            simulationManager.statisticsLogger.GetExperimentResultsForSampledVariables(out oneDimensionalSamples, out twoDimensionalSamples);
            simulationManager.statisticsLogger.LogAllExperimentSamples(experimentDescriptorToString(getExperimentDescriptor(setup)), oneDimensionalSamples, twoDimensionalSamples);
        }

        // Take Snapshot In Next Frame (After User and Virtual Path Is Disabled)
        if (!simulationManager.runAtFullSpeed)
            simulationManager.takeScreenshot = true;

        // Prepared for new experiment
        experimentIterator++;
        //lastExperimentEndTime = Time.time;
        experimentInProgress = false;

        // Log All Summary Statistics To File
        if (experimentIterator == experimentSetups.Count)
        {
            if (simulationManager.averageTrialResults)
                simulationManager.statisticsLogger.experimentResults = mergeTrialSummaryStatistics(simulationManager.statisticsLogger.experimentResults);
            simulationManager.statisticsLogger.LogExperimentSummaryStatisticsResultsSCSV(simulationManager.statisticsLogger.experimentResults);
            Debug.Log("Last Experiment Complete");
            experimentComplete = true;
        }

        // Disabling Redirectors
        simulationManager.redirectionManager.RemoveRedirector();
        simulationManager.redirectionManager.RemoveResetter();
    }

    void OnGUI()
    {
        if (experimentComplete)
            GUI.Box(new Rect((int)(0.5f * Screen.width) - 75, (int)(0.5f * Screen.height) - 14, 150, 28), "Experiment Complete");
    }


    Dictionary<string, string> getExperimentDescriptor(ExperimentSetup setup)
    {
        Dictionary<string, string> descriptor = new Dictionary<string, string>();

        descriptor["redirector"] = setup.redirector.ToString();
        descriptor["resetter"] = setup.resetter == null ? "no_reset" : setup.resetter.ToString();
        descriptor["tracking_size_x"] = setup.trackingSizeShape.x.ToString();
        descriptor["tracking_size_z"] = setup.trackingSizeShape.z.ToString();

        return descriptor;
    }

    void printExperimentDescriptor(Dictionary<string, string> experimentDescriptor)
    {
        foreach (KeyValuePair<string, string> pair in experimentDescriptor)
        {
            Debug.Log(pair.Key + ": " + pair.Value);
        }
    }

    string experimentDescriptorToString(Dictionary<string, string> experimentDescriptor)
    {
        string retVal = "";
        int i = 0;
        foreach (KeyValuePair<string, string> pair in experimentDescriptor)
        {
            retVal += pair.Value;
            if (i != experimentDescriptor.Count - 1)
                retVal += "+";
            i++;
        }
        return retVal;
    }

    public List<Dictionary<string, string>> mergeTrialSummaryStatistics(List<Dictionary<string, string>> experimentResults)
    {
        List<Dictionary<string, string>> mergedResults = new List<Dictionary<string, string>>();
        Dictionary<string, string> mergedResult = null;
        float tempValue = 0;
        Vector2 tempVectorValue = Vector2.zero;
        for (int i = 0; i < experimentResults.Count; i++)
        {
            if (i % trialsForCurrentExperiment == 0)
            {
                mergedResult = new Dictionary<string, string>(experimentResults[i]);
            }
            else
            {
                foreach (KeyValuePair<string, string> pair in experimentResults[i])
                {
                    if (float.TryParse(pair.Value, out tempValue))
                    {
                        //Debug.Log("Averaged Float Values: " + pair.Value + ", " + mergedResult[pair.Key]);
                        mergedResult[pair.Key] = (i % trialsForCurrentExperiment == trialsForCurrentExperiment - 1) ? ((float.Parse(mergedResult[pair.Key]) + tempValue) / ((float)trialsForCurrentExperiment)).ToString() : (float.Parse(mergedResult[pair.Key]) + tempValue).ToString();
                    }
                    else if (TryParseVector2(pair.Value, out tempVectorValue))
                    {
                        //Debug.Log("Averaged Vector Values: " + pair.Value + ", " + mergedResult[pair.Key]);
                        mergedResult[pair.Key] = (i % trialsForCurrentExperiment == trialsForCurrentExperiment - 1) ? ((ParseVector2(mergedResult[pair.Key]) + tempVectorValue) / ((float)trialsForCurrentExperiment)).ToString() : (ParseVector2(mergedResult[pair.Key]) + tempVectorValue).ToString();
                    }
                }
            }
            if (i % trialsForCurrentExperiment == trialsForCurrentExperiment - 1)
                mergedResults.Add(mergedResult);
        }
        return mergedResults;
    }

    void determineInitialConfigurations(ref List<ExperimentSetup> experimentSetups)
    {
        for (int i = 0; i < experimentSetups.Count; i++)
        {
            ExperimentSetup setup = experimentSetups[i];
            if (setup.initialConfiguration.isRandom)
            {
                if (!onlyRandomizeForward)
                    setup.initialConfiguration.initialPosition = VirtualPathGenerator.getRandomPositionWithinBounds(-0.5f * setup.trackingSizeShape.x, 0.5f * setup.trackingSizeShape.x, -0.5f * setup.trackingSizeShape.z, 0.5f * setup.trackingSizeShape.z);
                setup.initialConfiguration.initialForward = VirtualPathGenerator.getRandomForward();
                //Debug.LogWarning("Random Initial Configuration for size (" + trackingSizeShape.x + ", " + trackingSizeShape.z + "): Pos" + initialConfiguration.initialPosition.ToString("f2") + " Forward" + initialConfiguration.initialForward.ToString("f2"));
                experimentSetups[i] = setup;
            }
            else if (Mathf.Abs(setup.initialConfiguration.initialPosition.x) > 0.5f * setup.trackingSizeShape.x || Mathf.Abs(setup.initialConfiguration.initialPosition.y) > 0.5f * setup.trackingSizeShape.z)
            {
                Debug.LogError("Invalid beginning position selected. Defaulting Initial Configuration to (0, 0) and (0, 1).");
                setup.initialConfiguration.initialPosition = Vector2.zero;
                setup.initialConfiguration.initialForward = Vector2.up;
                experimentSetups[i] = setup;
            }
            if (!setup.initialConfiguration.isRandom)
            {
                // Deal with diagonal hack
                if (setup.initialConfiguration.initialForward == Vector2.one)
                {
                    setup.initialConfiguration.initialForward = (new Vector2(setup.trackingSizeShape.x, setup.trackingSizeShape.z)).normalized;
                    experimentSetups[i] = setup;
                }
            }
        }
    }

    void updateSimulatedWaypointIfRequired()
    {
        if ((simulationManager.redirectionManager.currState.pos - Utilities.FlattenedPos3D(simulationManager.motionManager.targetWaypoint.position)).magnitude < simulationManager.motionManager.DISTANCE_TO_WAYPOINT_THRESHOLD)
        {
            updateWaypoint();
        }
    }

    public void updateWaypoint()
    {
        if (!experimentInProgress)
            return;
        if (simulationManager.motionManager.waypointIterator == simulationManager.motionManager.waypoints.Count - 1)
        {
            // When this is the last waypoint, stop 
            if (experimentIterator < experimentSetups.Count)
                endExperiment();
        }
        else
        {
            simulationManager.motionManager.waypointIterator++;
            simulationManager.motionManager.targetWaypoint.position = new Vector3(simulationManager.motionManager.waypoints[simulationManager.motionManager.waypointIterator].x, simulationManager.motionManager.targetWaypoint.position.y, simulationManager.motionManager.waypoints[simulationManager.motionManager.waypointIterator].y);
        }
    }

    bool TryParseVector2(string value, out Vector2 result)
    {
        result = Vector2.zero;
        if (!(value[0] == '(' && value[value.Length - 1] == ')' && value.Contains(",")))
            return false;
        result.x = float.Parse(value.Substring(1, value.IndexOf(",") - 1));
        result.y = float.Parse(value.Substring(value.IndexOf(",") + 2, value.IndexOf(")") - (value.IndexOf(",") + 2)));
        return true;
    }

    Vector2 ParseVector2(string value)
    {
        Vector2 result = Vector2.zero;
        result.x = float.Parse(value.Substring(1, value.IndexOf(",") - 1));
        result.y = float.Parse(value.Substring(value.IndexOf(",") + 2, value.IndexOf(")") - (value.IndexOf(",") + 2)));
        return result;
    }
}
