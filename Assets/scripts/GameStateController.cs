
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class GameStateController : UdonSharpBehaviour
{
    public Transform ballsParent;
    public DropController dropControllerScript;


    void Start()
    {

    }

    private void OnTriggerStay(Collider other)
    {
        
        // If collision is in ball layer
        if(other.gameObject.layer == 24 && other.gameObject.GetComponent<BallBehavior>().ballInPlay)
        {
            Debug.Log("fail condition met");
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "FailGame");
        }
    }

    public void ResetGame()
    {
        Debug.Log("Reset game");
        foreach (Transform child in ballsParent)
        {
            Destroy(child.gameObject);
        }
        dropControllerScript.ResetDropper();
    }

    public void FailGame()
    {
        dropControllerScript.isGameActive = false;

        foreach (Transform child in ballsParent)
        {
            child.gameObject.GetComponent<Rigidbody>().isKinematic = true;
        }
    }
}
