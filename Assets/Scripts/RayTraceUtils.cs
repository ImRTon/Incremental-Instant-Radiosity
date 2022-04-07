using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UtilsModule;
using OpenCVForUnity.ImgprocModule;

public static class RayTraceUtils
{
    public const int MAX_DEPTH = 10;
    public static Dictionary<int, Object> _objects = new Dictionary<int, Object>();
    public static Dictionary<int, LightSource> _lightObjects = new Dictionary<int, LightSource>();

    public static List<Vector2> HaltonSequence(int size)
    {
        List<Vector2> haltonList = new List<Vector2>();
        for (int i = 0; i < size; i++)
        {
            haltonList.Add(new Vector2(Halton(i, 2), Halton(i, 3)));
        }
        return haltonList;
    }

    public static float Halton(int index, float hBase)
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


    private static Vector3[] SampleLightPoint(LightSource lightObClass)
    {
        Vector3[] points = null;
        Vector3 lightPos = lightObClass.transform.position;
        switch (lightObClass._lightType)
        {
            case LightType.Point:
                points = new Vector3[1];
                points[0] = lightObClass.transform.position;
                break;

            case LightType.Area:
                int sample = lightObClass._nSamples;
                points = new Vector3[sample * sample];

                Vector3 leftTopPos = new Vector3(lightPos.x - 0.5f * lightObClass._area[0], lightPos.y - 0.5f * lightObClass._area[1], lightPos.z);
                Vector2 aspectRatio = new Vector2(lightObClass._area[0] / sample, lightObClass._area[1] / sample);
                for (int w = 0; w < sample; w++)
                {
                    for (int h = 0; h < sample; h++)
                    {
                        points[w * sample + h] = new Vector3(
                            leftTopPos.x + aspectRatio.x * (w + 0.5f), leftTopPos.y + aspectRatio.y * (h + 0.5f), lightPos.z);
                    }
                }
                break;
        }
        return points;
    }

    public static bool IsInShadow(Vector3 lightPos, ref RaycastHit hit)
    {
        Vector3 lv = hit.point - lightPos;
        if (Vector3.Dot(-lv, hit.normal) < 0)
            return false;

        Ray shadowRay = new Ray(lightPos, lv);

        RaycastHit shadowHit;
        if (Physics.Raycast(shadowRay, out shadowHit, lv.magnitude * 0.9999f))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public static float GetAOWeight(Ray ray, int AOSamples)
    {
        RaycastHit hit;
        bool isHit = Physics.Raycast(ray, out hit, 100);
        if (isHit)
        {
            float ambientWeight = 0.0f;
            for (int i=0; i<AOSamples;i++)
            {
                Ray rayAmbient = new Ray(hit.point,new Vector3(hit.normal.x + Random.Range(-0.5f, 0.5f),hit.normal.y+Random.Range(0.0f, 0.5f),hit.normal.z+Random.Range(-0.5f, 0.5f)));
                RaycastHit hitAmbient;
                bool isHitAmbient = Physics.Raycast(rayAmbient, out hitAmbient, 3);
                if(!isHitAmbient)
                {
                    ambientWeight += 1.0f / (float)AOSamples;
                }
                else
                {
                    ambientWeight += 0.5f / (float)AOSamples;
                }
            }
            return ambientWeight;
        }
        return 0.0f;
    }
}

public static class Voronoi
{
    public static int width = 250;
    public static int height = 250;
    public static Mat _voronoiDiagram;
    public static OpenCVForUnity.CoreModule.Rect _rect;
    public static Subdiv2D _subdiv2D;
    public static List<Vector2> _points;
    public enum LightType
    {
        SPOT, POINT, AREA
    }
    public static LightType _lightType;

    public static void Init()
    {
        _voronoiDiagram = new Mat(height, width, CvType.CV_8U);
        _rect = new OpenCVForUnity.CoreModule.Rect(0, 0, width, height);
        _subdiv2D = new Subdiv2D(_rect);
        _lightType = LightType.SPOT;
    }

    public static void SetPointFromHalton(int size)
    {
        List<Vector2> haltonList = RayTraceUtils.HaltonSequence(size);
        for (int i = 0; i < size; i++)
        {
            _subdiv2D.insert(new Point(haltonList[i].x * width, haltonList[i].y * height));
        }
        _points = haltonList;
    }

    public static void Draw()
    {
        _voronoiDiagram.setTo(Scalar.all(0));
        DrawVoronoi();
        DrawDelaunay();
        DrawPoints();
        WarpVoronois();
    }

    public static void DrawDelaunay()
    {
        MatOfFloat6 triangleMatList = new MatOfFloat6();
        _subdiv2D.getTriangleList(triangleMatList);
        float[] pointArray = triangleMatList.toArray();

        for (int i = 0; i < pointArray.Length / 6; i++)
        {

            Point p0 = new Point(pointArray[i * 6 + 0], pointArray[i * 6 + 1]);
            Point p1 = new Point(pointArray[i * 6 + 2], pointArray[i * 6 + 3]);
            Point p2 = new Point(pointArray[i * 6 + 4], pointArray[i * 6 + 5]);

            if (!(p0.x < 0 || p0.y < 0 || p0.x > width || p0.y > height ||
                p1.x < 0 || p1.y < 0 || p1.x > width || p1.y > height ||
                p2.x < 0 || p2.y < 0 || p2.x > width || p2.y > height))
            {
                Imgproc.line(_voronoiDiagram, p0, p1, new Scalar(128), 1, Imgproc.LINE_AA, 0);
                Imgproc.line(_voronoiDiagram, p1, p2, new Scalar(128), 1, Imgproc.LINE_AA, 0);
                Imgproc.line(_voronoiDiagram, p2, p0, new Scalar(128), 1, Imgproc.LINE_AA, 0);
            }

        }
    }

    public static void DrawPoints()
    {
        for (int i = 0; i < _points.Count; i++)
        {
            Imgproc.circle(_voronoiDiagram, new Point(_points[i].x * width, _points[i].y * height), 3, new Scalar(255), -1, 8, 0);
        }
    }
    
    public static void DrawVoronoi()
    {
        List<MatOfPoint2f> facets = new List<MatOfPoint2f>();
        MatOfPoint2f centPoints = new MatOfPoint2f();
        _subdiv2D.getVoronoiFacetList(new MatOfInt(), facets, centPoints);

        List<MatOfPoint> ifacets = new List<MatOfPoint>();
        
        for (int i = 0; i < facets.Count; i++)
        {
            
            MatOfPoint ifacet = new MatOfPoint();
            ifacet.fromArray(facets[i].toArray());
            Scalar color = new Scalar(i * 2 % 255);
            Imgproc.fillConvexPoly(_voronoiDiagram, ifacet, color);
        }
    }

    public static List<Vector3> WarpVoronois()
    {
        List<Vector3> vecs = new List<Vector3>();
        switch (_lightType)
        {
            case LightType.SPOT:
                {
                    for (int i = 0; i < _points.Count; i++)
                    {
                        float a = _points[i].x * 3.14159265f * 2.0f;
                        float l = Mathf.Sqrt(_points[i].y);
                        Vector3 resVec = new Vector3(Mathf.Cos(a) * l, Mathf.Sin(a) * l, 0.0f);
                        l = Vector3.Dot(resVec, resVec);
                        if (l >= 1.0f)
                            resVec.z = 0;
                        else
                            resVec.z = Mathf.Sqrt(1.0f - l);
                        Debug.DrawRay(new Vector3(0, 0, 0), resVec, Color.red, 30f);
                    }
                }
                break;
            case LightType.POINT:
                {

                }
                break;
            case LightType.AREA:
                {

                }
                break;
        }
        return vecs;
    }
}