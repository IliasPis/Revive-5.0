using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeSceneToAI : MonoBehaviour
{
    public void ChangeScene()
    {
        SceneManager.LoadScene("GameSceneAI");
    }
}


