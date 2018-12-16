using System;
using System.Collections;
using UnityEngine;


public class zoom : MonoBehaviour
{
    public int zooming = 10;
    public int normal = 20;
    public float smooth = 5;

    private bool isZoomed = false;

    public void Update()
    {
      if(Input.GetMouseButtonDown(1))
        {
            isZoomed = !isZoomed;
        }

      if(isZoomed)
        {
            GetComponent<Camera>().orthographicSize = Mathf.Lerp(GetComponent<Camera>().orthographicSize, zooming, Time.deltaTime * smooth);
        }

      else
        {
            GetComponent<Camera>().orthographicSize = Mathf.Lerp(GetComponent<Camera>().orthographicSize, normal, Time.deltaTime * smooth);
        }
    }
}
