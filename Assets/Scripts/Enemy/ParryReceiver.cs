using System.Collections;
using UnityEngine;

public class ParryReceiver : MonoBehaviour
{
    public bool IsParryable { get; private set; }

    public void OpenWindow(float duration) => StartCoroutine(WindowRoutine(duration));

    IEnumerator WindowRoutine(float duration)
    {
        IsParryable = true;
        yield return new WaitForSeconds(duration);
        IsParryable = false;
    }
}
