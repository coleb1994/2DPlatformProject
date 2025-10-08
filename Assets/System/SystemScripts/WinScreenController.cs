using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;


public class WinScreenController : MonoBehaviour
{
    public Animator animator;
    public float noInputTime = 5f;
    public float endAnimTime = 2f;
    private bool startPressed = false;
    private float startTime = 0;

    void Start()
    {
        startTime = Time.time;
    }

    void Update()
    {
        if (!startPressed && Input.anyKey && (Time.time - startTime) > noInputTime)
        {
            StartCoroutine(EndameEnumerator());
        }
    }

    private IEnumerator EndameEnumerator()
    {
        if (animator != null) animator.SetTrigger("begin");
        yield return new WaitForSeconds(endAnimTime);
        Application.Quit();
    }
}
