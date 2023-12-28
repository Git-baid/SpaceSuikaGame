
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;
using VRC.Udon;
using TMPro;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class MergeController : UdonSharpBehaviour
{
    public EffectsController effectsControllerScript;
    public Transform ballParent;
    public DropController dropControllerScript;
    private GameObject[] ballGOPool;
    private GameObject combinedBall;

    [UdonSynced]
    public int firstBallIndex = -1;
    [UdonSynced]
    public int secondBallIndex = -1;
    private bool syncReady;
    private bool mergeReady;


    //debug
    public TextMeshProUGUI debugText;

    public void Start()
    {
        ballGOPool = dropControllerScript.ballGOPool;
        debugText = GameObject.Find("DebugText").GetComponent<TextMeshProUGUI>();
    }
    public void Update()
    {
        if(syncReady && mergeReady)
        {
            CombineBalls();
            syncReady = false;
            mergeReady = false;
        }
    }

    // called by owner
    public void NetworkedMerge()
    {
        RequestSerialization();
        syncReady = true;
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "SetMergeReady");
        // TODO: waiting for variables to serialize and then proceeding with merge works fine for a single merge instance
        // but during cascading merges, it is too slow to keep up and ends up missing merges for remote players...
    }

    public override void OnDeserialization()
    {
        syncReady = true;
    }

    public void SetMergeReady()
    {
        mergeReady = true;
    }


    public void CombineBalls()
    {   
        
        GameObject firstBall = ballGOPool[firstBallIndex];
        GameObject secondBall = ballGOPool[secondBallIndex];

        debugText.text = "Merging " + firstBallIndex + " and " + secondBallIndex;

        GameObject[] nextBallPool = firstBall.GetComponent<BallBehavior>().nextBallPool;

        secondBall.GetComponent<SphereCollider>().enabled = false;
        firstBall.GetComponent<SphereCollider>().enabled = false;

        Vector3 spawnPos = new Vector3((secondBall.transform.position.x + firstBall.transform.position.x) / 2, //get average position of two colliding balls
            (secondBall.transform.position.y + firstBall.transform.position.y) / 2,
            (secondBall.transform.position.z + firstBall.transform.position.z) / 2);

        for (int i = 0; i < 20; i++)
        {
            if (!nextBallPool[i].activeSelf)
            {
                combinedBall = nextBallPool[i];
                combinedBall.SetActive(true);
                combinedBall.transform.position = spawnPos;
                combinedBall.transform.parent = firstBall.transform.parent;
                combinedBall.GetComponent<VRCObjectSync>().SetKinematic(false);
                combinedBall.GetComponent<Rigidbody>().isKinematic = false;
                combinedBall.GetComponent<SphereCollider>().enabled = true;
                break;
            }
        }

        effectsControllerScript.PopFX(spawnPos);
        ResetBall(firstBall);
        ResetBall(secondBall);
    }


    void ResetBall(GameObject ball)
    {
        ball.GetComponent<VRCObjectSync>().SetKinematic(true);
        ball.GetComponent<Rigidbody>().isKinematic = true;
        ball.GetComponent<SphereCollider>().enabled = false;
        ball.transform.parent = ballParent;
        ball.transform.position = ballParent.position;
        ball.GetComponent<BallBehavior>().ballInPlay = false;

        ball.SetActive(false);
    }
}
