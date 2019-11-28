using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimpleBoxdude : MonoBehaviour
{
    public float torque = 50f;

    public float shootSpeed = 3f;
    public float recoil = 30f;
    public GameObject projectile;
    public float attractorForce = 10f;
    public float attractorMaxForce = 20f;

    List<Rigidbody2D> projectiles = new List<Rigidbody2D>();

    void Start()
    {
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
            GetComponent<Rigidbody2D>().AddForce(Vector2.left * torque);
        if (Input.GetKeyDown(KeyCode.D))
            GetComponent<Rigidbody2D>().AddForce(Vector2.right * torque);

        if(Input.GetMouseButtonDown(0) || Input.GetMouseButton(2))
        {
            GameObject proj = Instantiate(projectile);
            Vector3 mpos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mpos.z = transform.position.z;
            Vector3 dir = (mpos - transform.position).normalized;
            proj.transform.position = transform.position + dir;
            proj.SetActive(true);
            proj.GetComponent<Rigidbody2D>().velocity = dir * shootSpeed;

            projectiles.Add(proj.GetComponent<Rigidbody2D>());
        }

        if(Input.GetMouseButton(1))
        {
            foreach(var p in projectiles)
            {
                Vector3 mpos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector2 diff = new Vector2(mpos.x, mpos.y) - p.position;
                p.AddForce(diff.normalized * Mathf.Max(attractorMaxForce, attractorForce * diff.magnitude));
            }
        }

        if(Input.GetKeyDown(KeyCode.W))
        {
            GetComponent<Rigidbody2D>().AddForce(Vector2.up * recoil);
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("W/A/D - move");
        GUILayout.Label("left click - shoot once");
        GUILayout.Label("middle click - spam shoot");
        GUILayout.Label("right click - attract");
    }
}
