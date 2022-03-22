using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obj : Object
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public new void SetObject(ObjectContainer container)
    {
        _container = container;
        transform.position = _container._position;
        transform.rotation = Quaternion.identity;
        transform.Rotate(new Vector3(_container._rotation[1], _container._rotation[2], _container._rotation[3]),
            _container._rotation[0]);
        transform.localScale = new Vector3(_container._scale[0], _container._scale[1], _container._scale[2]);
    }
}
