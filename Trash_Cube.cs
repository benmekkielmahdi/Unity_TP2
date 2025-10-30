using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trash_Cube : MonoBehaviour
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
        // Correct tri: cube dans poubelle cube
        if (other.CompareTag("cube"))
        {
            if (gameController != null)
            {
                gameController.HandleCorrect("Cube");
            }
            Destroy(other.gameObject);
            return;
        }

        // Mauvais tri: autre forme
        if (other.CompareTag("capsule") || other.CompareTag("sphere"))
        {
            if (gameController != null)
            {
                string got = other.CompareTag("capsule") ? "Capsule" : "Sphere";
                gameController.HandleWrong("Cube", got);
            }
        }
    }
}
