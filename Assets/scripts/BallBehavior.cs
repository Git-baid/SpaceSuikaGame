
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class BallBehavior : UdonSharpBehaviour
{
    public MergeController mergeControllerScript;
    public DropController dropControllerScript;
    private GameObject[] ballGOPool;

    //combine behavior 
    public int value;
    public GameObject[] nextBallPool;



    //physics behavior
    public float maxSpeed;
    private Rigidbody rb;
    public bool ballInPlay;

    private void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        mergeControllerScript = GameObject.Find("MergeController").GetComponent<MergeController>();
        dropControllerScript = GameObject.Find("ControllerController :)").GetComponent<DropController>();
        ballGOPool = dropControllerScript.ballGOPool;

    }

    private void Update()
    {
        if (ballInPlay)
            rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxSpeed);
    }

    private void OnCollisionEnter(Collision collision)
    {
        //only if the collision is detected by owner
        if (Networking.IsOwner(gameObject))
        {
            // if ball collides with something that isnt the fail collider or the walls,
            // set ballInPlay to true (limits max velocity)
            if (!ballInPlay && collision.gameObject.name != "Fail Collider" && collision.gameObject.name != "Wall")
            {
                ballInPlay = true;
            }

            // If the layer of the collision object is a ball, it is not a galaxy ball,
            // and the value of this ball and the colliding ball is the same, then run the combine method
            if (collision.gameObject.layer == 24 && collision.gameObject.GetComponent<BallBehavior>().value != 2048 && collision.gameObject.GetComponent<BallBehavior>().value == gameObject.GetComponent<BallBehavior>().value)
            {
                // only one ball must run this,
                // arbitrarily choose which by comparing instanceIDs
                if (gameObject.GetInstanceID() < collision.gameObject.GetInstanceID())
                    return;

                //save index of ballGOPool of each ball
                for (int i = 0; i < ballGOPool.Length; i++)
                {
                    if(gameObject == ballGOPool[i])
                        mergeControllerScript.firstBallIndex = i;
                    if(collision.gameObject == ballGOPool[i])
                        mergeControllerScript.secondBallIndex = i;
                }

                mergeControllerScript.NetworkedMerge();
            }
        }
    }
}
