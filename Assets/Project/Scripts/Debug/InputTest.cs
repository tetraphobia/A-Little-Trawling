using UnityEngine;
using LittleTrawling.Core;

public class InputTest: MonoBehaviour
{
    void Start()
    {
        GameManager.Instance.StateChanged += s => Debug.Log($"state changed to: {s}");
        InputReader.Instance.CastPressed  += () => Debug.Log("cast pressed");
        InputReader.Instance.InteractPressed += () => Debug.Log("interact pressed");
    }
    void Update()
    {
        if (InputReader.Instance.MoveInput.sqrMagnitude > 0.01f)
            Debug.Log($"Move {InputReader.Instance.MoveInput}");
    }
}