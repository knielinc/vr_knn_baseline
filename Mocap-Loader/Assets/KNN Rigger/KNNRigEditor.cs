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

        if (GUILayout.Button("loadFromFile"))
        {
            knnRig.InitRig();
        }

        //knnRig.updateSkeleton();

    }
}
