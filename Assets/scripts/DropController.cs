
using UdonSharp;
using UnityEngine;
using UnityEngine.Animations;
using VRC.SDK3.Components;
using VRC.SDKBase;
using TMPro;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class DropController : UdonSharpBehaviour
{
    public ScoreController scoreScript;
    public GameStateController gameStateControllerScript;
    public EffectsController effectsController;

    public Transform dropper;
    public Transform nextBallPos;
    public Collider controllerCollider;
    private RaycastHit hitData;

    private GameObject newBall;
    private GameObject nextBall;

    private bool cursorInBounds;
    [UdonSynced]
    private int nextBallID = -1;
    [UdonSynced]
    private int newBallID = -1;
    private float newBallRadius;

    private float timer;
    private bool dropping;
    public float dropDelay;
    [SerializeField] private LayerMask dropperCollisionLayer;
    [SerializeField] private LayerMask resetLayer;

    private bool resetHover;
    [UdonSynced]
    public bool isGameActive;

    public TextMeshProUGUI debugText;
    public Material resetButtonMat;
    public Color32 resetHoverTint = new Color32(212, 103, 103, 255);
    public Color32 resetUnHoverTint = new Color32(255, 177, 177, 255);

    public GameObject ballsParent;
    public int ballGOPoolSize = 20;
    public GameObject[] ballGOPool;

    void Start()
    {
        if (Networking.IsOwner(gameObject))
            InitializeGameState();
    }

    private void Update()
    {
        CastRay(); //ray from controller to aim dropper


        // delay timer in between drops
        if (dropping)
        {
            if (timer < dropDelay)
                timer += Time.deltaTime;
            else
            {
                timer = 0;

                if (Networking.LocalPlayer.IsOwner(gameObject))
                    UpdateBalls();

                dropping = false;
                effectsController.dropping = false;
            }
        }
    }

    public void InitializeGameState()
    {
        isGameActive = true;
        nextBallID = Random.Range(0, 5);
        UpdateBalls();
    }

    public void ResetDropper()
    {
        dropping = false;
        timer = 0;
        effectsController.dropping = false;
                
        nextBall.SetActive(false);
        
        newBall.SetActive(false);

        isGameActive = true;
        scoreScript.score = 0;
        if(Networking.IsMaster)
            InitializeGameState();
    }

    // updateballs() is only called by owner
    public void UpdateBalls()
    {
        newBallID = nextBallID;

        nextBallID = Random.Range(0, 0);

        RequestSerialization();

        SpawnNewBall();
        SpawnNextBall();
    }

    public override void OnDeserialization()
    {
        if (!Networking.IsOwner(gameObject))
        {
            SpawnNewBall();
            SpawnNextBall();
        }
    }

    public void SpawnNewBall()
    {
        // loop over each ball in ball's pool to find one that is
        // not already active, and set it active under dropper
        for (int i = 0; i < 20; i++)
        {
            if (!ballGOPool[newBallID * ballGOPoolSize + i].activeSelf)
            {
                newBall = ballGOPool[newBallID * ballGOPoolSize + i];
                newBall.GetComponent<BallBehavior>().ballInPlay = false;
                newBallRadius = newBall.transform.lossyScale.x/2;
                newBall.SetActive(true);
                newBall.transform.position = dropper.position - new Vector3(0, 0.2f, 0);
                newBall.transform.parent = dropper;
                newBall.GetComponent<ParentConstraint>().constraintActive = true;
                return;
            }
        }
    }

       
    public void SpawnNextBall()
    {
        // if nextBall exists, disable it
        if (nextBall)
            nextBall.SetActive(false);

        for (int i = 0; i < 20; i++)
        {
            if (!ballGOPool[nextBallID * ballGOPoolSize + i].activeSelf)
            {
                nextBall = ballGOPool[nextBallID * ballGOPoolSize + i];
                nextBall.GetComponent<BallBehavior>().ballInPlay = false;
                nextBall.SetActive(true);
                nextBall.transform.position = nextBallPos.position;
                return;
            }
        }
    }

    void CastRay()
    {
        Ray ray = new Ray(transform.position, -transform.up);
        Debug.DrawRay(ray.origin, ray.direction * 10);

        // if cursor is hovering over play collider
        if (Physics.Raycast(ray, out hitData, Mathf.Infinity, dropperCollisionLayer) && isGameActive)
        {
            float dropperXPos = Mathf.Clamp(hitData.point.x, -0.9f + newBallRadius, 0.9f - newBallRadius); //find radius of the current ball to offset the max dropper X pos values so you cant drop balls clipping the walls
            dropper.transform.position = new Vector3(dropperXPos, dropper.transform.position.y, dropper.transform.position.z);
            cursorInBounds = true;
            resetHover = false;
        }
        // if cursor is hovering over reset button
        else if (Physics.Raycast(ray, out hitData, Mathf.Infinity, resetLayer))
        {
            resetButtonMat.SetColor("_RefractionTint", resetHoverTint);
            resetHover = true;
            cursorInBounds = false;
        }
        else
        {
            resetButtonMat.SetColor("_RefractionTint", resetUnHoverTint);
            cursorInBounds = false;
            resetHover = false;
        }
    }

    // called from ControllerInteract.cs by owner
    public void NetworkedDrop()
    {
        if(!dropping && cursorInBounds && isGameActive)
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, "UseDropper");
    }

    public void UseDropper()
    {   
        // if cursor is hovering over the reset button when dropped, then reset game
        if (resetHover)
            gameStateControllerScript.ResetGame();

        else
            DropBall();
    }

    void DropBall()
    {
        scoreScript.score += newBall.GetComponent<BallBehavior>().value;

        newBall.GetComponent<ParentConstraint>().constraintActive = false;
        newBall.GetComponent<VRCObjectSync>().SetKinematic(false);
        newBall.GetComponent<SphereCollider>().enabled = true;
        newBall.transform.parent = ballsParent.transform;
        dropping = true;
        effectsController.dropping = true;
    }
}
