using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trash_Sphere : MonoBehaviour
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
        // Correct tri: sphere dans poubelle sphere
        if (other.CompareTag("sphere"))
        {
            if (gameController != null)
            {
                gameController.HandleCorrect("Sphere");
            }
            Destroy(other.gameObject);
            return;
        }

        // Mauvais tri: autre forme
        if (other.CompareTag("cube") || other.CompareTag("capsule"))
        {
            if (gameController != null)
            {
                string got = other.CompareTag("cube") ? "Cube" : "Capsule";
                gameController.HandleWrong("Sphere", got);
            }
        }
    }
}
