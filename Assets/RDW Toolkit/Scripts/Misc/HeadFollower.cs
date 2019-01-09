using UnityEngine;
using System.Collections;

public class HeadFollower : MonoBehaviour {

    [HideInInspector]
    public RedirectionManager redirectionManager;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        this.transform.position = redirectionManager.currState.pos;
        if (redirectionManager.currState.dir != Vector3.zero)
            this.transform.rotation = Quaternion.LookRotation(redirectionManager.currState.dir, Vector3.up);
	}
}
