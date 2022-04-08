using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;

public class RayTracer : MonoBehaviour
{
    public Camera _rayTracingCam;
    public Parser _parser;
    public UnityEngine.UI.RawImage _image;
    public UnityEngine.UI.RawImage _diagram;
    private Texture2D _texture;
    public UnityEngine.UI.Toggle _SaveImgToggle;
    public GameObject VPLPrefab;
    public LightSource _lightSource;

    public Mat _voronoiDiagram;
    public int width = 250;
    public int height = 250;
    
    private Texture2D _rImgTexture;

    private bool _isRender = false;


    // Start is called before the first frame update
    void Start()
    {
        Init();
        Voronoi.Init();
        _rImgTexture = new Texture2D(Voronoi.width, Voronoi.height, TextureFormat.RGBA32, false);
        /*for (int i = 0; i < vec2s.Count; i++)
        {
            Debug.Log(vec2s[i].ToString("F4"));
        }*/
        SetMat2Texture(_voronoiDiagram, _diagram);

    }

    // Update is called once per frame
    void Update()
    {
        if (_parser._isParseDone)
        {
            _parser._isParseDone = false;
            Init();
            Voronoi.Init();
            Voronoi.SetPointFromHalton(64);
            _isRender = true;
            foreach (int obHash in RayTraceUtils._lightObjects.Keys)
            {
                _lightSource = RayTraceUtils._lightObjects[obHash];
            }
        }

        if (_isRender)
        {
            // Move lights

            // VPL process


            // UI update
            Voronoi.Draw();
            SetMat2Texture(Voronoi._voronoiDiagram, _diagram);
            CastVPLsOnScene(Voronoi.WarpVoronois());
        }
    }

    public void RadiosityMaster()
    {
        
    }
    public List<GameObject> CastVPLsOnScene(List<Vector3> dir)
    {
        List<GameObject> VPLs = new List<GameObject>();
        for (int i = 0; i < dir.Count; i++)
        {
            GameObject VPLOb = Instantiate(VPLPrefab);
            VPL vpl = VPLOb.GetComponent<VPL>();
            RayTraceUtils._VPLs.Add(VPLOb.GetHashCode(), vpl);
            Vector3 lightDir = _lightSource.transform.forward;
            RaycastHit hit;
            if (Physics.Raycast(_lightSource.transform.position, _lightSource.transform.TransformDirection(dir[i]), out hit))
            {
                Debug.DrawLine(_lightSource.transform.position, hit.point, Color.green);
            }
            VPLs.Add(VPLOb);
        }
        return VPLs;
    }

    private void SetImgTexture(Texture2D texture2D)
    {
        texture2D.Apply();
        RenderTexture renderTexture = new RenderTexture(texture2D.width, texture2D.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(texture2D, renderTexture);
        _image.texture = renderTexture;
        if (_SaveImgToggle.isOn)
        {
            var bytes = texture2D.EncodeToPNG();
            File.WriteAllBytes(Path.Combine(_parser._rootFolder, "result.png"), bytes);
        }
    }

    private void SetMat2Texture(Mat img, UnityEngine.UI.RawImage rawImage)
    {
        Utils.matToTexture2D(img, _rImgTexture);
        _diagram.texture = _rImgTexture;
    }

    public void ResetScene()
    {
        RayTraceUtils._lightObjects.Clear();
        RayTraceUtils._objects.Clear();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Debug.Log("Scene reload!");
        SetMat2Texture(_voronoiDiagram, _diagram);
    }

    public void ExitScene()
    {
        Application.Quit();
    }

    private void Init()
    {
        //_voronoiDiagram = new Mat(height, width, CvType.CV_8U);
        _voronoiDiagram = Mat.zeros(height, width, CvType.CV_8U);
    }
}
