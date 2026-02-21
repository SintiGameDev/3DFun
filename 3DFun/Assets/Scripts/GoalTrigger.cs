using UnityEngine;

/// <summary>
/// Muss auf dem Goal-Prefab liegen (oder wird automatisch vom GameManager hinzugefügt).
/// Voraussetzung: Collider auf dem Ziel-Objekt mit „Is Trigger" = true.
/// </summary>
public class GoalTrigger : MonoBehaviour
{
    [Tooltip("Tag des Spieler-GameObjects (Standard: 'Player')")]
    public string playerTag = "Player";

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            GameManager.Instance?.OnGoalReached();
        }
    }
}