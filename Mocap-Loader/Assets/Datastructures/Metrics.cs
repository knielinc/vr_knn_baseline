using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Metrics
{
    public static float L2Weight = 0.3f;
    // Start is called before the first frame update
    public static Func<float[], float[], double> L2Norm = (x, y) =>
    {
        double dist = 0f;
        for (int i = 0; i < x.Length; i++)
        {
            if(i % 9 < 3)
                dist += (x[i] - y[i]) * (x[i] - y[i]);
        }

        return dist;
    };

    public static Func<float[], float[], double> WeightedL2Norm = (x, y) =>
    {
        double dist = 0f;
        for (int i = 0; i < x.Length; i++)
        {
            if (i % 9 < 3 && i < 9)
                dist += (1.0f - L2Weight) * (x[i] - y[i]) * (x[i] - y[i]);
            if (i % 9 < 3 && i >= 9)
                dist += L2Weight * (x[i] - y[i]) * (x[i] - y[i]);
        }

        return dist;
    };
}

public struct RotationIndex
{
    public RotationIndex(int idx, float w)
    {
        index = idx;
        weight = w;
    }

    public int index { get; }
    public float weight { get; }
    public override string ToString() => "index: " + index.ToString() + " , weight: " + weight.ToString();
}