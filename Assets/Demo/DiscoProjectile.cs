using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiscoProjectile : MonoBehaviour
{
    void OnCollisionEnter2D(Collision2D col)
    {
        GetComponent<sdBox>().color = Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f);
    }
}
