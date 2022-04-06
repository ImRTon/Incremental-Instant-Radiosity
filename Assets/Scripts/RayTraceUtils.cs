using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class RayTraceUtils
{
    public const int MAX_DEPTH = 10;
    public static Dictionary<int, Object> _objects = new Dictionary<int, Object>();
    public static Dictionary<int, LightSource> _lightObjects = new Dictionary<int, LightSource>();

    
    private static Color ToColor(Vector3 v)
    {
        return new Color(v.x, v.y, v.z);
    }

    private static Vector3 ToVector(Color c)
    {
        return new Vector3(c.r, c.g, c.b);
    }
    public static Color Tracer(Ray ray, int depth, int scatterCount)
    {
        RaycastHit hit;
        //Physics.queriesHitBackfaces = true;
        bool isHit = Physics.Raycast(ray, out hit, 100);
        if (isHit)
        {
            // if (depth <= 0 || scatterCount<0.00001)
            //     return new Color(0, 0, 0);
            // Debug.DrawLine(ray.origin, hit.point, Color.yellow, 30f);
            Color attenuation = new Color(0, 0, 0);
            Ray scatteredRay = new Ray();
            float mult = 1;
            if (Scatter(ref ray, ref hit, ref attenuation, ref scatteredRay, ref scatterCount, depth, ref mult))
            {
                if (mult >= 0.01)
                    return attenuation + Tracer(scatteredRay, depth - 1, scatterCount);
                else
                    return attenuation;
            }
            return new Color(0, 0, 0);
        }
        else
        {
            float t = 0.5f * (ray.direction.normalized.y + 1f);
            return ((1f - t) * Color.white + t * new Color(0.5f, 0.5f, 0.5f)) * 0.3f;
        }
    }

    public static bool Scatter(ref Ray ray, ref RaycastHit hit, ref Color attenuation, ref Ray scatteredRay, ref int scatterCount, int depth, ref float mult)
    {
        Object hitObClass = _objects[hit.transform.gameObject.GetHashCode()];
        switch (hitObClass._container._material._matType)
        {
            case MatType.Mirror:
                {
                    Vector3 reflected = Vector3.Reflect(ray.direction, hit.normal);
                    scatteredRay.origin = hit.point;
                    scatteredRay.direction = reflected;
                    attenuation = new Color(0, 0, 0);
                    mult = 1f;
                    return Vector3.Dot(scatteredRay.direction, hit.normal) > 0;
                }

            case MatType.Normal:
                {
                    Vector3 target = hit.normal.normalized * 0.5f +
                        new Vector3(Random.Range(-1, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;

                    scatteredRay.origin = hit.point;
                    scatteredRay.direction = target - hit.point;

                    //attenuation = _objects[hit.collider.gameObject.GetHashCode()].GetAlbedo(ref ray, ref hit);
                    CheckShadowRay(ref ray, ref hit, ref attenuation);
                    //attenuation *= Mathf.Pow(0.3f, scatterCount++);
                    //attenuation *= Mathf.Abs(Mathf.Cos(Vector3.Angle(ray.direction, hit.normal))) * ;
                    mult = Mathf.Max(0, Mathf.Abs(Mathf.Cos(Vector3.Angle(ray.direction, hit.normal))) / 3.1415926f)
                        * Vector3.Dot(hit.normal, scatteredRay.direction);
                    attenuation *= mult;
                    Vector3.Distance(ray.origin, hit.point);
                    return true;
                }

            case MatType.Map:
            case MatType.Obj:
                {
                    Vector3 targetPos = hit.normal.normalized * 0.5f +
                        new Vector3(Random.Range(-1, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;

                    scatteredRay.origin = hit.point;
                    scatteredRay.direction = targetPos - hit.point;

                    CheckShadowRay(ref ray, ref hit, ref attenuation);
                    mult = Mathf.Max(0, Mathf.Cos(Vector3.Angle(ray.direction, hit.normal)) / 3.1415926f)
                        * Vector3.Dot(hit.normal, scatteredRay.direction);
                    attenuation *= mult;
                    //attenuation *= Mathf.Pow(0.3f, scatterCount++);
                    //attenuation *= Mathf.Abs(Mathf.Cos(Vector3.Angle(ray.direction, hit.normal)));
                    return true;
                }

            default:
                Debug.LogError("ERROR, no MatType!");
                break;

        }

        return false;
    }

    public static void CheckShadowRay(ref Ray ray, ref RaycastHit hit, ref Color attenuation)
    {
        foreach (int obHash in _lightObjects.Keys)
        {
            LightSource lightOb = _lightObjects[obHash];
            Object hitOb = _objects[hit.transform.gameObject.GetHashCode()];
            Vector3[] lightSamplePoint = SampleLightPoint(lightOb);

            foreach (Vector3 lightPos in lightSamplePoint)
            {
                if (!IsInShadow(lightPos, ref hit))
                {
                    Vector3 lightDis = lightPos - hit.point;

                    Vector3 halfDir = Vector3.Normalize(-ray.direction + lightDis.normalized);
                    float diffuseScalar = Mathf.Max(0, Vector3.Dot(lightDis.normalized, hit.normal));
                    float specularScalar = Mathf.Pow(Mathf.Max(0, Vector3.Dot(halfDir, hit.normal)), 6);

                    switch (hitOb._container._material._matType)
                    {
                        case MatType.Normal:
                            {
                                attenuation += lightOb._lightSource.color * (ToColor(diffuseScalar * ToVector(hitOb._container._material._colorKd))
                                    + ToColor(specularScalar * ToVector(hitOb._container._material._colorKs )));
                            }
                            break;

                        case MatType.Mirror:
                            {
                                // None
                            }
                            break;

                        case MatType.Map:
                            {
                                Texture2D texture = (Texture2D)hit.transform.GetComponent<MeshRenderer>().material.GetTexture("_MainTex");
                                //var bytes = texture.EncodeToPNG();
                                //File.WriteAllBytes(Path.Combine("D://", "texture.png"), bytes);
                                //texture = (Texture2D)hit.transform.GetComponent<MeshRenderer>().material.GetTexture("_BumpMap");
                                //bytes = texture.EncodeToPNG();
                                //File.WriteAllBytes(Path.Combine("D://", "normal.png"), bytes);
                                Vector2 pixUV = hit.textureCoord;
                                Vector3 mapColor =ToVector(texture.GetPixel((int)(pixUV.x * texture.width), (int)(pixUV.y * texture.height)));
                                attenuation += lightOb._lightSource.color * ToColor(diffuseScalar * mapColor);
                            }
                            break;

                        case MatType.Obj:
                            {
                                int triangleIndex = hit.triangleIndex;

                                Material[] mats = hit.transform.GetComponent<MeshRenderer>().sharedMaterials;
                                Color mapColor = new Color(0, 0, 0);
                                Obj meshObClass = (Obj)_objects[hit.transform.gameObject.GetHashCode()];
                                Texture2D matTex = mats[meshObClass._matIndex[triangleIndex]].mainTexture as Texture2D;
                                if (matTex != null)
                                {
                                    mapColor = matTex.GetPixelBilinear(hit.textureCoord.x, hit.textureCoord.y) * mats[meshObClass._matIndex[triangleIndex]].color;
                                }
                                else if (mats[meshObClass._matIndex[triangleIndex]].color != null)
                                    mapColor = mats[meshObClass._matIndex[triangleIndex]].color;
                                attenuation += lightOb._lightSource.color * ToColor(diffuseScalar * ToVector(mapColor));
                            }
                            break;

                        default:
                            Debug.LogError("ERROR, no MatType");
                            break;
                    }
                }
            }
            attenuation /= (float)lightSamplePoint.Length;
        }
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
