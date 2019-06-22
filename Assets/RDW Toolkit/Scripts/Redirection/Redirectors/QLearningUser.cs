using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QLearningUser : Redirector
{
    private Rasterization rasterization;
    //与QLearningTrainer中的action需要保持一致，重构代码时考虑两个脚本从同一处取数据
    List<float> actions = new List<float>();
    int state_size;
    int action_size;
    //索引对应状态，值对应最佳的动作
    float[] q_table;
    float currentGain;
    private const float MOVEMENT_THRESHOLD = 0.05f; // meters per second
    private const float ROTATION_THRESHOLD = 1.5f; // degrees per second
    //一次step造成的重定向操作持续时间
    const float duration = 1f;
    private float timer = duration;


    // Start is called before the first frame update
    void Start()
    {
        Initialize();
        InitializeQTable();
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer > duration)
        {
            takeAction();
            timer -= duration;
        }
        
    }
    void Initialize()
    {
        rasterization = this.gameObject.GetComponent<Rasterization>();
        //初始化actions
        actions.Clear();
        int step = 10;
        const int topLine = 50;
        const int bottomLine = -50;
        for (int i = bottomLine; i <= topLine; i += step) actions.Add(i * Mathf.Deg2Rad);
        state_size = rasterization.dimension * rasterization.dimension;
        action_size = actions.Count;
    }

    void InitializeQTable()
    {
        q_table = new float[state_size];
        QTableRecord qtableRecord = new QTableRecord(state_size, action_size);
        //需要手动设置表名
        qtableRecord.Load("QTable20190525");
        q_table = qtableRecord.DecodeQTable();       
    }

    void takeAction() {
        int state = rasterization.GetBodyDividedLocation();
        currentGain = actions[(int)q_table[state]];
        Debug.Log(" 目前增益：" + currentGain + " 状态序号:" + state );
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
}
