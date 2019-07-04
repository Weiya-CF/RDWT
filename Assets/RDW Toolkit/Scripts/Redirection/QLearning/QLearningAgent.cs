using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.IO;
using System;

public class QLearningAgent : Agent
{
    public float[][] q_table;   // The matrix containing the values estimates.
    float learning_rate = 0.5f; // The rate at which to update the value estimates given a reward.
    int action = -1;
    public int action_size = 0;
    float gamma = 0.99f; // Discount factor for calculating Q-target.
    float e = 1; // Initial epsilon value for random action selection.
    float eMin = 0.1f; // Lower bound of epsilon.
    int annealingSteps = 2000; // Number of steps to lower e to eMin.
    int lastState;

    /// <summary>
    /// Initialize the Q table based on the given environment
    /// </summary>
    /// <param name="env">the parameters of the given environment</param>
    public override void SendParameters(EnvironmentParameters env)
    {
        // Create a new Q table according to the env
        q_table = new float[env.state_size][];
        
        for (int i = 0; i < env.state_size; i++)
        {
            q_table[i] = new float[env.action_size];
            for (int j = 0; j < env.action_size; j++)
            {
                q_table[i][j] = 0.0f;
            }
        }

        action_size = env.action_size;
    }

    public override void ResetEpisode()
    {
        action = -1;
    }

    /// <summary>
    /// Picks an action to take from its current state.
    /// </summary>
    /// <returns>The action choosen by the agent's policy</returns>
    public override float[] GetAction()
    {
        action = q_table[lastState].ToList().IndexOf(q_table[lastState].Max());
        if (UnityEngine.Random.Range(0f, 1f) < e) {
            action = UnityEngine.Random.Range(0, action_size-1);
        }
        if (e > eMin) {
            e = e - ((1f - eMin) / (float)annealingSteps);
        }
        GameObject.Find("ETxt").GetComponent<Text>().text = "Epsilon: " + e.ToString("F2");
        //Debug.Log("laststate="+ lastState+ " action="+ action);
        //Debug.Log(q_table.Length + " " + q_table[lastState].Length);
        float currentQ = q_table[lastState][action];
        GameObject.Find("QTxt").GetComponent<Text>().text = "Current Q-value: " + currentQ.ToString("F2");
        
        return new float[1] { action };
    }

    /// <summary>
    /// Gets the values stored within the Q table.
    /// </summary>
    /// <returns>The average Q-values per state.</returns>
	public override float[] GetValue()
    {
        float[] value_table = new float[q_table.Length];
        for (int i = 0; i < q_table.Length; i++)
        {
            value_table[i] = q_table[i].Average();
        }
        return value_table;
    }

    /// <summary>
    /// Updates the value estimate matrix given a new experience (state, action, reward).
    /// </summary>
    /// <param name="state">The environment state the experience happened in.</param>
    /// <param name="reward">The reward recieved by the agent from the environment for it's action.</param>
    /// <param name="done">Whether the episode has ended</param>
    public override void SendState(List<float> state, float reward, bool done)
    {
        int nextState = Mathf.FloorToInt(state.First());
        if (action != -1)
        {
            if (done)
            {
                q_table[lastState][action] += learning_rate * (reward - q_table[lastState][action]);
            }
            else
            {
                q_table[lastState][action] += learning_rate * (reward + gamma * q_table[nextState].Max() - q_table[lastState][action]);
            }
        }
        lastState = nextState;
    }

    public void SaveQTable(string filePath)
    {
        string qtableString = "";

        for (int i = 0; i < this.q_table.Length; i++)
        {
            for (int j = 0; j < this.q_table[i].Length; j++)
            {
                qtableString += i + "," + j + "," + this.q_table[i][j] + "\n";
            }
        }

        using (FileStream fs = new FileStream(filePath, FileMode.Create))
        {
            using (StreamWriter writer = new StreamWriter(fs))
            {
                writer.Write(qtableString);
            }
        }
        Debug.Log("Q table has been saved to TXT file");
    }

    public void LoadQTable(string filePath, int numState, int numAction)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("The file is not found in this path:" + filePath);
            return;
        }

        StreamReader sr;
        sr = File.OpenText(filePath);
        string qtableString = sr.ReadToEnd();
        sr.Close();
        sr.Dispose();

        string[] qtable_lines = qtableString.Split('\n');

        q_table = new float[numState][];
        for (int i = 0; i < q_table.Length; i++)
        {
            q_table[i] = new float[numAction];
        }

        for (int i = 0; i < numState*numAction; i++)
        {       
            float[] qtableFloats = Array.ConvertAll(qtable_lines[i].Split(','), float.Parse);
            q_table[(int)qtableFloats[0]][(int)qtableFloats[1]] = qtableFloats[2];
        }

        Debug.Log("Q table has been loaded from TXT file");
    }
}
