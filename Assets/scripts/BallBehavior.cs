
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class BallBehavior : UdonSharpBehaviour
{
    public EffectsController effectsControllerScript;
    public MergeController mergeControllerScript;
    public DropController dropControllerScript;
    private GameObject[] ballGOPool;

    //combine behavior 
    public int value;
    public GameObject[] nextBallPool;

    [UdonSynced]
    public int firstBallID = -1;
    [UdonSynced]
    public int secondBallID = -1;
    [UdonSynced]
    public int combinedBallID = -1;

    //physics behavior
    public float maxSpeed;
    private Rigidbody rb;
    public bool ballInPlay;

    [Tooltip("The parent object pool in the heirarchy that the ball should return under when disabled")]
    public Transform ballParent;
    private GameObject combinedBall;
    private Transform spawnTransform;

    public bool readyToMerge;

    //debug
    public TextMeshProUGUI debugText;

    private void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        mergeControllerScript = GameObject.Find("MergeController").GetComponent<MergeController>();
        dropControllerScript = GameObject.Find("ControllerController :)").GetComponent<DropController>();
        effectsControllerScript = GameObject.Find("Fx").GetComponent<EffectsController>();

        ballGOPool = dropControllerScript.ballGOPool;
        debugText = GameObject.Find("DebugText").GetComponent<TextMeshProUGUI>();
        spawnTransform = GameObject.Find("SpawnTransform").transform;
    }

    private void OnEnable()
    {
        combinedBallID = -1;
        firstBallID = -1;
        secondBallID = -1;
    }

    private void Update()
    {
        if (ballInPlay)
            rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxSpeed);
    }

    public void MergeReady()
    {
        readyToMerge = true;
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
                // only one ball must continue script,
                // arbitrarily choose which by comparing instanceIDs
                if (gameObject.GetInstanceID() < collision.gameObject.GetInstanceID())
                    return;

                //save index of ballGOPool of each ball
                for (int i = 0; i < ballGOPool.Length; i++)
                {
                    if (gameObject == ballGOPool[i])
                        firstBallID = i;
                    if (collision.gameObject == ballGOPool[i])
                        secondBallID = i;
                }

                // find next ball in objectpool that is disabled and use that as the new combined ball
                for (int i = 0; i < nextBallPool.Length; i++)
                    if (!nextBallPool[i].activeSelf)
                    {
                        combinedBallID = i;
                        break;
                    }

                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "MergeReady");
                MergeBallsOwner();
            }
        }
    }


    public void MergeBallsOwner()
    {
        debugText.text = firstBallID + ", " + secondBallID + ", " + combinedBallID;
        GameObject firstBall = ballGOPool[firstBallID];
        GameObject secondBall = ballGOPool[secondBallID];

        secondBall.GetComponent<SphereCollider>().enabled = false;
        firstBall.GetComponent<SphereCollider>().enabled = false;
        firstBall.GetComponent<VRCObjectSync>().SetKinematic(false);
        firstBall.GetComponent<Rigidbody>().isKinematic = false;
        secondBall.GetComponent<VRCObjectSync>().SetKinematic(false);
        secondBall.GetComponent<Rigidbody>().isKinematic = false;

        //get average position of two colliding balls
        Vector3 spawnPos = new Vector3((secondBall.transform.position.x + firstBall.transform.position.x) / 2,
            (secondBall.transform.position.y + firstBall.transform.position.y) / 2,
            (secondBall.transform.position.z + firstBall.transform.position.z) / 2);

        //get rotation of first ball
        Quaternion spawnRot = firstBall.transform.rotation;

        spawnTransform.position = spawnPos;
        spawnTransform.rotation = spawnRot;

        //teleport new ball from object pool
        combinedBall = nextBallPool[combinedBallID];
        combinedBall.SetActive(true);
        combinedBall.GetComponent<VRCObjectSync>().FlagDiscontinuity();
        combinedBall.GetComponent<VRCObjectSync>().TeleportTo(spawnTransform);
        combinedBall.transform.parent = firstBall.transform.parent;
        combinedBall.GetComponent<VRCObjectSync>().SetKinematic(false);
        combinedBall.GetComponent<Rigidbody>().isKinematic = false;
        combinedBall.GetComponent<SphereCollider>().enabled = true;

        effectsControllerScript.PopFX(spawnPos);

        // Reset first ball
        ResetBall(firstBall, false);

        // Reset second ball
        ResetBall(secondBall, true);
        SendCustomEventDelayedSeconds("ResetBall2", 3);
    }

    public override void OnDeserialization()
    {
        if (readyToMerge && combinedBallID != -1)
        {
            readyToMerge = false;
            debugText.text = firstBallID + ", " + secondBallID + ", " + combinedBallID;
            GameObject firstBall = ballGOPool[firstBallID];
            GameObject secondBall = ballGOPool[secondBallID];

            secondBall.GetComponent<SphereCollider>().enabled = false;
            firstBall.GetComponent<SphereCollider>().enabled = false;

            combinedBall = nextBallPool[combinedBallID];
            combinedBall.SetActive(true);
            combinedBall.transform.parent = firstBall.transform.parent;
            combinedBall.GetComponent<VRCObjectSync>().SetKinematic(false);
            combinedBall.GetComponent<Rigidbody>().isKinematic = false;
            combinedBall.GetComponent<SphereCollider>().enabled = true;

            effectsControllerScript.PopFX(combinedBall.transform.position);
            ResetBall(secondBall, true);
            ResetBall(firstBall, true);
        }
    }
    
    public void ResetBall(GameObject ball, bool inactiveInHeirarchy)
    {
        ball.GetComponent<VRCObjectSync>().SetKinematic(true);
        ball.GetComponent<Rigidbody>().isKinematic = true;
        ball.GetComponent<SphereCollider>().enabled = false;
        ball.transform.parent = ballParent;

        if (Networking.LocalPlayer.IsOwner(gameObject))
        {
            ball.GetComponent<VRCObjectSync>().FlagDiscontinuity();
            ball.GetComponent<VRCObjectSync>().Respawn();
        }

        ball.GetComponent<BallBehavior>().ballInPlay = false;

        if(inactiveInHeirarchy)
            ball.SetActive(false);
        else
            ball.GetComponent<MeshRenderer>().enabled = false;
    }

    public void ResetBall2()
    {
        gameObject.GetComponent<MeshRenderer>().enabled = true;
        gameObject.SetActive(false);
    }
}
