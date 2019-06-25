using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotionManager : MonoBehaviour
{
    [Tooltip("How user movement is controlled.")]
    public MovementController MOVEMENT_CONTROLLER = MovementController.Tracker;

    public enum MovementController { Keyboard, AutoPilot, Tracker };

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
