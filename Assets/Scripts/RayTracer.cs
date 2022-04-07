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

    public Mat _voronoiDiagram;
    public int width = 250;
    public int height = 250;
    
    
    // Start is called before the first frame update
    void Start()
    {
        Init();
        Voronoi.Init();
        List<Vector2> vec2s = RayTraceUtils.HaltonSequence(10);
        for (int i = 0; i < vec2s.Count; i++)
        {
            Debug.Log(vec2s[i].ToString("F4"));
        }
        SetMat2Texture(_voronoiDiagram, _diagram);

    }

    // Update is called once per frame
    void Update()
    {
        // Move lights

        // VPL process


        // UI update
        Voronoi.SetPointFromHalton(20);
        Voronoi.Draw();
        SetMat2Texture(Voronoi._voronoiDiagram, _diagram);
    }

    public void RadiosityMaster()
    {
        
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
        Texture2D texture = new Texture2D(img.cols(), img.rows(), TextureFormat.RGBA32, false);
        Utils.matToTexture2D(img, texture);
        _diagram.texture = texture;
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
