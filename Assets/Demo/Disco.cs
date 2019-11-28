using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Disco : MonoBehaviour
{
    public float duration = 3f;

    [Range(0f, 1f)]
    public float offset = 0f;

    private void Update()
    {
        GetComponent<sdBox>().color = Color.HSVToRGB(((Time.realtimeSinceStartup / duration) + offset) % 1, 1f, 1f);
    }
}
