using UnityEngine;
using Redirection;
using System.Collections.Generic;

public class RedirectionManager : MonoBehaviour {

    enum AlgorithmChoice { None, S2C, S2O, Zigzag, MPC, Q };
    enum ResetChoice { None, TwoOneTurn };

    [SerializeField]
    AlgorithmChoice condAlgorithm;

    [SerializeField]
    ResetChoice condReset;

    
    [Tooltip("Maximum translation gain applied")]
    [Range(0, 5)]
    public float MAX_TRANS_GAIN = 0.26F;
    
    [Tooltip("Minimum translation gain applied")]
    [Range(-0.99F, 0)]
    public float MIN_TRANS_GAIN = -0.14F;
    
    [Tooltip("Maximum rotation gain applied")]
    [Range(0, 5)]
    public float MAX_ROT_GAIN = 0.49F;
    
    [Tooltip("Minimum rotation gain applied")]
    [Range(-0.99F, 0)]
    public float MIN_ROT_GAIN = -0.2F;

    [Tooltip("Radius applied by curvature gain")]
    [Range(1, 23)]
    public float CURVATURE_RADIUS = 7.5F;
    [Tooltip("The game object that is being physically tracked (probably user's head)")]
    public Transform headTransform;

    [HideInInspector]
    public State currState;

    [HideInInspector]
    public State prevState;

    [HideInInspector]
    public Vector3 deltaPos = new Vector3(0, 0, 0);

    [HideInInspector]
    public float deltaDir = 0f;

    [HideInInspector]
    public Transform body;

    [HideInInspector]
    public Redirector redirector;
    [HideInInspector]
    public Resetter resetter;
    [HideInInspector]
    public ResetTrigger resetTrigger;

    [HideInInspector]
    public SimulationManager simulationManager;
    [HideInInspector]
    public System.Type redirectorType;
    [HideInInspector]
    public System.Type resetterType;

    [HideInInspector]
    public bool inReset = false;

    public struct State
    {
        public Vector3 pos, posReal; // user's virtual and real position
        public Vector3 dir, dirReal; // user's virtual and real direction

        private void Reset()
        {
            pos = new Vector3(0, 0, 0); posReal = new Vector3(0, 0, 0);
            dir = new Vector3(0, 0, 0); dirReal = new Vector3(0, 0, 0);
        }

    }; // the state of the environment

    void Awake()
    {
        GetBody();
        GetRedirector();
        GetResetter();
        GetResetTrigger();

        SetReferenceForRedirector();
        SetReferenceForResetter();
        SetReferenceForResetTrigger();
        SetBodyReferenceForResetTrigger();
    }

	// Use this for initialization
	void Start () {

        UpdateCurrentUserState();
        UpdatePreviousUserState();
	}
	
	// Update is called once per frame
	void Update () {

	}

    public void Initialize()
    {

        switch (condAlgorithm)
        {
            case AlgorithmChoice.None:
                redirectorType = typeof(NullRedirector);
                break;
            case AlgorithmChoice.S2C:
                redirectorType = typeof(S2CRedirector);
                break;
            case AlgorithmChoice.S2O:
                redirectorType = typeof(S2ORedirector);
                break;
            case AlgorithmChoice.Zigzag:
                redirectorType = typeof(ZigZagRedirector);
                break;
            case AlgorithmChoice.MPC:
                redirectorType = typeof(MPCRedirector);
                break;
            case AlgorithmChoice.Q:
                redirectorType = typeof(QLearningRedirector);
                break;
        }

        switch (condReset)
        {
            case ResetChoice.None:
                resetterType = typeof(NullResetter);
                break;
            case ResetChoice.TwoOneTurn:
                resetterType = typeof(TwoOneTurnResetter);
                break;
        }

        
        resetTrigger.Initialize();
        // Resetter needs ResetTrigger to be initialized before initializing itself
        if (resetter != null)
            resetter.Initialize();

        // Enabling/Disabling Redirectors
        this.UpdateRedirector(this.redirectorType);
        this.UpdateResetter(this.resetterType);
    }

    void UpdateBodyPose()
    {
        body.position = Utilities.FlattenedPos3D(this.headTransform.position);
        body.rotation = Quaternion.LookRotation(Utilities.FlattenedDir3D(this.headTransform.forward), Vector3.up);
    }

    public void SetReferenceForRedirector()
    {
        if (redirector != null)
        {
            redirector.redirectionManager = this;
            redirector.simulationManager = this.simulationManager;
        }
    }

    public void SetReferenceForResetter()
    {
        if (resetter != null)
        {
            resetter.redirectionManager = this;
            resetter.simulationManager = this.simulationManager;
        } 
    }

    public void SetReferenceForResetTrigger()
    {
        if (resetTrigger != null)
            resetTrigger.redirectionManager = this;
    }

    public void SetBodyReferenceForResetTrigger()
    {
        if (resetTrigger != null && body != null)
        {
            // NOTE: This requires that getBody gets called before this
            resetTrigger.bodyCollider = body.GetComponentInChildren<CapsuleCollider>();
        }
    }

    public void GetRedirector()
    {
        redirector = this.gameObject.GetComponent<Redirector>();
        if (redirector == null)
            this.gameObject.AddComponent<NullRedirector>();
        redirector = this.gameObject.GetComponent<Redirector>();
    }

    public void GetResetter()
    {
        resetter = this.gameObject.GetComponent<Resetter>();
        if (resetter == null)
            this.gameObject.AddComponent<NullResetter>();
        resetter = this.gameObject.GetComponent<Resetter>();
    }

    public void GetResetTrigger()
    {
        resetTrigger = this.gameObject.GetComponentInChildren<ResetTrigger>();
    }

    public void GetBody()
    {
        body = transform.Find("Body");
    }
 
    public void UpdateCurrentUserState()
    {
        currState.pos = Utilities.FlattenedPos3D(this.headTransform.position);
        currState.posReal = Utilities.GetRelativePosition(currState.pos, this.transform);
        currState.dir = Utilities.FlattenedDir3D(this.headTransform.forward);
        currState.dirReal = Utilities.FlattenedDir3D(Utilities.GetRelativeDirection(currState.dir, this.transform));
    }

    public void UpdatePreviousUserState()
    {
        prevState.pos = Utilities.FlattenedPos3D(this.headTransform.position);
        prevState.posReal = Utilities.GetRelativePosition(prevState.pos, this.transform);
        prevState.dir = Utilities.FlattenedDir3D(this.headTransform.forward);
        prevState.dirReal = Utilities.FlattenedDir3D(Utilities.GetRelativeDirection(prevState.dir, this.transform));
    }

    public void CalculateStateChanges()
    {
        deltaPos = currState.pos - prevState.pos;
        deltaDir = Utilities.GetSignedAngle(prevState.dir, currState.dir);
    }

    public void Run()
    {
        UpdateCurrentUserState();
        CalculateStateChanges();

        // BACK UP IN CASE UNITY TRIGGERS FAILED TO COMMUNICATE RESET (Can happen in high speed simulations)
        if (resetter != null && !inReset && resetter.IsUserOutOfBounds())
        {
            Debug.LogWarning("Reset Aid Helped!");
            OnResetTrigger();
        }

        if (inReset)
        {
            if (resetter != null)
            {
                resetter.ApplyResetting();
                Debug.LogWarning("Reset Calledddd!");
            }
        }
        else
        {
            if (redirector != null)
            {
                redirector.ApplyRedirection();
                Debug.LogWarning("Redirection Calledddd!");
            }
        }

        this.simulationManager.statisticsLogger.UpdateStats();

        UpdatePreviousUserState();

        UpdateBodyPose();
    }

    public void OnResetTrigger()
    {
        //print("RESET TRIGGER");
        if (inReset)
            return;
        //print("NOT IN RESET");
        //print("Is Resetter Null? " + (resetter == null));
        if (resetter != null && resetter.IsResetRequired())
        {
            Debug.Log("RESET WAS REQUIRED");
            resetter.InitializeReset();
            inReset = true;

            // stop the planning thread
            if(redirector is MPCRedirector)
            {
                ((MPCRedirector)redirector).toPause = true;
                Debug.LogWarning("planning is paused");
            }
        }
    }

    public void OnResetEnd()
    {
        //print("RESET END");
        resetter.FinalizeReset();
        inReset = false;

        // start the planning thread
        if (redirector is MPCRedirector)
        {
            ((MPCRedirector)redirector).ResumeThread();
            Debug.LogWarning("planning is resumed");
        }
    }

    public void RemoveRedirector()
    {
        this.redirector = this.gameObject.GetComponent<Redirector>();
        if (this.redirector != null)
            Destroy(redirector);
        redirector = null;
    }

    public void UpdateRedirector(System.Type redirectorType)
    {
        RemoveRedirector();
        this.redirector = (Redirector) this.gameObject.AddComponent(redirectorType);
        //this.redirector = this.gameObject.GetComponent<Redirector>();
        SetReferenceForRedirector();
    }

    public void RemoveResetter()
    {
        this.resetter = this.gameObject.GetComponent<Resetter>();
        if (this.resetter != null)
            Destroy(resetter);
        resetter = null;
    }

    public void UpdateResetter(System.Type resetterType)
    {
        RemoveResetter();
        if (resetterType != null)
        {
            this.resetter = (Resetter) this.gameObject.AddComponent(resetterType);
            //this.resetter = this.gameObject.GetComponent<Resetter>();
            SetReferenceForResetter();
            if (this.resetter != null)
                this.resetter.Initialize();
        }
    }

 


}
