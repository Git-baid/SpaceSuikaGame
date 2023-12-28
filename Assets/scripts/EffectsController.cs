
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class EffectsController : UdonSharpBehaviour
{
    public Transform dropperController;
    public Transform dropper;
    public LineRenderer dropperControllerLine;
    public float cursorLineLength;
    public LineRenderer dropperLine;
    public AudioSource[] popSFX;
    public ParticleSystem combineParticles;

    private RaycastHit hitData;
    public bool dropping;


    private void Update()
    {
        CastDropperLine(); //aim line below dropper
        CastControllerLine(); //aim line for controller
    }

    public void PopFX(Vector3 spawnPos)
    {
        popSFX[Random.Range(0, popSFX.Length)].Play();
        combineParticles.transform.position = spawnPos;
        combineParticles.Play();
    }

    void CastDropperLine()
    {
        if (!dropping)
        {
            dropperLine.enabled = true;
            Ray ray = new Ray(dropper.position - new Vector3(0, 0.2f, 0), -dropper.up);
            Debug.DrawRay(ray.origin, ray.direction * 10);
            if (Physics.Raycast(ray, out hitData))
            {
                dropperLine.SetPosition(0, dropper.position);
                dropperLine.SetPosition(1, hitData.point);
            }
        }
        else
        {
            dropperLine.enabled = false;
        }
    }

    void CastControllerLine()
    {
        Ray ray = new Ray(dropperController.position, -dropperController.up);
        Debug.DrawRay(ray.origin, ray.direction * 10);
        dropperControllerLine.SetPosition(0, dropperController.position);
        dropperControllerLine.SetPosition(1, dropperController.position - dropperController.up * cursorLineLength);
    }

}
