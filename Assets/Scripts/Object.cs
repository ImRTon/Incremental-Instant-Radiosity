using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Object : MonoBehaviour
{
    public ObjectContainer _container;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetObject(ObjectContainer container)
    {
        _container = container;
    }

    public void SetMat()
    {
        if (_container._material._matType == MatType.Map)
        {
            Texture2D colorMap = LoadTexImg(_container._material._colorMapFilePath);
            Texture2D bumpMap = LoadTexImg(_container._material._bumpMapFilePath);
            UnityEngine.Material mat = new UnityEngine.Material(Shader.Find("Standard"));
            mat.SetTexture("_MainTex", colorMap);
            mat.SetTexture("_BumpMap", bumpMap);
            transform.GetComponent<MeshRenderer>().material = mat;
        }
        else if (_container._material._matType == MatType.Normal)
        {
            UnityEngine.Material mat = new UnityEngine.Material(Shader.Find("Standard (Specular setup)"));
            mat.SetColor("_Color", _container._material._colorKd);
            mat.SetColor("_SpecColor", _container._material._colorKs);
            transform.GetComponent<MeshRenderer>().material = mat;
        }
        else if (_container._material._matType == MatType.Mirror)
        {
            UnityEngine.Material mat = new UnityEngine.Material(Shader.Find("Standard (Specular setup)"));
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


public class ObjectContainer
{
    public ObjectType _obType;
    public Vector3 _position = new Vector3(0, 0, 0);
    public Vector4 _rotation = new Vector4(0, 0, 0, 0); /* Angle x y z*/
    public Vector3 _scale = new Vector3(1, 1, 1);
    public string _filePath;
    public MaterialContainer _material = new MaterialContainer();
    public ShapeContainer _shapeContainer = new ShapeContainer();
}

public class ShapeContainer
{
    public ObShape _obShape;
    public Vector2 _size = new Vector2(0, 0);
    /*
     0 : width or radius,
     1 : height or radius
     */
    public Vector2 _yVal = new Vector2(0, 0);
}

public class MaterialContainer
{
    public MatType _matType;
    public Color _colorKs;
    public Color _colorKd;
    public string _colorMapFilePath;
    public string _bumpMapFilePath;


}

public enum ObjectType
{
    Shape, Obj, Light
}

public enum ObShape
{
    Sphere, Cylinder, Cone, Plane
}

public enum MatType
{
    Normal, Mirror, Map
}
