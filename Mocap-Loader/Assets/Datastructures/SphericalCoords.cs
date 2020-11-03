using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The main SphericalCoords class.
/// Contains all methods for converting from and to SphericalCoords.
/// </summary>
public class SphericalCoords
{
    // Summary:
    //     Radius of current coords [0 <= Radius] 
    public float radius { get; set; }

    // Summary:
    //     Theta angle of current coordinates. [0 <= theta <= 2*pi]
    public float theta { get; set; }

    // Summary:
    //     Phi angle of current coordinates. Phi = 90 deg - latitude. [0 <= phi <= pi]
    public float phi { get; set; }

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

    /// <summary>
    /// Converts to cartesian coordinates.
    /// </summary>
    public Vector3 ToCartesian()
    {
        return SphericalCoords.SphericalToCartesian(this);
    }
}
