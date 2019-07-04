using UnityEngine;
using System.Collections;

public class HeadFollower : MonoBehaviour {

    [HideInInspector]
    public SimulationManager simulationManager;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        this.transform.position = simulationManager.redirectionManager.currState.pos;
        if (!simulationManager.simuEnded && simulationManager.redirectionManager.currState.dir != Vector3.zero)
            this.transform.rotation = Quaternion.LookRotation(simulationManager.redirectionManager.currState.dir, Vector3.up);
	}
}
