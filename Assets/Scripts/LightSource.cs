using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightSource : MonoBehaviour
{
    public Light _lightSource;
    public int _nSamples = 1;
    public Vector2 _area = Vector2.one;
    public LightType _lightType;
    public List<VPL> VPLs = new List<VPL>();
    public GameObject _spotOb;
    // Start is called before the first frame update
    private void Awake()
    {
        _lightSource = GetComponent<Light>();

    }
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

public enum LightType
{
    Point, Area, Spot
}
