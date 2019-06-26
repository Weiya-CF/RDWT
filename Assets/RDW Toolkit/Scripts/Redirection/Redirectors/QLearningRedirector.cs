using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QLearningRedirector : Redirector
{
    GridEnvironment grid_env;
    // the user is considered static beneath these thresholds
    private const float MOVEMENT_THRESHOLD = 0.05f; // meters per second
    private const float ROTATION_THRESHOLD = 1.5f; // degrees per second

    // "None", "SmallLeft", "LargeLeft", "SmallRight", "LargeRight"
    List<float> gains; 
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("The QLearning Redirector has been created");
        grid_env = GameObject.Find("GridEnv").GetComponent(typeof(GridEnvironment)) as GridEnvironment;
        gains = new List<float> { 0, -6f * Mathf.Deg2Rad, -15f * Mathf.Deg2Rad, 6f * Mathf.Deg2Rad, 15f * Mathf.Deg2Rad };
    }

    // Update is called once per frame
    void Update()
    {

    }

    public override void ApplyRedirection()
    {
        // Get Required Data
        Vector3 deltaPos = redirectionManager.deltaPos;
        float deltaDir = redirectionManager.deltaDir;

        if (deltaPos.magnitude / redirectionManager.simulationManager.GetDeltaTime() > MOVEMENT_THRESHOLD) // User is moving
        {
            if (!grid_env.done)
            {
                InjectCurvature(deltaPos.magnitude * gains[grid_env.sendAction] * Mathf.Rad2Deg);
            }
            
        }
        //Debug.Log("ApplyRedirection");
    }
}
