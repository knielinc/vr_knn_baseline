using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SphericalCoords
{
    public float radius;
    public float theta; //how far around the "up" axis
    public float phi; // how "high"

    public SphericalCoords(float r, float t, float p)
    {
        radius = r;
        theta = t;
        phi = p;
    }

    public static Vector3 SphericalToCartesian(SphericalCoords coords)
    {
        float rSinPhi = coords.radius * Mathf.Sin(coords.phi);
        float x = rSinPhi * Mathf.Cos(coords.theta);
        float y = coords.radius * Mathf.Cos(coords.phi);
        float z = rSinPhi * Mathf.Sin(coords.theta);
        return new Vector3(x, y, z);
    }

    public static SphericalCoords CartesianToSpherical(Vector3 coords)
    {
        float r = Mathf.Sqrt(coords.x * coords.x + coords.y * coords.y + coords.z * coords.z);
        
        if (r == 0)
            return new SphericalCoords(0, 0, 0);

        float t = Mathf.Atan2(coords.z, coords.x);
        float p = Mathf.Acos(coords.y / r);

        return new SphericalCoords(r, t, p);
    }

    public Vector3 ToCartesian()
    {
        return SphericalCoords.SphericalToCartesian(this);
    }
}
