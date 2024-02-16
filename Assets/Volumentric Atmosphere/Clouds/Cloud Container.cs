using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CloudContainer : MonoBehaviour
{
    [HideInInspector] public Vector3 boundsMin;
    [HideInInspector] public Vector3 boundsMax;
    // public Color boxColor = Color.green;

    // void OnDrawGizmosSelected() {
    //     Gizmos.color = boxColor;
    //     Gizmos.color = Color.green;
    //     Gizmos.DrawWireCube(transform.position, transform.localScale);

    // }
    public Color colour = Color.green;
    public bool displayOutline = true;

    void OnDrawGizmosSelected () {
        if (displayOutline) {
            Gizmos.color = colour;
            Gizmos.DrawWireCube (transform.position, transform.localScale);
        }
    }
    // Update is called once per frame
    void Update()
    {
        boundsMin = transform.position - transform.localScale/2;
        boundsMax = transform.position + transform.localScale/2;
    }
}
