using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VPL : MonoBehaviour
{
    public Light _myLight;
    // Start is called before the first frame update
    void Start()
    {
        _myLight = GetComponent<Light>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetLightIntensity(float val)
    {
        _myLight.intensity = val;
    }

    public Vector3 GetPos()
    {
        return transform.position;
    }
}
