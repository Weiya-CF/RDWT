using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rasterization : MonoBehaviour
{
    #region 属性
    private const float halfLength = 0.5f;

    #region 现实场景栅格化属性
    [Tooltip("场地GameObject,场地应为正方形，且scale与距离比例一一对应")]
    public GameObject space;
    [Tooltip("虚拟用户")]
    public GameObject body;
    [Tooltip("场地划分格数，划分结果是该值的平方")]
    public int dimension;
    //用于重置关于reset时间reward的标记
    //该标记在RedirectionManager.OnResetEnd()中被置为2，在OnResetTrigger被置为1，0表示可以正常计时,3表示已经进行过reward计算
    //逻辑上讲，这个变量不应该由Rasterization持有
    [HideInInspector]
    public int resetState = 0;


    //场地的四个角
    [HideInInspector]
    public GameObject CenterCorner;
    private GameObject Corner1;
    private GameObject Corner2;
    private GameObject Corner3;
    private GameObject Corner4;
    //偏移量
    private float bias=0f;
    [HideInInspector]
    //场地大小,在GetCorners中初始化
    public int spaceSize;
    //斜角半距离，在GetCorners中初始化
    private float obliqueDistance;
    #endregion
    #region 虚拟场景栅格化属性
    [Tooltip("虚拟场地GameObject")]
    public GameObject sceneGameObject;
    //场地边长的一半，暂时写死
    const float vHalfSizw = 10;
    #endregion
    #endregion

    void Awake()
    {
        GetCorners();
        GetBias();
    }

    private void Update()
    {
        //Debug.Log(GetBodyDividedLocation()+" "+GetBodySpacePos().x+" "+GetBodySpacePos().z);
    }

    //获取并创建场地四个角位置及中心位置
    private void GetCorners()
    {
        float sizeX = space.GetComponent<Transform>().localScale.x;
        float sizeZ = space.GetComponent<Transform>().localScale.z;
        if (sizeX != sizeZ||space==null) { Debug.Log("场地形状非正方形|场地未指定Gameobject,栅格化失败"); return; }
        //初始化两个场地属性，边长和斜角距离
        spaceSize = (int)(sizeX * sizeZ);
        obliqueDistance = Mathf.Sqrt(spaceSize)/2;

        float halfLength = sizeX / 2;
        Corner1 = new GameObject("Corner1");
        Corner2 = new GameObject("Corner2");
        Corner3 = new GameObject("Corner3");
        Corner4 = new GameObject("Corner4");
        CenterCorner = new GameObject("CenterCorner");
        Corner1.GetComponent<Transform>().position = new Vector3(halfLength, 0, halfLength);
        Corner2.GetComponent<Transform>().position = new Vector3(halfLength, 0, -halfLength);
        Corner3.GetComponent<Transform>().position = new Vector3(-halfLength, 0, -halfLength);
        Corner4.GetComponent<Transform>().position = new Vector3(-halfLength, 0, halfLength);
        CenterCorner.GetComponent<Transform>().position = new Vector3(0, 0, 0);
        Corner1.GetComponent<Transform>().SetParent(space.GetComponent<Transform>());
        Corner2.GetComponent<Transform>().SetParent(space.GetComponent<Transform>());
        Corner3.GetComponent<Transform>().SetParent(space.GetComponent<Transform>());
        Corner4.GetComponent<Transform>().SetParent(space.GetComponent<Transform>());
        CenterCorner.GetComponent<Transform>().SetParent(space.GetComponent<Transform>());
    }
    private void GetBias() { bias = 1.0f/ dimension; }
    //获取虚拟用户相对于场景的position
    private Vector3 GetBodySpacePos()
    {
        Vector3 bodyPos = body.GetComponent<Transform>().position;
        return space.GetComponent<Transform>().InverseTransformPoint(bodyPos);
    }
    //计算虚拟用户所属的栅格，外部调用接口
    public int GetBodyDividedLocation()
    {
        Vector3 bodyPos = GetBodySpacePos();
        float posX = bodyPos.x;
        float posZ = bodyPos.z;
        int biasX = (int)((posX - (-halfLength)) / bias);
        int biasZ = (int)((posZ - (-halfLength)) / bias);
        return biasZ * dimension + biasX ;
    }
    //计算距离中心的比例系数，距离/正方形斜角距离
    public float GetDistanceRatio()
    {
        Vector3 bodyPosition = body.GetComponent<Transform>().position;
        bodyPosition.y = 0;
        return -Vector3.Distance(bodyPosition, CenterCorner.GetComponent<Transform>().position) / obliqueDistance;
         
    }
 

}
