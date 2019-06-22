using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Redirection;

public class QLearningTrainer : Redirector
{
    [HideInInspector]
    public float[][] q_table;   // The matrix containing the values estimates.第一个索引为状态，第二个索引为动作
    float learning_rate = 0.5f; // The rate at which to update the value estimates given a reward.
    float gamma = 0.99f; // Discount factor for calculating Q-target.
    float e = 1; // Initial epsilon value for random action selection.
    float eMin = 0.1f; // Lower bound of epsilon.
    float deltae = 0.01f;//每次迭代e的减少值
    int annealingSteps = 2000; // Number of steps to lower e to eMin.
    int stepCount = 0;
    const int maxstep = 5000;
    List<float> actions=new List<float>();
    int action_size;
    //List<float> observation=new List<float>();
    //int observation_size;
    int state_size;
    int[] oldStateAction=new int[2];//状态动作对，二维数组，float[0]为状态标记，float[1]为动作标记 

    // the user is considered static beneath these thresholds
    private const float MOVEMENT_THRESHOLD = 0.05f; // meters per second
    private const float ROTATION_THRESHOLD = 1.5f; // degrees per second

    private Rasterization rasterization;
    //当前施加的增益值
    private float currentGain = 0;
    //一次step造成的重定向操作持续时间
    const float duration = 1f;
    private float timer = duration;
    const float rewardValueLimit=0.5f;
    //计算关于reset的reward相关变量
    private float resetTimer = 0;
    const int timeLimit = 10;
    //曲率上下限
    const int topLine = 50;
    const int bottomLine = -50;
    //训练的次数上限,TODO
    const int trainNum = 500;
    int trainCount = 0;
    bool eposideOver = false;
    Vector3 originCoordiate;
    Vector3 biasMazeToBody;

    private void Awake()
    {
        Initilalize();
        InitilalizeQTable();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        //处理step
        if(timer>=duration)
        {
            timer -= duration;
            ++stepCount;
            int state = rasterization.GetBodyDividedLocation();
            step(chooseAction(state),state);
        }
        //处理resetTimer
        switch(rasterization.resetState)
        {
            case 0: resetTimer += Time.deltaTime;
                break;
            case 1:resetTimer = 0;
                break;
            case 2:rasterization.resetState = 0;
                break;
            default:break;
        }
    }


    //硬编码操作值,状态值,初始化场地坐标(以便重新reset)
    private void Initilalize (){
        int step = 10;//每10度取一个曲率增益
        rasterization = this.gameObject.GetComponent<Rasterization>();
        actions.Clear();
        for(int i=bottomLine;i<=topLine;i+=step) actions.Add(i * Mathf.Deg2Rad);
        action_size = actions.Count;
        //observation_size = 1;//观察空间暂时为1
        //observation.Clear();
        //observation[0] = rasterization.GetBodyDividedLocation();
        //初始化状态储存器，这步存疑
        oldStateAction[0] = rasterization.GetBodyDividedLocation();
        oldStateAction[1] = 5;
        //场地初始位置,计算两者间的bias
        originCoordiate = GameObject.Find("Redirected User").GetComponent<Transform>().position;
        //biasMazeToBody = GameObject.Find("SceneObject").GetComponent<Transform>().position - GameObject.Find("Body").GetComponent<Transform>().position;
    }
    //初始化q表
    private void InitilalizeQTable()
    {
        state_size = rasterization.dimension * rasterization.dimension;
        //q-table的状态总数从rasterization中得到
        q_table = new float[rasterization.dimension * rasterization.dimension][];
        for (int i = 0; i < state_size; ++i)
        {
            q_table[i] = new float[action_size];
            for (int j = 0; j < action_size; ++j) q_table[i][j] = 0f;
        }

    }

    #region q-learning主体部分
    private void reset()
    {
        
    }

    private void step(int action,int state)
    {
        if (trainCount < trainNum)
        {
            Debug.Log("步数："+trainCount+" 目前增益：" + currentGain + " 状态序号:" + oldStateAction[0] + " e" + e);
            float reward = CalculateReward((int)actions[oldStateAction[1]]);
            updateQtable(reward, state);
            oldStateAction[0] = state;
            oldStateAction[1] = action;
            //递减e值
            if (e > eMin) e -= deltae;
            currentGain = actions[action];
            trainCount++;
        }
        else RecordQTable();
    }
    //返回action标记,如果遇到多个同样的value值action，只是返回第一个
    private int chooseAction(int state)
    {
        int maxvalueAction=5;//动作标记为5的索引为不施加重定向
        float max = Mathf.NegativeInfinity;
        float random;
        if ((random=Random.Range(0f, 1)) < e)
        {
            Debug.Log("随机选择action "+random);
            return Random.Range(0, action_size);
        }
        else
        {
            for(int i=0;i<action_size;++i)
            {
                if(q_table[state][i]>max)
                {
                    max = q_table[state][i];
                    maxvalueAction = i;
                }
            }
            Debug.Log("非随机选择action");
        }
        return maxvalueAction;
    }

    private void updateQtable(float reward,int newState)
    {
        //寻找最大价值的动作
        float maxActionValue = Mathf.NegativeInfinity;
        for (int i = 0; i < action_size; ++i)
            if (q_table[newState][i] > maxActionValue) maxActionValue = q_table[newState][i];
        //前后两次状态值一样，q表就不更新
        if (oldStateAction[0] == newState) return;
        //更新Q-table
        //Q(s, a) += alpha * (reward(s,a) + gamma*max(Q(s') - Q(s,a))
        float oldValue = q_table[oldStateAction[0]][oldStateAction[1]];
        q_table[oldStateAction[0]][oldStateAction[1]] += learning_rate * (reward + gamma * maxActionValue - oldValue);
    }
    #endregion
    //计算奖励函数
    private float CalculateReward(int actionValue)
    {
        float reward;
        //float rDistance = rasterization.GetDistanceRatio();
        //float rAction = -Mathf.Abs((float)actionValue / topLine);
        //float rDuration = ResetReward();
        //return rDistance + rAction + rDuration;
        if (rasterization.resetState == 1)
        {
            reward = -100;
            rasterization.resetState = 3;
        }
        else reward = 1;
        return reward;
    }

    //计算reset时长奖励
    private float ResetReward()
    {
        float resetReward = 0;
        if (resetTimer < timeLimit) resetReward = resetTimer / timeLimit * rewardValueLimit;
        else resetReward = rewardValueLimit;
        return resetReward;
    }

    public override void ApplyRedirection()
    {
        // Get Required Data
        Vector3 deltaPos = redirectionManager.deltaPos;
        float deltaDir = redirectionManager.deltaDir;

        if (deltaPos.magnitude / redirectionManager.GetDeltaTime() > MOVEMENT_THRESHOLD) // User is moving
        {
                InjectCurvature(deltaPos.magnitude * currentGain * Mathf.Rad2Deg);
        }

    }
    //重置函数
    private void resetEposide()
    {
        //停止重定向
        currentGain = 0;
        //随机设置玩家在现实场景中的位置
        float realityHalfLength = Mathf.Sqrt(rasterization.spaceSize)/2;
        float x = Random.Range(-realityHalfLength, realityHalfLength);
        float z = Random.Range(-realityHalfLength, realityHalfLength);
        rasterization.body.GetComponent<Transform>().position = new Vector3(x, 0, z);
        //重置虚拟场景位置
        GameObject.Find("SceneObject").GetComponent<Transform>().position = new Vector3(x, 0, z) + biasMazeToBody;
    }
    //训练完后记录Q表
    private void RecordQTable()
    {
        QTableRecord qtableRecord = new QTableRecord(state_size, action_size);
        qtableRecord.Load("QTable");
        //循环在表中添加数据
        for (int i = 0; i < state_size; ++i)
        {
            float maxValue = Mathf.NegativeInfinity;
            //5为不施加任何增益
            int maxAction = 5;
            for (int j = 0; j < action_size; ++j) {
                if(q_table[i][j]>maxValue)
                {
                    maxValue = q_table[i][j];
                    maxAction = j;
                }
            }
            qtableRecord.AddData(i, maxAction);
        }
        qtableRecord.Save();
    }
  

}

//环境step函数返回的三元组,暂时不需要使用
public class result
{
    public float reward;
    public bool done;
    public int observation;
}
