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
            Voronoi.SetPointFromHalton(Voronoi._sampleCount);
            _isRender = true;
            foreach (int obHash in RayTraceUtils._lightObjects.Keys)
            {
                _lightSource = RayTraceUtils._lightObjects[obHash];
            }
            Voronoi.Draw();
            SetMat2Texture(Voronoi._voronoiDiagram, _diagram);
            FirstCastVPLsOnScene(Voronoi.WarpVoronois());
        }

        if (_isRender)
        {
            // Move lights
            _lightSource.transform.Rotate(Vector3.up, Time.deltaTime * 20);

            // VPL process
            CheckLightVisibility();

            // UI update
            Voronoi.Draw();
            SetMat2Texture(Voronoi._voronoiDiagram, _diagram);
            

        }
    }

    public void RadiosityMaster()
    {
        
    }
    public void FirstCastVPLsOnScene(List<Vector3> dir)
    {
        for (int i = 0; i < dir.Count; i++)
        {
            GameObject VPLOb = Instantiate(VPLPrefab);
            VPL vpl = VPLOb.GetComponent<VPL>();
            Vector3 lightDir = _lightSource.transform.forward;
            RaycastHit hit;
            if (Physics.Raycast(_lightSource.transform.position, _lightSource.transform.TransformDirection(dir[i]), out hit))
            {
                Debug.DrawLine(_lightSource.transform.position, hit.point, Color.green);
                VPLOb.transform.position = hit.point;
                vpl.SetLightIntensity(1 / Voronoi._sampleCount);
            }
            RayTraceUtils._VPLs.Add(VPLOb.GetHashCode(), vpl);
        }
    }
    
    public void CheckLightVisibility()
    {
        List<int> toRemoveObs = new List<int>();
        foreach (int obHash in RayTraceUtils._VPLs.Keys)
        {
            VPL vpl = RayTraceUtils._VPLs[obHash];
            if (Physics.Raycast(_lightSource.transform.position, vpl.GetPos()))
            {
                float angle = Vector3.SignedAngle(_lightSource.transform.forward, (vpl.transform.position - _lightSource.transform.position), _lightSource.transform.forward);
                if (Mathf.Abs(angle) <= 90)
                {
                    Debug.DrawRay(_lightSource.transform.position, _lightSource.transform.forward, Color.red);
                    continue;
                }
            }
            toRemoveObs.Add(obHash);
            Destroy(vpl.gameObject);
        }
        for (int i = 0; i < toRemoveObs.Count; i++)
        {
            RayTraceUtils._VPLs.Remove(toRemoveObs[i]);
        }
    }

    private void CastVPLsBack()
    {
        List<Vector2> projectedPnts = new List<Vector2>();
        foreach (int obHash in RayTraceUtils._VPLs.Keys)
        {
            VPL vpl = RayTraceUtils._VPLs[obHash];
            Vector3 projectedPnt = (vpl.GetPos() - _lightSource.transform.position).normalized;
            float angleXY = Vector3.SignedAngle(_lightSource.transform.right, projectedPnt, _lightSource.transform.forward);
            Vector3 projected2DPnt = Quaternion.AngleAxis(angleXY, _lightSource.transform.forward) * _lightSource.transform.right;
            float angleXZ = Vector3.Angle(projected2DPnt, projectedPnt);
            float cos = Mathf.Cos(angleXY * Mathf.Deg2Rad);
            float sin = Mathf.Sin(angleXY * Mathf.Deg2Rad);
            switch (Voronoi._lightType)
            {
                case Voronoi.LightType.SPOT:
                    angleXZ /= 90.0f;
                    break;
                case Voronoi.LightType.POINT:
                    angleXZ /= 180.0f;
                    break;
            }
            Vector2 resVec = new Vector2(cos , sin);
            resVec = RayTraceUtils.PointOnBounds(new Bounds(Vector3.zero, Vector3.one), resVec);
            resVec *= angleXZ;
            projectedPnts.Add(resVec);
        }
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
