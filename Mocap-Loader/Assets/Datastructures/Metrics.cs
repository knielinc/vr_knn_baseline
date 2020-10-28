using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Metrics
{
    // Start is called before the first frame update
    public static Func<float[], float[], double> L2Norm = (x, y) =>
    {
        double dist = 0f;
        for (int i = 0; i < x.Length; i++)
        {
            dist += (x[i] - y[i]) * (x[i] - y[i]);
        }

        return dist;
    };
}
