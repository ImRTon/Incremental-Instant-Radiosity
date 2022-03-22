using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Plane : Object
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
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, 90));
        transform.Rotate(new Vector3(_container._rotation[1], _container._rotation[2], _container._rotation[3]),
            _container._rotation[0]);
        transform.localScale = new Vector3(_container._shapeContainer._size[0] / 10f,
            1, _container._shapeContainer._size[1] / 10f);
        SetMat();
    }
    public new void SetMat()
    {
        if (_container._material._matType == MatType.Map)
        {
            Texture2D colorMap = LoadTexImg(_container._material._colorMapFilePath);
            Texture2D bumpMap = LoadTexImg(_container._material._bumpMapFilePath);
            UnityEngine.Material mat = new UnityEngine.Material(Shader.Find("Ciconia Studio/CS_Standard/Builtin/Lite/Standard (Specular setup)/Opaque"));
            mat.SetTexture("_MainTex", colorMap);
            mat.SetTexture("_BumpMap", bumpMap);
            transform.GetComponent<MeshRenderer>().material = mat;
        }
        else if (_container._material._matType == MatType.Normal)
        {
            UnityEngine.Material mat = new UnityEngine.Material(Shader.Find("Ciconia Studio/CS_Standard/Builtin/Lite/Standard (Specular setup)/Opaque"));
            mat.SetColor("_Color", _container._material._colorKd);
            mat.SetColor("_SpecColor", _container._material._colorKs);
            transform.GetComponent<MeshRenderer>().material = mat;
        }
        else if (_container._material._matType == MatType.Mirror)
        {
            UnityEngine.Material mat = new UnityEngine.Material(Shader.Find("Ciconia Studio/CS_Standard/Builtin/Lite/Standard (Specular setup)/Opaque"));
            mat.SetColor("_Color", Color.white);
            mat.SetColor("_SpecColor", Color.white);
            mat.SetFloat("_Smoothness", 1f);
            transform.GetComponent<MeshRenderer>().material = mat;
        }
    }

    private Texture2D LoadTexImg(string filePath)
    {
        Texture2D texture = null;
        byte[] fileData;
        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath);
            texture = new Texture2D(10, 10);
            texture.LoadImage(fileData);
        }
        else
            texture = Texture2D.whiteTexture;
        return texture;
    }
}
