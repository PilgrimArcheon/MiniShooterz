using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class SetUpManager : MonoBehaviour
{
    public static SetUpManager Instance { get; set; }

    void Awake() { Instance = this; }

    [Header("Gameplay Assets")]
    public GameObject playerPrefab;
    public GameObject aiPrefab;
    public GameObject occupier;

    public Transform[] teamOneSpawnPoints;  // Red Team Spawn Points
    public Transform[] teamTwoSpawnPoints;  // Blue Team Spawn Points
    public Material[] teamMats;
    public Material[] teamIDMats;

    public TMP_Text[] scoreBoard;
    public TMP_Text gameXp;
    public TMP_Text gameKills;
    public TMP_Text timerText;


    [Header("GameEnd Settings")]
    public GameObject GameUIParent;
    public Image winnerStatus;
    public TMP_Text winnerText;
    public TMP_Text endScoreBoard;

    [Header("Results Settings")]
    public GameObject ResultGameScreen;
    public Animator characterAnimator;
    public TMP_Text resultText;
    public TMP_Text finalXp;
    public TMP_Text finalKills;

    [Header("Item Spawning")]
    public ItemData[] itemsToSpawn; // array of items to spawn
    public List<Transform> spawnPoints; // List of spawn points
}