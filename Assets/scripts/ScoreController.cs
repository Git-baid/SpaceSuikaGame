
using UdonSharp;
using TMPro;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ScoreController : UdonSharpBehaviour
{
    public TextMeshProUGUI scoreText;
    [UdonSynced]
    public int score;

    private void Update()
    {
        scoreText.text = "Score: " + score.ToString();
    }
}
