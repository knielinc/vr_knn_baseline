using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;



[CustomEditor(typeof(BVHLoader))]
public class BVHLoaderEditor : Editor
{
    float hSliderValue = 0.0f;
    float prevHSliderValue = 0.0f;
    float currFrame = 0;
    bool centerAnimation = false;
    bool alignHips = false;
    bool normalizeSkeleton = false;
    string bvhFileName = "sample.bvh";
    string bvhFolderName = "";
    string headBoneName  = "LowerBack/Spine/Spine1/Neck/Neck1/Head";
    string lHandBoneName = "LowerBack/Spine/Spine1/LeftShoulder/LeftArm/LeftForeArm/LeftHand";
    string rHandBoneName = "LowerBack/Spine/Spine1/RightShoulder/RightArm/RightForeArm/RightHand";
    string outputPath = "Data/KNNSkeletons/knnSkeleton.knnSkeleton";

    string slidingWindowSize = "100";
    bool ignoreRotationOnExport = true;

    string slidingWindowOffset = "50";
    string pollingRate = "80";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        BVHLoader bvhLoader = (BVHLoader)target;

        GUILayout.Label(bvhFileName);

        if (GUILayout.Button("Load skeleton from File"))
        {
            bvhFileName = EditorUtility.OpenFilePanel("Choose File or Dir", "", "bvh");
            bvhFolderName = "Using File";
            currFrame = 0;
            hSliderValue = 0;
            bvhLoader.loadFromFile(bvhFileName);
            bvhLoader.setFile(bvhFileName);
        }
        GUILayout.Label(bvhFolderName);

        if (GUILayout.Button("Load skeletons from Folder"))
        {
            bvhFolderName = EditorUtility.OpenFolderPanel("Choose File or Dir", "", "");
            bvhFileName = "Using Folder";
            currFrame = 0;
            hSliderValue = 0;
            bvhLoader.setFolder(bvhFolderName);
        }

        hSliderValue = GUILayout.HorizontalScrollbar(hSliderValue, .1f, 0.0f, bvhLoader.getFrames() - 1.5f);
        //GUILayout.Label(hSliderValue.ToString());

        if (prevHSliderValue != hSliderValue)
        {
            currFrame = bvhLoader.setToFrame(hSliderValue, centerAnimation, alignHips, normalizeSkeleton);
            prevHSliderValue = hSliderValue;
        }

        GUILayout.Label("Frame: " + currFrame.ToString());

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("<<"))
        {
            currFrame--;
            bvhLoader.setToFrame(currFrame, centerAnimation, alignHips, normalizeSkeleton);
        }
        if (GUILayout.Button(">>"))
        {
            currFrame += 0.1f;
            bvhLoader.setToFrame(currFrame, centerAnimation, alignHips, normalizeSkeleton);
        }
        GUILayout.EndHorizontal();

        centerAnimation = GUILayout.Toggle(centerAnimation, "center animation (head)", "Button");
        alignHips = GUILayout.Toggle(alignHips, "align hips", "Button");
        normalizeSkeleton = GUILayout.Toggle(normalizeSkeleton, "normalize", "Button");
        
        GUILayout.Space(20.0f);

        GUILayout.Label("Head Bone Path:");
        headBoneName = GUILayout.TextField(headBoneName);

        GUILayout.Label("Right Hand Bone Path:");
        rHandBoneName = GUILayout.TextField(rHandBoneName);

        GUILayout.Label("Left Hand Bone Path:");
        lHandBoneName = GUILayout.TextField(lHandBoneName);

        GUILayout.Label("Sliding Window Size (ms)");
        slidingWindowSize = GUILayout.TextField(slidingWindowSize);

        GUILayout.Label("Sliding Window Offset (ms)");
        slidingWindowOffset = GUILayout.TextField(slidingWindowOffset);

        GUILayout.Label("Target Polling Rate (Hz)");
        pollingRate = GUILayout.TextField(pollingRate);

        ignoreRotationOnExport = GUILayout.Toggle(ignoreRotationOnExport, "Ignore Rotation");
        
        if (GUILayout.Button("Create K-NN Rig"))
        {
            bvhLoader.createKNNRig(headBoneName, rHandBoneName, lHandBoneName, float.Parse(slidingWindowSize), float.Parse(slidingWindowOffset),  float.Parse(pollingRate), ignoreRotationOnExport, outputPath);
        }
        //GUILayout.Label("Output FileName");

        outputPath = GUILayout.TextField(outputPath);


    }
}
