
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ControllerInteract : UdonSharpBehaviour
{
    public DropController dropControllerScript;
    public GameObject dropControllerGO;
    private GameObject[] ballGOPool;

    public void Start()
    {
        ballGOPool = dropControllerScript.ballGOPool;
    }

    public override void OnPickup()
    {
        foreach (GameObject ball in ballGOPool)
        {
            Networking.SetOwner(Networking.LocalPlayer, ball);
        }
        Networking.SetOwner(Networking.LocalPlayer, dropControllerGO);
    }

    public override void OnPickupUseDown()
    {
        dropControllerScript.NetworkedDrop();
    }
}
