using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridEnvironment : Environment
{
    [SerializeField]
    public SimulationManager simulationManager;

    RedirectionManager redirectionManager;

    public bool loadExistingQTable;
    public bool saveQTable;


    [Tooltip("How we divide the tracking zone into grids")]
    public int gridSize;
 
    float episodeReward;

    public void Initialize()
    {
        if (this.simulationManager.simuMode == SimulationManager.SimuMode.Learn)
        {
            this.redirectionManager = this.simulationManager.redirectionManager;
            waitTime = 0.001f;
            BeginNewGame();
            Debug.Log("The Environment " + this.envParameters.env_name + " has been created");
        }
    }

    /// <summary>
    /// Restarts the learning process with a new walking condition.
    /// </summary>
    public void BeginNewGame()
    {
        sendAction = 0;

        SetUp();
        agent = new QLearningAgent();

        // if we continue a previous training
        if (this.loadExistingQTable)
        {
            Debug.Log(this.loadExistingQTable);
            #if UNITY_EDITOR
            ((QLearningAgent)agent).LoadQTable("Assets/Resources/qtable.txt", envParameters.state_size, envParameters.action_size);
            #endif
            ((QLearningAgent)agent).action_size = envParameters.action_size;
        }
        else
        {
            agent.SendParameters(envParameters);
        }
        
        Reset();

    }

    /// <summary>
    /// Establish the walking environment
    /// </summary>
    public override void SetUp()
    {
        envParameters = new EnvironmentParameters()
        {
            observation_size = 0,
            state_size = gridSize * gridSize,
            action_descriptions = new List<string>() { "None", "SmallLeft", "LargeLeft", "SmallRight", "LargeRight" },
            action_size = 5,
            env_name = "GridRW",
            action_space_type = "discrete",
            state_space_type = "discrete",
            num_agents = 1
        };

    }

    /// <summary>
    /// Resizes the grid to the specified size.
    /// </summary>
    public void SetEnvironment()
    {
        // this part is done in the resetter
    }

    private void Update()
    {
        if (this.simulationManager.simuMode == SimulationManager.SimuMode.Learn && !this.simulationManager.simuEnded)
        {
            //waitTime = 1.0f - GameObject.Find("Slider").GetComponent<Slider>().value;
            waitTime = 1f;
            RunMdp();
        }
        
    }

    /// <summary>
    /// Allows the agent to take actions, and set rewards accordingly.
    /// </summary>
    /// <param name="action">Action.</param>
    public override void MiddleStep(int action)
    {
        reward = 0f;
        // "None", "SmallLeft", "LargeLeft", "SmallRight", "LargeRight"
        if (action == 0)
        {
            reward = 0;
        }
        else if (action == 1 || action == 3)
        {
            reward = -0.5f;
        }

        else if (action == 2 || action == 3)
        {
            reward = -1f;
        }

        // A negative reward when the user is near the border of tracking zone, and facing the wall
        if (redirectionManager.resetter.IsUserOutOfBounds() && redirectionManager.resetter.IsResetRequired())
        {
            reward = -100;
        }

        //LoadSpheres();
        episodeReward += reward;
        GameObject.Find("RTxt").GetComponent<Text>().text = "Episode Reward: " + episodeReward.ToString("F2");

    }

    /// <summary>
    /// Gets the agent's current position and transforms it into a discrete integer state.
    /// </summary>
    /// <returns>The state.</returns>
    public override List<float> collectState()
    {
        List<float> state = new List<float>();
        Vector2 userpos = new Vector2(redirectionManager.currState.posReal.x +
            simulationManager.envManager.trackedSpace.localScale.x * 0.5f, redirectionManager.currState.posReal.z + 
            simulationManager.envManager.trackedSpace.localScale.z * 0.5f);

        userpos = userpos / simulationManager.envManager.trackedSpace.localScale.x * this.gridSize;
        float point = gridSize * Mathf.FloorToInt(userpos.y) + Mathf.FloorToInt(userpos.x);
        state.Add(point);

        return state;
    }

    /// <summary>
    /// Resets the episode by placing the objects in their original positions.
    /// </summary>
    public override void Reset()
    {
        base.Reset();

        episodeReward = 0;

        // reset user and trail drawing
        this.simulationManager.ResetEpisode();

        // reset the internal state of agent
        this.agent.ResetEpisode();
       
        EndReset();

       
    }

}
