using UnityEngine;
using System.Collections.Generic;


public class SimulationManager : MonoBehaviour {

    [HideInInspector]
    public RedirectionManager redirectionManager;

    [HideInInspector]
    public MotionManager motionManager;

    // Experiment Variables
    System.Type redirector = null;
    System.Type resetter = null;
    
    [SerializeField]
    bool runInSimulationMode = false;

    
    [SerializeField]
    bool runAtFullSpeed = false;
    [SerializeField]
    public bool onlyRandomizeForward = true;
    [SerializeField]
    bool averageTrialResults = false;
    [SerializeField]
    public float DISTANCE_TO_WAYPOINT_THRESHOLD = 0.3f; // Distance requirement to trigger waypoint

    bool takeScreenshot = false;
    

    [HideInInspector]
    public List<Vector2> waypoints;
    [HideInInspector]
    public int waypointIterator = 0;
    [HideInInspector]
    public bool userIsWalking = false;

	
    // Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
    void Update()
    {
        

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
        if ((redirectionManager.currState.pos - Utilities.FlattenedPos3D(redirectionManager.targetWaypoint.position)).magnitude < DISTANCE_TO_WAYPOINT_THRESHOLD)
        {
            redirectionManager.simulationManager.updateWaypoint();
        }
    }

    public void updateWaypoint()
    {
        if (!experimentInProgress)
            return;
        if (waypointIterator == waypoints.Count - 1)
        {
            // When this is the last waypoint, stop 
            if (experimentIterator < experimentSetups.Count)
                endExperiment();
        }
        else
        {
            waypointIterator++;
            redirectionManager.targetWaypoint.position = new Vector3(waypoints[waypointIterator].x, redirectionManager.targetWaypoint.position.y, waypoints[waypointIterator].y);
        }
    }
}
