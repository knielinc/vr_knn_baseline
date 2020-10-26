using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using KNNRigger;

[CustomEditor(typeof(KNNRig))]
public class KNNRigEditor : Editor
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        KNNRig knnRig = (KNNRig)target;

        if (GUILayout.Button("set to right hand"))
        {
            knnRig.updateSkeleton();
        }

        knnRig.updateSkeleton();

    }
}
