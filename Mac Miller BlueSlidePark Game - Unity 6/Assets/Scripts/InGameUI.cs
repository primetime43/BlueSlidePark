using UnityEngine;

public class InGameUI : MonoBehaviour
{
    private Animator am;

    private void Start()
    {
        am = GetComponent<Animator>();
    }

    public void CallAnimator(string param)
    {
        am.SetTrigger(param);
    }
}
