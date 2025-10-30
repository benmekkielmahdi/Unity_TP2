using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trash_Capsule : MonoBehaviour
{
    private GameController gameController;

    private void Awake()
    {
        gameController = FindObjectOfType<GameController>();
        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Correct tri: capsule dans poubelle capsule
        if (other.CompareTag("capsule"))
        {
            if (gameController != null)
            {
                gameController.HandleCorrect("Capsule");
            }
            Destroy(other.gameObject);
            return;
        }

        // Mauvais tri: autre forme
        if (other.CompareTag("cube") || other.CompareTag("sphere"))
        {
            if (gameController != null)
            {
                string got = other.CompareTag("cube") ? "Cube" : "Sphere";
                gameController.HandleWrong("Capsule", got);
            }
        }
    }
}
