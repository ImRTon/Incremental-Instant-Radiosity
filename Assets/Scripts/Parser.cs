using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Dummiesman;

public class Parser : MonoBehaviour
{
    public int _pixelSamples = 100;

    public int _AOSamples = 10;
    public int _resolutionX = 1600;
    public int _resolutionY = 900;

    public bool _isParseDone = false;


    public Camera _camera;
    public RawImage _canvas;

    public string _rootFolder = "C://";

    /* Prefabs */
    public GameObject LightSource;
    public GameObject Cylinder;
    public GameObject Sphere;
    public GameObject Cone;
    public GameObject Plane;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Parse(string filePath)
    {
        RayTraceUtils._objects.Clear();
        _rootFolder = Directory.GetParent(filePath).FullName;
        StreamReader reader = new StreamReader(filePath);
        string line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.Replace("\t", "");
            List<string> words = SplitStrWithSpc(line);

            if (words.Count <= 0)
                continue;

            // ignore comment
            if (words[0].Length <= 0)
                continue;
            else if (words[0][0] == '#')
                continue;

            for (int i = 0; i < words.Count; i++)
            {
                // Debug.Log(words[i]);
                switch (words[i])
                {
                    /* Camera Setting*/
                    case "integer xresolution":
                        _resolutionX = int.Parse(words[i + 1]);
                        i++;
                        break;

                    case "integer yresolution":
                        _resolutionY = int.Parse(words[i + 1]);
                        i++;
                        break;

                    case "Sampler":
                        _pixelSamples = GetIntAfter(line, words[i]);
                        break;

                    case "LookAt":
                        _camera.transform.position = new Vector3(float.Parse(words[i + 1]), float.Parse(words[i + 2]), float.Parse(words[i + 3]));
                        _camera.transform.LookAt(new Vector3(float.Parse(words[i + 4]), float.Parse(words[i + 5]), float.Parse(words[i + 6])),
                            new Vector3(float.Parse(words[i + 7]), float.Parse(words[i + 8]), float.Parse(words[i + 9])));
                        i += 9;
                        break;

                    case "Camera":
                        for (int j = i + 1; j < words.Count; j++)
                        {
                            switch (words[j])
                            {
                                case "perspective":
                                    _camera.orthographic = false;
                                    break;

                                case "orthographic":
                                    _camera.orthographic = true;
                                    break;

                                case "float fov":
                                    _camera.fieldOfView = float.Parse(words[j + 1]);
                                    j++;
                                    break;
                            }
                            i = j;
                        }
                        break;

                    /* Scene Setting */
                    case "WorldBegin":
                        bool isSceneSettingEnd = false;
                        while ((line = reader.ReadLine()) != null)
                        {
                            line = line.Replace("\t", "");
                            List<string> worldWords = SplitStrWithSpc(line);

                            if (worldWords.Count <= 0)
                                continue;

                            // ignore comment
                            if (worldWords[0].Length <= 0)
                                continue;
                            else if (worldWords[0][0] == '#')
                                continue;

                            for (int j = 0; j < worldWords.Count; j++)
                            {
                                switch(worldWords[j])
                                {
                                    case "WorldEnd":
                                        isSceneSettingEnd = true;
                                        break;
                                    case "AttributeBegin":
                                        /* Object Setting */
                                        bool isObSettingEnd = false;

                                        GameObject ob = null;
                                        ObjectContainer obCont = new ObjectContainer();
                                        while ((line = reader.ReadLine()) != null)
                                        {
                                            line = line.Replace("\t", "");
                                            List<string> obWords = SplitStrWithSpc(line);
                                            if (obWords.Count <= 0)
                                                continue;

                                            // ignore comment
                                            if (obWords[0].Length <= 0)
                                                continue;
                                            else if (obWords[0][0] == '#')
                                                continue;

                                            for (int k = 0; k < obWords.Count; k++)
                                            {
                                                int oriK = k;
                                                switch (obWords[k])
                                                {
                                                    case "AttributeEnd":
                                                        isObSettingEnd = true;
                                                        _isParseDone = true;
                                                        break;
                                                    case "LightSource":
                                                        obCont._obType = ObjectType.Light;
                                                        ob = Instantiate(LightSource);
                                                        LightSource obLight = ob.GetComponent<LightSource>();
                                                        oriK = k;
                                                        for (int l = oriK + 1; l < obWords.Count; l++)
                                                        {
                                                            switch (obWords[l])
                                                            {
                                                                case "point":
                                                                    obLight._lightSource.type = UnityEngine.LightType.Point;
                                                                    obLight._lightType = LightType.Point;
                                                                    break;

                                                                case "area":
                                                                    obLight._lightSource.type = UnityEngine.LightType.Rectangle;
                                                                    obLight._lightType = LightType.Area;
                                                                    break;

                                                                case "color L":
                                                                    List<float> colors = GetNums(obWords[l + 1]);
                                                                    obLight._lightSource.color = new Color(
                                                                        colors[0] / 15f, colors[1] / 15f, colors[2] / 15f);
                                                                    l++;
                                                                    break;

                                                                case "point from":
                                                                    List<float> posVecs = GetNums(obWords[l + 1]);
                                                                    ob.transform.position = new Vector3(posVecs[0], posVecs[1], posVecs[2]);
                                                                    l++;
                                                                    break;

                                                                case "integer nsamples":
                                                                    obLight._nSamples = int.Parse(obWords[l + 1]);
                                                                    l++;
                                                                    break;

                                                                case "float width":
                                                                    obLight._area[0] = float.Parse(obWords[l + 1]);
                                                                    //obLight._lightSource.areaSize = obLight._area;
                                                                    l++;
                                                                    break;

                                                                case "float height":
                                                                    obLight._area[1] = float.Parse(obWords[l + 1]);
                                                                    //obLight._lightSource.areaSize = obLight._area;
                                                                    l++;
                                                                    break;
                                                                case "rotate":
                                                                    List<float> rotVecs = GetNums(obWords[l + 1]);
                                                                    ob.transform.Rotate(new Vector3(rotVecs[1], rotVecs[2], rotVecs[3]), rotVecs[0]);
                                                                    l++;
                                                                    break;
                                                            }
                                                            k = l;
                                                        }
                                                        RayTraceUtils._lightObjects.Add(ob.GetHashCode(), obLight);
                                                        
                                                        break;

                                                    case "Translate":
                                                        obCont._position = new Vector3(float.Parse(obWords[k + 1]), float.Parse(obWords[k + 2]), float.Parse(obWords[k + 3]));
                                                        k += 3;
                                                        break;

                                                    case "Rotate":
                                                        obCont._rotation = new Vector4(float.Parse(obWords[k + 1]), float.Parse(obWords[k + 2]), float.Parse(obWords[k + 3]), float.Parse(obWords[k + 4]));
                                                        k += 4;
                                                        break;

                                                    case "Material":
                                                        oriK = k;
                                                        for (int l = oriK + 1; l < obWords.Count; l++)
                                                        {
                                                            switch (obWords[l])
                                                            {
                                                                case "color Kd":
                                                                    List<float> colorsKd = GetNums(obWords[l + 1]);
                                                                    obCont._material._colorKd = new Color(colorsKd[0], colorsKd[1], colorsKd[2]);
                                                                    obCont._material._matType = MatType.Normal;
                                                                    l++;
                                                                    break;

                                                                case "color Ks":
                                                                    List<float> colorsKs = GetNums(obWords[l + 1]);
                                                                    obCont._material._colorKs = new Color(colorsKs[0], colorsKs[1], colorsKs[2]);
                                                                    obCont._material._matType = MatType.Normal;
                                                                    l++;
                                                                    break;

                                                                case "mirror":
                                                                    obCont._material._matType = MatType.Mirror;
                                                                    break;

                                                                case "color map":
                                                                    obCont._material._matType = MatType.Map;
                                                                    obCont._material._colorMapFilePath = Path.Combine(_rootFolder, obWords[l + 1]);
                                                                    l++;
                                                                    break;

                                                                case "bump map":
                                                                    obCont._material._matType = MatType.Map;
                                                                    obCont._material._bumpMapFilePath = Path.Combine(_rootFolder, obWords[l + 1]);
                                                                    l++;
                                                                    break;
                                                            }
                                                            k = l;
                                                        }
                                                        break;

                                                    case "Shape":
                                                        obCont._obType = ObjectType.Shape;
                                                        oriK = k;
                                                        for (int l = oriK + 1; l < obWords.Count; l++)
                                                        {
                                                            switch (obWords[l])
                                                            {
                                                                case "sphere":
                                                                    obCont._shapeContainer._obShape = ObShape.Sphere;
                                                                    break;

                                                                case "cylinder":
                                                                    obCont._shapeContainer._obShape = ObShape.Cylinder;
                                                                    break;

                                                                case "cone":
                                                                    obCont._shapeContainer._obShape = ObShape.Cone;
                                                                    break;

                                                                case "plane":
                                                                    obCont._shapeContainer._obShape = ObShape.Plane;
                                                                    break;

                                                                case "float width":
                                                                    obCont._shapeContainer._size[0] = float.Parse(obWords[l + 1]);
                                                                    l++;
                                                                    break;

                                                                case "float height":
                                                                    obCont._shapeContainer._size[1] = float.Parse(obWords[l + 1]);
                                                                    l++;
                                                                    break;

                                                                case "float radius":
                                                                    obCont._shapeContainer._size[0] = float.Parse(obWords[l + 1]);
                                                                    obCont._shapeContainer._size[1] = float.Parse(obWords[l + 1]);
                                                                    l++;
                                                                    break;

                                                                case "float ymin":
                                                                    obCont._shapeContainer._yVal[0] = float.Parse(obWords[l + 1]);
                                                                    obCont._shapeContainer._size[1] = obCont._shapeContainer._yVal[1] - obCont._shapeContainer._yVal[0];
                                                                    l++;
                                                                    break;


                                                                case "float ymax":
                                                                    obCont._shapeContainer._yVal[1] = float.Parse(obWords[l + 1]);
                                                                    obCont._shapeContainer._size[1] = obCont._shapeContainer._yVal[1] - obCont._shapeContainer._yVal[0];
                                                                    l++;
                                                                    break;

                                                            }
                                                            k = l;
                                                        }
                                                        break;

                                                    case "Scale":
                                                        obCont._scale = new Vector3(float.Parse(obWords[k + 1]), float.Parse(obWords[k + 2]), float.Parse(obWords[k + 3]));
                                                        k += 3;
                                                        break;

                                                    case "Include":
                                                        obCont._obType = ObjectType.Obj;
                                                        obCont._filePath = Path.Combine(_rootFolder, obWords[k + 1]);
                                                        obCont._material._matType = MatType.Obj;
                                                        break;

                                                }
                                            }


                                            if (isObSettingEnd)
                                            {
                                                // Set Canvas
                                                _canvas.rectTransform.sizeDelta = new Vector2(_resolutionX / _resolutionY * 900, 900);
                                                _canvas.rectTransform.anchoredPosition = new Vector3(-_resolutionX / _resolutionY * 900 / 2, 0, 0);

                                                // Set Camera

                                                _camera.targetTexture = new RenderTexture(_resolutionX, _resolutionY, 0);
                                                _canvas.texture = _camera.targetTexture;
                                                // Create Object
                                                switch (obCont._obType)
                                                {
                                                    case ObjectType.Shape:
                                                        switch (obCont._shapeContainer._obShape)
                                                        {

                                                            case ObShape.Cylinder:
                                                                {
                                                                    ob = Instantiate(Cylinder);
                                                                    Cylinder obClass = ob.GetComponent<Cylinder>();
                                                                    obClass.SetObject(obCont);
                                                                    RayTraceUtils._objects.Add(ob.GetHashCode(), obClass);
                                                                }
                                                                break;

                                                            case ObShape.Sphere:
                                                                {
                                                                    ob = Instantiate(Sphere);
                                                                    Sphere obClass = ob.GetComponent<Sphere>();
                                                                    obClass.SetObject(obCont);
                                                                    RayTraceUtils._objects.Add(ob.GetHashCode(), obClass);
                                                                }
                                                                break;

                                                            case ObShape.Cone:
                                                                {
                                                                    ob = Instantiate(Cone);
                                                                    Cone obClass = ob.GetComponent<Cone>();
                                                                    obClass.SetObject(obCont);
                                                                    RayTraceUtils._objects.Add(ob.GetHashCode(), obClass);
                                                                }
                                                                break;

                                                            case ObShape.Plane:
                                                                {
                                                                    ob = Instantiate(Plane);
                                                                    Plane obClass = ob.GetComponent<Plane>();
                                                                    obClass.SetObject(obCont);
                                                                    RayTraceUtils._objects.Add(ob.GetHashCode(), obClass);
                                                                }
                                                                break;
                                                        }
                                                        break;

                                                    case ObjectType.Obj:
                                                        string mtlPath = Path.GetFileNameWithoutExtension(obCont._filePath) + ".mtl";
                                                        var loadedObj = new OBJLoader().Load(obCont._filePath, mtlPath);
                                                        loadedObj.transform.GetChild(0).gameObject.AddComponent<MeshCollider>();
                                                        Obj objContainer = loadedObj.AddComponent<Obj>();
                                                        objContainer.SetObject(obCont);
                                                        RayTraceUtils._objects.Add(loadedObj.transform.GetChild(0).gameObject.GetHashCode(), objContainer);

                                                        // Mesh process
                                                        Mesh mesh = loadedObj.transform.GetChild(0).GetComponent<MeshFilter>().mesh;
                                                        objContainer._matIndex = new byte[mesh.triangles.Length / 3];
                                                        int subMeshesNr = mesh.subMeshCount;

                                                        if (Path.GetFileNameWithoutExtension(obCont._filePath) != "sibenik")
                                                        {
                                                            for (int k = 0; k < mesh.triangles.Length / 3; k++)
                                                            {
                                                                int materialIdx = -1;
                                                                int[] hittedTriangle = new int[] { mesh.triangles[k * 3], mesh.triangles[k * 3 + 1], mesh.triangles[k * 3 + 2] };

                                                                for (int l = 0; l < subMeshesNr; l++)
                                                                {
                                                                    int[] tr = mesh.GetTriangles(i);
                                                                    for (int m = 0; m < tr.Length - 2; m++)
                                                                    {
                                                                        if (tr[m] == hittedTriangle[0] && tr[m + 1] == hittedTriangle[1] && tr[m + 2] == hittedTriangle[2])
                                                                        {
                                                                            materialIdx = l;
                                                                            break;
                                                                        }
                                                                    }
                                                                    if (materialIdx != -1) break;
                                                                }
                                                                objContainer._matIndex[k] = (byte)materialIdx;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            // objContainer._matIndex = File.ReadAllBytes(Path.Combine(_rootFolder, "ChurchData.bin"));
                                                        }
                                                        

                                                        // File.WriteAllBytes(Path.Combine(_rootFolder, "ModelData.bin"), objContainer._matIndex);
                                                        break;
                                                }
                                                break;
                                            }
                                        }
                                        break;
                                }
                            }
                            if (isSceneSettingEnd)
                                break;
                        }

                        break;

                }
            }

            
        }
        
    }

    public List<string> SplitStrWithSpc(string str)
    {
        List<string> splittedStr = new List<string>();
        string tmpStr = "";
        string[] words = str.Split(' ');
        char flag = '\0';
        for (int i = 0; i < words.Length; i++)
        {
            if (flag != '\0')
            {
                if (words[i].EndsWith(flag.ToString()))
                {
                    tmpStr += ' ' + words[i].Remove(words[i].Length - 1, 1);
                    flag = '\0';
                    splittedStr.Add(tmpStr);
                    tmpStr = "";
                }
                else
                {
                    tmpStr += ' ' + words[i];
                }
            }
            else if (words[i].StartsWith("\""))
            {
                if (words[i].EndsWith("\""))
                {
                    splittedStr.Add(words[i].Remove(0, 1).Remove(words[i].Length - 2, 1));
                    continue;
                }
                flag = '"';
                tmpStr = words[i].Remove(0, 1);
            } 
            else if (words[i].StartsWith("["))
            {
                if (words[i].EndsWith("]"))
                {
                    splittedStr.Add(words[i].Remove(0, 1).Remove(words[i].Length - 2, 1));
                    continue;
                }
                flag = ']';
                tmpStr = words[i].Remove(0, 1);
            }
            else
            {
                splittedStr.Add(words[i]);
            }
        }
        return splittedStr;
    }

    public List<float> GetNums(string str)
    {
        List<float> result = new List<float>();
        string[] numStrs = str.Split(' ');
        for (int i = 0; i < numStrs.Length; i++)
        {
            result.Add(float.Parse(numStrs[i]));
        }
        return result;
    }
    public int GetIntAfter(string str, string afterStr)
    {
        string[] words = str.Replace("[", "").Replace("]", "").Split(' ');
        bool isStrFound = false;
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i] == afterStr)
                isStrFound = true;
            if (isStrFound)
            {
                if (int.TryParse(words[i], out int num))
                    return num;
            }            
        }
        return 0;
    }

}