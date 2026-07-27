using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public enum GameStateFishing { Playing, Won, Lost }

/// <summary>
/// Tracks win/lose state for the fishing minigame.
/// Attach to an empty "GameManager" GameObject.
/// </summary>
public class FishingGameManager : MonoBehaviour
{
    public static FishingGameManager Instance { get; private set; }

    [Header("Core References")]
    public PlayAreaBounds playArea;
    public Transform rod;
    public Transform fish;

    [Header("Win Condition")]
    [Tooltip("How close (world units) the fish must get to the rod to count as caught.")]
    public float catchDistance = 0.75f;

    [Header("UI (optional)")]
    [Tooltip("Assign a UI Text (or swap for TMP_Text) to show status messages. Safe to leave empty.")]
    public Text statusText;

    public GameStateFishing State { get; private set; } = GameStateFishing.Playing;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (State != GameStateFishing.Playing)
        {
            var kb = Keyboard.current;
            if (kb.rKey.isPressed)
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        if (fish != null && rod != null)
        {
            float dist = Vector2.Distance(fish.position, rod.position);
            if (dist <= catchDistance)
                Win();
        }
    }

    /// <summary>Called by FishController whenever the fish grazes a piece of debris.</summary>
    public void RegisterHit()
    {
        // Hook point for screen shake, sound effects, camera flash, etc.
    }

    public void Lose(string reason)
    {
        if (State != GameStateFishing.Playing) return;
        State = GameStateFishing.Lost;
        SetStatus($"Line snapped! {reason}\nPress R to retry.");
    }

    private void Win()
    {
        if (State != GameStateFishing.Playing) return;
        State = GameStateFishing.Won;
        SetStatus("Fish caught! Press R to play again.");
    }

    private void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
        Debug.Log(msg);
    }
}
