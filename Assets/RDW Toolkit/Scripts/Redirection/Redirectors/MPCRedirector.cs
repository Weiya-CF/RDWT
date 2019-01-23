using UnityEngine;
using System.Collections.Generic;
using Redirection;
using System.Threading;

public class MPCRedirector : Redirector {
    // DEFINITIONS
    public enum ActionType { ZERO, TRANSLATION, ROTATION, CURVATURE, RESET }; // Translation is currently not used

    public struct Action
    {
        public ActionType type;
        public float gain;
        public float cost;
    }; // the actions that can be taken by the redirector

    public struct MPCResult {
        public Action bestAction;
        public float bestCost;
    }; // to store the result of planning method

    // VARIABLES

    private List<Action> actions;
    private List<Segment> segments;
    private List<Transition> transitions; // a transition connects two segments
    private Action currAction; // current action suggested by the redirector
    private Segment currSeg; // current segment that the user is in

    private int planningHorizon = 5; // how many steps (stages) we look forward
    private float stageDuration = 2; // how long is a single stage
    private float declineFactor = 0.9f; // used in Plan method, the future state is less credible

    // the user is considered static beneath these thresholds
    private const float MOVEMENT_THRESHOLD = 0.05f; // meters per second
    private const float ROTATION_THRESHOLD = 1.5f; // degrees per second

    Thread ChildThread = null; // we do planning on a separate thread
    EventWaitHandle ChildThreadWait = new EventWaitHandle(true, EventResetMode.ManualReset); // for thread sync
    private bool runningMPC = true;
    public bool toPause = false; // a flag to pause the planning while resetting
 
    // FUNCTIONS

    public void Awake()
    {
        // they are hard-written in the code for now
        LoadActionsFromFile(""); // not done
        LoadSegmentsAndTrans(""); // not done

        // initialization
        this.currAction = GetZeroAction();
        this.currSeg = GetSegmentById(1);
    }

    /// <summary>
    /// This is the function called by the child thread.
    /// </summary>
    void ChildThreadLoop()
    {
        while (runningMPC)
        {
            if (toPause)
            {
                this.PauseThread();
            }

            // Do Update
            Debug.LogWarning("Starting MPC Loop at pos: "+this.redirectionManager.currState.posReal);

            var watch = System.Diagnostics.Stopwatch.StartNew(); // start a timer

            MPCResult result = Plan(this.redirectionManager.currState, this.currSeg, this.planningHorizon);
            this.currAction = result.bestAction;

            watch.Stop(); // stop the timer
            var deltaT = watch.ElapsedMilliseconds;
            Debug.Log("best action: " + this.currAction.type + ", gain:" +this.currAction.gain +", best cost: " + result.bestCost + ", duration: " + deltaT);
            
            //Thread.Sleep(1000); // use this if you want to be more stable
        }
    }

    public void PauseThread()
    {
        ChildThreadWait.Reset();
    }

    public void ResumeThread()
    {
        this.toPause = false;
        ChildThreadWait.Set();
    }

    public void Start()
    {

        ChildThread = new Thread(ChildThreadLoop);
        ChildThread.Start();
    }

    public void Update()
    {
        UpdateCurrentSegment(redirectionManager.currState);
    }

    void OnApplicationQuit()
    {
        runningMPC = false;
    }

    // TODO
    private void LoadActionsFromFile(string file)
    {
        this.actions = new List<Action>();
        this.actions.Add(new Action { type = ActionType.ZERO, gain = 0f, cost = 0f });
        this.actions.Add(new Action { type = ActionType.ROTATION, gain = 1.2f, cost = 0f });
        this.actions.Add(new Action { type = ActionType.ROTATION, gain = 0.8f, cost = 0f });
        this.actions.Add(new Action { type = ActionType.CURVATURE, gain = 6f * Mathf.Deg2Rad, cost = 0f });
        this.actions.Add(new Action { type = ActionType.CURVATURE, gain = -6f * Mathf.Deg2Rad, cost = 0f });
        this.actions.Add(new Action { type = ActionType.CURVATURE, gain = 15f * Mathf.Deg2Rad, cost = 1f });
        this.actions.Add(new Action { type = ActionType.CURVATURE, gain = -15f * Mathf.Deg2Rad, cost = 1f });
        this.actions.Add(new Action { type = ActionType.RESET, gain = 0f, cost = 500f });
    }

    // TODO
    private void LoadSegmentsAndTrans(string path)
    {
        Debug.Log("Initiate Motion Graph!");
        this.transitions = new List<Transition>();
        this.segments = new List<Segment>();

        this.segments.Add(new LineSegment(1, new Vector2(0, 0), new Vector2(0, 1000)));

        //// Read the file and display it line by line.  
        //System.IO.StreamReader file =
        //    new System.IO.StreamReader(@"c:\test.txt");
        //while ((line = file.ReadLine()) != null)
        //{
        //    System.Console.WriteLine(line);
        //    counter++;
        //}

        //file.Close();
    }

    /// <summary>
    /// Each segment has an id, and segments are ordered with increasing id value.
    /// Note: this is only a temporory setting that will be removed in future version
    /// </summary>
    /// <param name="id"></param> the id of a segment
    /// <returns></returns>
    private Segment GetSegmentById(int id)
    {
        foreach (var seg in this.segments)
        {
            if (seg.id == id)
            {
                return seg;
            }
        }
        return null;
    }

    public List<Action> GetActionsByType(ActionType type)
    {
        List<Action> result = new List<Action>();
        foreach (var action in this.actions)
        {
            if (action.type == type)
            {
                result.Add(action);
            }
        }
        return result;
    }

    public Action GetZeroAction()
    {
        return this.actions[0];
    }

    /// <summary>
    /// Get a list of allowed actions according to the type of segment,
    /// eg. if the user is moving forward in a straight line, then no rotation gains are allowed.
    /// </summary>
    /// <param name="seg"></param> the query segment
    /// <returns></returns>
    public List<MPCRedirector.Action> GetAllowedActions(Segment seg)
    {
        List<MPCRedirector.Action> list = new List<MPCRedirector.Action>();
        list.AddRange(GetActionsByType(MPCRedirector.ActionType.ZERO));
        if (seg is LineSegment || seg is ArcSegment)
        {
            list.AddRange(GetActionsByType(MPCRedirector.ActionType.CURVATURE));
            list.AddRange(GetActionsByType(MPCRedirector.ActionType.RESET));
        }
        else if (seg is RotationSegment)
        {
            list.AddRange(GetActionsByType(MPCRedirector.ActionType.ROTATION));
        }
        return list;
    }

    /// <summary>
    /// To see if we need to update the current segment according to user's state.
    /// Whether a user stays at current segment, or he/she will jump into the next segment if near the end of the current one.
    /// </summary>
    /// <param name="state"></param> the state of the environment
    public void UpdateCurrentSegment(RedirectionManager.State state)
    {
        if (currSeg.IsEndOfSegment(state))
        {
            // We need to follow the Transition to the next segment
            Segment nextSeg = currSeg.transitions[0].endSeg;
            foreach (var trans in currSeg.transitions)
            {
                // return the segment with smaller id (user's path is predefined by segment id)
                if (trans.endSeg.id < nextSeg.id)
                {
                    nextSeg = trans.endSeg;
                }

            }
            currSeg = nextSeg;
        }
    }

    /// <summary>
    /// Get an estimate of the future state based on the current state, redirector's action and the path to be followed.
    /// ATTENTION: This method is only used for simulation(planning)! A user's actual state is only updated by the tracking system.
    /// </summary>
    /// <param name="s"></param> the current state of the environment.
    /// <param name="a"></param> action to be taken by the redirector.
    /// <param name="seg"></param> the path that the user is walking on in the current stage.
    public RedirectionManager.State ApplyStateUpdate(RedirectionManager.State state, Action a, Segment seg)
    {
        RedirectionManager.State newState = new RedirectionManager.State();
        if (seg is LineSegment)
        {
            LineSegment segment = (LineSegment)seg;
            Vector2 tangentDir = (segment.endPos - Utilities.FlattenedPos2D(state.pos)).normalized;
            Vector2 delta_p = this.redirectionManager.speedReal * this.stageDuration * tangentDir;
            
            // update virtual position and direction
            newState.pos = state.pos + Utilities.UnFlatten(delta_p);
            newState.dir = state.dir;

            // update the real world state according to the action
            if (a.type == ActionType.CURVATURE)
            {
                float s = delta_p.magnitude;
                //Debug.Log("s=" + s);
                float kr = a.gain; // the curvature gain rou(c)
                float ori_0 = Vector2.SignedAngle(Vector2.right, Utilities.FlattenedDir2D(state.dirReal)) * Mathf.Deg2Rad;
                //Debug.Log("angle:"+ Vector2.SignedAngle(Vector2.right, Utilities.FlattenedDir2D(state.dirReal))+" ori_0:" + ori_0);
                // The new real position depends on user's real orientation
                newState.posReal.x = (Mathf.Sin(ori_0 + kr * s) - Mathf.Sin(ori_0)) / kr + state.posReal.x;
                newState.posReal.z = (Mathf.Cos(ori_0) - Mathf.Cos(ori_0 + kr * s)) / kr + state.posReal.z;
                newState.dirReal = Utilities.UnFlatten(Utilities.RotateVector(Utilities.FlattenedDir2D(state.dirReal), s * kr*Mathf.Rad2Deg));
                //Debug.Log("ppp " + newState.posReal);
                //Debug.Log("ddd " + newState.dirReal);
            }
            else if (a.type == ActionType.ZERO)
            {
                newState.posReal = state.posReal + state.dirReal * this.redirectionManager.speedReal * this.stageDuration;
                newState.dirReal = state.dirReal;
            }
            else if (a.type == ActionType.RESET)
            {
                newState.posReal = state.posReal - state.dirReal * this.redirectionManager.speedReal * this.stageDuration;
                newState.dirReal = -state.dirReal;
            }
        }
        else if (seg is ArcSegment)
        {
            ArcSegment segment = (ArcSegment)seg;

            // update virtual position and direction
            float s = this.redirectionManager.speedReal * this.stageDuration;
            float ori_v0 = Vector2.SignedAngle(Vector2.right, Utilities.FlattenedDir2D(state.dir)) * Mathf.Deg2Rad;
            newState.pos.x = (Mathf.Sin(ori_v0 + s/segment.radius) - Mathf.Sin(ori_v0)) * segment.radius + state.pos.x;
            newState.pos.z = (Mathf.Cos(ori_v0) - Mathf.Cos(ori_v0 + s/segment.radius)) / segment.radius + state.pos.z;
            newState.dir = Utilities.UnFlatten(Utilities.RotateVector(state.dir, s / segment.radius));

            // update the real world state according to the action
            float ori_r0 = Vector2.SignedAngle(Vector2.right, Utilities.FlattenedDir2D(state.dirReal)) * Mathf.Deg2Rad;

            if (a.type == ActionType.CURVATURE)
            {
                float kr = 1/segment.radius + a.gain; // the compound curvature gain 1/r + rou(c)
                // The new real position depends on user's real orientation
                newState.posReal.x = (Mathf.Sin(ori_r0 + kr * s) - Mathf.Sin(ori_r0)) / kr + state.posReal.x;
                newState.posReal.z = (Mathf.Cos(ori_r0) - Mathf.Cos(ori_r0 + kr * s)) / kr + state.posReal.z;
                newState.dirReal = Utilities.UnFlatten(Utilities.RotateVector(state.dirReal, s * kr));
            }
            else if (a.type == ActionType.ZERO)
            {
                newState.posReal.x = (Mathf.Sin(ori_r0 + s / segment.radius) - Mathf.Sin(ori_r0)) * segment.radius + state.posReal.x;
                newState.posReal.z = (Mathf.Cos(ori_r0) - Mathf.Cos(ori_r0 + s / segment.radius)) / segment.radius + state.posReal.z;
                newState.dirReal = Utilities.UnFlatten(Utilities.RotateVector(state.dirReal, s / segment.radius));
            }
            else if (a.type == ActionType.RESET)
            {
                ori_r0 = Vector2.SignedAngle(Vector2.right, Utilities.FlattenedDir2D(-state.dirReal)) * Mathf.Deg2Rad;
                newState.posReal.x = (Mathf.Sin(ori_r0 + s / segment.radius) - Mathf.Sin(ori_r0)) * segment.radius + state.posReal.x;
                newState.posReal.z = (Mathf.Cos(ori_r0) - Mathf.Cos(ori_r0 + s / segment.radius)) / segment.radius + state.posReal.z;
                newState.dirReal = Utilities.UnFlatten(Utilities.RotateVector(-state.dirReal, s / segment.radius));
            }

        }
        else if (seg is RotationSegment)
        {
            RotationSegment segment = (RotationSegment)seg;

            // update virtual position and direction
            newState.pos = state.pos;
            float rotatedAngle = this.redirectionManager.angularSpeedReal * this.stageDuration * Mathf.Sign(segment.angle);
            newState.dir = Utilities.RotateVector(state.dir, rotatedAngle);

            if (a.type == ActionType.ROTATION)
            {
                newState.posReal = state.posReal;
                rotatedAngle = rotatedAngle * a.gain;
                newState.dirReal = Utilities.RotateVector(state.dirReal, rotatedAngle);
            }
            else if (a.type == ActionType.ZERO)
            {
                newState.posReal = state.posReal;
                newState.dirReal = Utilities.RotateVector(state.dirReal, rotatedAngle);
            }
        }

        return newState;
    }

    /// <summary>
    /// Compute the cost associated with a given state, currently only TowardWallCost is used.
    /// </summary>
    /// <param name="state"></param>
    /// <returns>a scalar cost value</returns>
    public float GetStateCost(RedirectionManager.State state)
    {
        float cost = TowardWallCost(state);
        //Debug.LogError("!!!!!!! cost:"+cost+", pos:" + state.posReal + ", dir:" + state.dirReal);
        return cost;
    }

    /// <summary>
    /// The cost is infinite if user is outside the tracked space
    /// When inside, the cost is inverse proportional to the longest walking distance described in the FORCE paper.
    /// </summary>
    /// <param name="state"></param>
    /// <returns>a scalar cost value</returns>
    private float TowardWallCost(RedirectionManager.State state)
    {
        // outside the tracking space
        if (Mathf.Abs(state.posReal.x) >= this.redirectionManager.resetter.maxX ||
            Mathf.Abs(state.posReal.z) >= this.redirectionManager.resetter.maxZ)
        {
            return Mathf.Infinity;
        }
        else
        {
            Vector2 interPoint = Vector2.zero;
            // check the intersection of a ray casting from user's current pos/ori with the walls of the trackedspace
            for (int i = 0; i < this.redirectionManager.roomCorners.Length; i++)
            {
                interPoint = Utilities.GetIntersection(Utilities.FlattenedPos2D(state.posReal), 
                    Utilities.FlattenedDir2D(state.dirReal), this.redirectionManager.roomCorners[i%4],
                    this.redirectionManager.roomCorners[(i + 1)%4]);
                
                // if we find the intersection point
                if (!interPoint.Equals(Vector2.zero))
                {
                    break;
                }
            }
            if (!interPoint.Equals(Vector2.zero))
            {
                float costLimit = 0.1f; // the max cost will be 100 if distance=0, this is to avoid 0-division 
                float distance = Vector2.Distance(Utilities.FlattenedPos2D(state.posReal), interPoint);
                //Debug.Log("pos: " + state.posReal + ", the interpoint is: " + interPoint
                //    +", distance: "+distance + ", cost:"+ 10 / (distance + costLimit));
                return 10/(distance + costLimit);
            }
            else
            {
                Debug.LogError("user pos: " + state.posReal+" dir: "+state.dirReal+" Error computing intersection point");
                return Mathf.Infinity;
            }
        }
    }

    /// <summary>
    /// The main MPC K-stage forward planning, this is basically depth-first tree search with branch cutting.
    /// </summary>
    /// <param name="state"></param> current state
    /// <param name="currSeg"></param> current segment of the user
    /// <param name="depth"></param> the planning depth
    /// <returns></returns>
    public MPCResult Plan(RedirectionManager.State state, Segment currSeg, int depth) {
        //Debug.Log("Plan called");
        if (depth == 0)
            return new MPCResult { bestCost = 0, bestAction = { } };
        else
        {
            float bestCost = Mathf.Infinity;
            Action bestAction = GetZeroAction();
            //Debug.Log("Plan depth="+depth+" pos="+state.posReal);
            float cost;
            List<Action> allowedActions = GetAllowedActions(currSeg);
            //Debug.Log("GetAllowedActions");
            foreach (Action a in allowedActions)
            {
                //Debug.Log("depth=" + depth + ", action=" + a.type+", gain="+a.gain);
                cost = 0f;
                if (a.cost < bestCost)
                {
                    cost += a.cost;
                    List<Segment> nextSegments = currSeg.GetNextSegments(state);
                    //Debug.Log("GetNextSegments");
                    foreach (var nextSeg in nextSegments)
                    {
                        //Debug.Log("?????????? " + state.dirReal);
                        RedirectionManager.State nextState = ApplyStateUpdate(state, a, nextSeg);
                        //Debug.Log("pos="+state.posReal+" next pos="+nextState.posReal);
                        float stateCost = GetStateCost(nextState);
                        cost += stateCost * nextSeg.proba;
                        //Debug.Log("summed cost= " + cost + ", cost of next state: " + stateCost);
                        if(cost >= bestCost)
                        {
                            //Debug.Log("CUTOFF cost=" + cost + ", bestcost=" + bestCost);
                            break;
                        }
                        if(depth > 0)
                        {
                            MPCResult nextResult = Plan(nextState, nextSeg, depth - 1);
                            cost += declineFactor * nextSeg.proba * nextResult.bestCost;
                            
                            //Debug.Log("Depth=" + depth + ", action: " + a.type + " gain: "+a.gain+
                            //        ", final cost=" + cost + ",  next bestcost=" + nextResult.bestCost);
                            
                        }
                    }
                    if(cost < bestCost)
                    {
                        //Debug.Log("update best cost, new best=" + cost + ", old bestcost=" + bestCost+", action: "
                        //    +bestAction.type+", gain="+bestAction.gain);
                        bestCost = cost;
                        bestAction = a;
                    }
                }
                else
                {
                    //Debug.Log("Depth=" + depth + " action " + a.type+ ":"+a.gain + " cutoff with cost " + a.cost);
                }
            }
            //if (depth==4)
            //{
            //    Debug.Log("Depth=" + depth + " final best cost: " + bestCost + ", best action: " + bestAction.type
            //    + ", gain: " + bestAction.gain + " at pos=" + state.posReal + " dir=" + state.dirReal);
            //}
            
            return new MPCResult { bestAction=bestAction, bestCost=bestCost};
        }
    }

    /// <summary>
    /// This overrides the same function in parent class. 
    /// </summary>
    public override void ApplyRedirection()
    {
        // Get Required Data
        Vector3 deltaPos = redirectionManager.deltaPos;
        float deltaDir = redirectionManager.deltaDir;

        
        if (deltaPos.magnitude / redirectionManager.GetDeltaTime() > MOVEMENT_THRESHOLD) // User is moving
        {
            if (this.currAction.type == ActionType.CURVATURE)
            {
                InjectCurvature(deltaPos.magnitude * this.currAction.gain * Mathf.Rad2Deg);
                //Debug.LogError("pppppppppp " + this.currAction.gain);
            }   
        }

        if (Mathf.Abs(deltaDir) / redirectionManager.GetDeltaTime() >= ROTATION_THRESHOLD)  // if User is rotating
        {
            if (this.currAction.type == ActionType.ROTATION)
            {
                InjectRotation(deltaDir * (1-this.currAction.gain));
            }
        }
            
    }
}

/// <summary>
/// This is a primitive of user's current action, e.g. moving in a line, an arc or rotating in place.
/// We assume that user will keep the same motion until the current segment is finished.
/// </summary>
public abstract class Segment
{
    public int id; // id starts from 1
    public float proba; // this probability is computed according to the segment tree
    public List<Transition> transitions; // each segment will have one or more transitions that will lead to other segments

    // the threshold to activate the termination of current segment
    public const float DISTANCE_THRESHOLD = 0.3f; // meter
    public const float ANGLE_THRESHOLD = 5f; // degree

    public abstract List<Segment> GetNextSegments(RedirectionManager.State state);

    public abstract bool IsEndOfSegment(RedirectionManager.State currState);

    /// <summary>
    /// Add this segment as the starting segment for a given transition
    /// </summary>
    /// <param name="t"></param>
    public void AddTransition(Transition t)
    {
        if(t.startSeg.Equals(this))
            this.transitions.Add(t);
    }
}

/// <summary>
/// The user is moving in a straight line
/// </summary>
public class LineSegment : Segment
{
    public Vector2 startPos;
    public Vector2 endPos;

    public LineSegment(int id, Vector2 sp, Vector2 ep)
    {
        this.id = id;
        this.startPos = sp;
        this.endPos = ep;
    }

    public override List<Segment> GetNextSegments(RedirectionManager.State state)
    {
        List<Segment> segs = new List<Segment>();
        
        if (this.transitions == null) // this segment has no further transition
        {
            this.proba = 1;
            segs.Add(this);
            return segs;
        }
        else if (IsEndOfSegment(state))
        {
            int num = this.transitions.Count;
            foreach (var transition in this.transitions)
            {
                transition.endSeg.proba = 1.0f / num;
                segs.Add(transition.endSeg);
            }
            return segs;
        }
        else
        {
            this.proba = 1;
            segs.Add(this);
            return segs;
        }
    }

    public override bool IsEndOfSegment(RedirectionManager.State currState)
    {
        return Vector2.Distance(this.endPos, Utilities.FlattenedPos2D(currState.pos)) <= Segment.DISTANCE_THRESHOLD;
    }
}

/// <summary>
/// The user is moving along an arc, we know everything about the arc.
/// </summary>
public class ArcSegment : Segment
{
    public Vector2 startPos;
    public Vector2 endPos;
    public Vector2 startDir;
    public Vector2 endDir;
    public Vector2 oriPos; // center of the arc
    public float radius;

    public ArcSegment(int id, Vector2 op, Vector2 sp, Vector2 ep, Vector2 sd, Vector2 ed)
    {
        this.id = id;
        this.oriPos = op;
        this.startPos = sp;
        this.endPos = ep;
        this.startDir = sd;
        this.endDir = ed;
        this.radius = Vector2.Distance(startPos, oriPos);
    }

    public override List<Segment> GetNextSegments(RedirectionManager.State state)
    {
        List<Segment> segs = new List<Segment>();
        if (IsEndOfSegment(state)) // near the end position
        {
            foreach (var transition in this.transitions)
            {
                segs.Add(transition.endSeg);
            }
            return segs;
        }
        else
        {
            segs.Add(this);
            return segs;
        }
    }

    public override bool IsEndOfSegment(RedirectionManager.State currState)
    {
        return Vector2.Distance(this.endPos, Utilities.FlattenedPos2D(currState.pos)) <= Segment.DISTANCE_THRESHOLD;
    }
}

/// <summary>
/// The user rotates in place (i.e. without moving).
/// </summary>
public class RotationSegment : Segment
{
    public Vector2 pos;
    public Vector2 startDir;
    public Vector2 endDir;
    public float angle; // the signed angle in degree

    public RotationSegment(int id, Vector2 p, Vector2 sd, Vector2 ed)
    {
        this.id = id;
        this.pos = p;
        this.startDir = sd;
        this.endDir = ed;
        this.angle = Vector2.SignedAngle(startDir, endDir);
    }

    public override List<Segment> GetNextSegments(RedirectionManager.State state)
    {
        List<Segment> segs = new List<Segment>();
        if (IsEndOfSegment(state))
        {
            foreach (var transition in this.transitions)
            {
                segs.Add(transition.endSeg);
            }
            return segs;
        }
        else
        {
            segs.Add(this);
            return segs;
        }
    }

    public override bool IsEndOfSegment(RedirectionManager.State currState)
    {
        // near the end direction
        return Vector2.Angle(this.endDir, Utilities.FlattenedPos2D(currState.dir)) <= Segment.ANGLE_THRESHOLD;
    }
}
/// <summary>
/// This is a transition between different user motion segments
/// </summary>
public class Transition
{
    public Segment startSeg;
    public Segment endSeg;
    public float proba;

    public Transition(Segment n1, Segment n2, float p)
    {
        this.startSeg = n1;
        this.endSeg = n2;
        this.proba = p;
    }

}

