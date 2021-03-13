using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class InGameUI : MonoBehaviour
{

    private Animator am;
    // Start is called before the first frame update
    void Start()
    {
        am = GetComponent<Animator>();
    }

    public void CallAnimator(string param)
    {
        am.SetTrigger(param);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
