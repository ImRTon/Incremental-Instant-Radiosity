using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class RayTracer : MonoBehaviour
{
    public Camera _rayTracingCam;
    public Parser _parser;
    public UnityEngine.UI.RawImage _image;
    private Texture2D _texture;
    public UnityEngine.UI.Toggle _SaveImgToggle;

    // Start is called before the first frame update
    void Start()
    {
        List<Vector2> vec2s = HaltonSequence(5);
        for (int i = 0; i < vec2s.Count; i++)
        {
            Debug.Log(vec2s[i].ToString("F4"));
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    public void RadiosityMaster()
    {
        
    }

    private List<Vector2> HaltonSequence(int size)
    {
        List<Vector2> haltonList = new List<Vector2>();
        for (int i = 0; i < size; i++)
        {
            haltonList.Add(new Vector2(Halton(i, 2), Halton(i, 3)));
        }
        return haltonList;
    }

    private float Halton(int index, float hBase)
    {
        float f = 1;
        float r = 0;
        while (index > 0)
        {
            f /= hBase;
            r += f * (index % hBase);
            index = (int)(index / hBase);
        }
        return r;
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

    public void ResetScene()
    {
        RayTraceUtils._lightObjects.Clear();
        RayTraceUtils._objects.Clear();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Debug.Log("Scene reload!");
    }

    public void ExitScene()
    {
        Application.Quit();
    }
}
