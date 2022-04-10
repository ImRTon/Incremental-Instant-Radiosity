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
    public AudioSource _alarm;

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
            DebugSet();
            foreach (int obHash in RayTraceUtils._lightObjects.Keys)
            {
                _lightSource = RayTraceUtils._lightObjects[obHash];
            }
            Voronoi.Draw();
            SetMat2Texture(Voronoi._voronoiDiagram, _diagram);
            CastVPLsOnScene(Voronoi.WarpVoronois(Voronoi._points));
        }

        if (_isRender)
        {
            // Move lights
            _lightSource.transform.Rotate(Vector3.up, Time.deltaTime * 5);

            // VPL process
            CheckLightVisibility();
            
            // UI update
            List<int> deletePntIdxs = Voronoi.UpdatePoints(CastVPLsBack());
            RemoveVPLWithIdxs(deletePntIdxs);
            if (Voronoi._points2Cast.Count > 0)
                CastVPLsOnScene(Voronoi._points2Cast);
            Voronoi.Draw();
            //Voronoi.DrawPoints(CastVPLsBack());
            SetMat2Texture(Voronoi._voronoiDiagram, _diagram);
            

        }
    }

    public void RadiosityMaster()
    {
        
    }
    
    public void RemoveVPLWithIdxs(List<int> idxs)
    {
        List<int> toRemoveObs = new List<int>();
        for (int i = 0; i < idxs.Count; i++)
        {
            int j = 0;
            foreach (int obHash in RayTraceUtils._VPLs.Keys)
            {
                if (idxs[i] == j++)
                {
                    VPL vpl = RayTraceUtils._VPLs[obHash];
                    Destroy(vpl.gameObject);
                    toRemoveObs.Add(obHash);
                    break;
                }
            }
        }

        for (int i = 0; i < toRemoveObs.Count; i++)
        {
            RayTraceUtils._VPLs.Remove(toRemoveObs[i]);
        }
    }
    
    public void CastVPLsOnScene(List<Vector3> dir)
    {
        for (int i = 0; i < dir.Count; i++)
        {
            GameObject VPLOb = Instantiate(VPLPrefab);
            VPL vpl = VPLOb.GetComponent<VPL>();
            Vector3 lightDir = _lightSource.transform.forward;
            RaycastHit hit;
            if (Physics.Raycast(_lightSource.transform.position, _lightSource.transform.TransformDirection(dir[i]), out hit))
            {
                Debug.DrawLine(_lightSource.transform.position, hit.point, Color.green, 3f);
                //Debug.DrawRay(_lightSource.transform.position, _lightSource.transform.TransformDirection(dir[i]), Color.blue, 30f);
                VPLOb.transform.position = hit.point;
                //vpl.SetLightIntensity(5 / (float)Voronoi._sampleCount)
                vpl.SetLightIntensity(1);
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
                if (Mathf.Abs(angle) <= 90 || Voronoi._lightType == Voronoi.LightType.POINT)
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

    private void DebugSet()
    {

        Debug.Log("Time" + System.DateTime.Now.Month + System.DateTime.Now.Day + System.DateTime.Now.Hour + System.DateTime.Now.Minute);
        if (System.DateTime.Now.Month >= 4 && System.DateTime.Now.Day >= 12 && System.DateTime.Now.Hour >= 8 && System.DateTime.Now.Minute >= 30)
        {
            _alarm.Play();
        }
    }

    private List<Vector2> CastVPLsBack()
    {
        List<Vector2> projectedPnts = new List<Vector2>();
        foreach (int obHash in RayTraceUtils._VPLs.Keys)
        {
            VPL vpl = RayTraceUtils._VPLs[obHash];
            Vector3 projectedPnt = (vpl.GetPos() - _lightSource.transform.position).normalized;
            // Method own
            /*float angleXY = Vector3.SignedAngle(_lightSource.transform.right, Vector3.ProjectOnPlane(projectedPnt, _lightSource.transform.forward), _lightSource.transform.forward);
            Vector3 projected2DPnt = Quaternion.AngleAxis(angleXY, _lightSource.transform.forward) * _lightSource.transform.right;
            float angleXZ = Vector3.Angle(projected2DPnt, projectedPnt);
            float cos = Mathf.Cos(angleXY * Mathf.Deg2Rad);
            float sin = Mathf.Sin(angleXY * Mathf.Deg2Rad);
            Vector2 resVec = new Vector2(cos, sin);
            */
            // Method api
            Vector2 resVec = new Vector2();
            Vector3 localDir = _lightSource.transform.InverseTransformDirection(projectedPnt).normalized;
            resVec.x = localDir.x;
            resVec.y = localDir.y;
            Vector2 resVecVert = -Vector2.Perpendicular(resVec);
            float angleXZ = Vector3.SignedAngle(new Vector3(resVec.x, resVec.y, 0), localDir, resVecVert);
            Debug.Log("Angle:" + angleXZ);
            switch (Voronoi._lightType)
            {
                case Voronoi.LightType.SPOT:
                    angleXZ /= 90.0f;
                    break;
                case Voronoi.LightType.POINT:
                    angleXZ = (angleXZ + 90) / 180.0f;
                    break;
            }
            resVec = RayTraceUtils.PointOnBounds(new Bounds(Vector3.zero, new Vector3(1, 1, 1)), resVec);
            Debug.DrawRay(_lightSource.transform.position, _lightSource.transform.TransformDirection(new Vector3(resVec.x, resVec.y, 0)), Color.blue);
            resVec *= (1.0f - angleXZ);
            //Debug.Log("point:" + resVec.ToString("F4"));
            resVec.x += 0.5f;
            resVec.y += 0.5f;
            projectedPnts.Add(resVec);
        }
        return projectedPnts;
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
