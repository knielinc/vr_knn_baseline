﻿
using Supercluster.KDTree;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class KNNSkeleton : MonoBehaviour
{
    public KNNBone rootBone;
    public KDTree<float, int> lHandQueryTree;
    public KDTree<float, int> rHandQueryTree;
    private List<Transform> boneTransforms;
    private List<Tuple<Transform, Transform, Quaternion>> rigTransforms;
    public Transform rig;
    private int featureVectorLength;

    public void SetKNNSkeleton(string filePath)
    {
        this.Parse(filePath);
        SetBoneRenderer();
    }

    public void SetKNNSkeleton(KNNBone rootBone, KDTree<float, int> lHandQueryTree, KDTree<float, int> rHandQueryTree, int featureVectorLength)
    {
        this.rootBone = rootBone;
        this.lHandQueryTree = lHandQueryTree;
        this.rHandQueryTree = rHandQueryTree;
        this.featureVectorLength = featureVectorLength;
        SetBoneRenderer();
    }

    private void SetBoneRenderer()
    {
        BoneRenderer myBoneRenderer;

        if (this.gameObject.GetComponent<BoneRenderer>() == null)
        {
            myBoneRenderer = this.gameObject.AddComponent<BoneRenderer>();
        }
        else
        {
            myBoneRenderer = this.gameObject.GetComponent<BoneRenderer>();
        }

        boneTransforms = new List<Transform>();
        rigTransforms = new List<Tuple<Transform, Transform, Quaternion>>();


        Stack<KNNBone> boneStack = new Stack<KNNBone>();
        Stack<Transform> transformStack = new Stack<Transform>();
        boneStack.Push(this.rootBone);
        transformStack.Push(rig);

        while (boneStack.Count > 0)
        {
            KNNBone topBone = boneStack.Pop();
            Transform topTransform = null;
            if (transformStack.Count > 0)
                topTransform = transformStack.Pop();

            foreach (KNNBone childBone in topBone.children)
            {
                boneStack.Push(childBone);
                if (topTransform != null)
                {
                    Transform childTransform = topTransform.Find(childBone.name);
                    if (childTransform != null)
                        transformStack.Push(childTransform);
                }
            }

            boneTransforms.Add(topBone.transform);
            if (topTransform != null && topTransform.name == topBone.name)
            {
                rigTransforms.Add(Tuple.Create(topBone.transform, topTransform, topTransform.rotation));
            }
        }

        myBoneRenderer.transforms = boneTransforms.ToArray();
    }

    public void SetSkeletonFromHandPos(KDTree<float,int> tree, Transform handTarget, string handName)
    {
        float[] posVec = new float[featureVectorLength];
        Transform headTransform = rootBone.transform.Find("LowerBack/Spine/Spine1/Neck/Neck1/Head");
        Transform neckTransform = rootBone.transform.Find("LowerBack/Spine/Spine1/Neck/Neck1/Head");
        rootBone.transform.position -= headTransform.position;

        SphericalCoords handTargetSpPosition = SphericalCoords.CartesianToSpherical(handTarget.position - headTransform.position);
        float handTargetSpPositionAngle = handTargetSpPosition.theta * Mathf.Rad2Deg;
        handTargetSpPosition.theta = 0;
        Vector3 rotatedHandTargetPos = handTargetSpPosition.ToCartesian();

        for (int i = 0; i < featureVectorLength; i++)
        {
            switch (i % 2)
            {
                case 0:
                    posVec[i] = rotatedHandTargetPos.x;
                    break;
                case 1:
                    posVec[i] = rotatedHandTargetPos.y;
                    break;
            }
        }

        Tuple<float[], int>[] poseIndex = tree.NearestNeighbors(posVec, 1);
        int index = poseIndex[0].Item2;
        rootBone.SetToRotation(index);

        Transform handTransform = rootBone.transform.Find(handName);
        SphericalCoords handTransformSpPosition = SphericalCoords.CartesianToSpherical(handTransform.position - headTransform.position);
        float handTransformSpPositionAngle = handTransformSpPosition.theta * Mathf.Rad2Deg;
        rootBone.transform.localRotation *= Quaternion.Euler(0, -handTargetSpPositionAngle + handTransformSpPositionAngle, 0);
    }

    public void SetSkeletonFromRightHandPos(Transform rHandTarget)
    {
        SetSkeletonFromHandPos(rHandQueryTree, rHandTarget, "LowerBack/Spine/Spine1/RightShoulder/RightArm/RightForeArm/RightHand");
    }

    public void SetSkeletonFromLeftHandPos(Transform lHandTarget)
    {
        SetSkeletonFromHandPos(lHandQueryTree, lHandTarget, "LowerBack/Spine/Spine1/LeftShoulder/LeftArm/LeftForeArm/LeftHand");
    }

    public void AssignRig()
    {
        foreach (var currElement in rigTransforms)
        {
            currElement.Item2.rotation = currElement.Item1.rotation * currElement.Item3;
        }
    }

    public void Save(string outputPath)
    {
        FileInfo fi = new FileInfo(outputPath);
        if (!fi.Directory.Exists)
        {
            System.IO.Directory.CreateDirectory(fi.DirectoryName);
        }
        StreamWriter writer = new StreamWriter(outputPath, false);

        writer.Write(this.ComposeString());

        writer.Close();
    }

    private string ComposeString()
    {
        StringBuilder stringBuilder = new StringBuilder();

        stringBuilder.AppendLine("#bones timesteps framesPerTimeStep");

        List<KNNBone> boneList = new List<KNNBone>();
        Stack<KNNBone> boneStack = new Stack<KNNBone>();
        boneStack.Push(this.rootBone);

        while (boneStack.Count > 0)
        {
            KNNBone top = boneStack.Pop();
            foreach (KNNBone child in top.children)
            {
                boneStack.Push(child);
            }
            boneList.Add(top);
        }
        
        stringBuilder.AppendLine(boneList.Count.ToString() + " " + this.rootBone.rotations.Count.ToString() + " " + this.lHandQueryTree.Navigator.Point.Length.ToString());
        stringBuilder.AppendLine("#name id parentid offsetx offsety offsetz rotations(quaternions * timesteps)");

        foreach (KNNBone currBone in boneList)
        {
            string currLine = currBone.name + " ";

            currLine += boneList.IndexOf(currBone) + " ";

            currLine += boneList.IndexOf(currBone.parent) + " ";

            currLine += currBone.offset.x.ToString() + " " + currBone.offset.y.ToString() + " " + currBone.offset.z.ToString();

            foreach (Quaternion currRot in currBone.rotations)
            {
                currLine += " " + currRot.x + " " + currRot.y + " " + currRot.z + " " + currRot.w;
            }
            stringBuilder.AppendLine(currLine);
        }

        string nextLine = this.KdTreeToString(this.lHandQueryTree);

        stringBuilder.AppendLine(nextLine);

        nextLine = this.KdTreeToString(this.rHandQueryTree);

        stringBuilder.AppendLine(nextLine);

        return stringBuilder.ToString();
    }

    public void Parse(string file)
    {
        StreamReader reader = new StreamReader(file);
        string currLine = reader.ReadLine(); // skip comment
        currLine = reader.ReadLine();
        string[] words = currLine.Split(' ');
        int nrOfBones = int.Parse(words[0]);
        int timeSteps = int.Parse(words[1]);
        int featureVecLength = int.Parse(words[2]);
        this.featureVectorLength = featureVecLength;
        currLine = reader.ReadLine(); // skip comment

        GameObject rootBone = new GameObject();
        this.rootBone = rootBone.AddComponent<KNNBone>();
        this.rootBone.initKNNBoneFromFile(file);
        this.rootBone.transform.parent = this.transform;

        for (int currBoneIdx = 0; currBoneIdx < nrOfBones; currBoneIdx++)
        {
            currLine = reader.ReadLine(); // ignore since it's done in the initBoneFromFile
        }

            currLine = reader.ReadLine();
        if(this.lHandQueryTree == null || this.lHandQueryTree.Count == 0)
            this.lHandQueryTree = ParseKDTree(currLine, featureVecLength, timeSteps);

        currLine = reader.ReadLine();
        if (this.rHandQueryTree == null || this.rHandQueryTree.Count == 0)
            this.rHandQueryTree = ParseKDTree(currLine, featureVecLength, timeSteps);

        reader.Close();
    }

    private KDTree<float, int> ParseKDTree(string file, int featureVecLength, int timeSteps)
    {
        //KdTree<float, int> tree = new KdTree<float, int>(featureVecLength, new FloatMath());
        var treeNodes = new List<int>();
        var treePoints = new List<float[]>();

        string[] words = file.Split(' ');
        int startingWindowIndex = 0;
        for (int currWindow = 0; currWindow < timeSteps; currWindow++)
        {
            int value = int.Parse(words[startingWindowIndex]);
            float[] featureVec = new float[featureVecLength];

            for (int currFeatureIdx = 0; currFeatureIdx < featureVecLength; currFeatureIdx++)
            {
                featureVec[currFeatureIdx] = float.Parse(words[startingWindowIndex + 1 + currFeatureIdx]);
            }

            //tree.Add(featureVec, value);
            treePoints.Add(featureVec); treeNodes.Add(value);

            startingWindowIndex += featureVecLength + 1;
        }
        return new KDTree<float, int>(featureVecLength, treePoints.ToArray(), treeNodes.ToArray(), Metrics.L2Norm);
    }
    private string KdTreeToString(KDTree<float, int> tree)
    {
        string returnString = "";
        bool isFirstItem = true;
        for (int currValueIdx = 0; currValueIdx < tree.InternalPointArray.Length && currValueIdx < tree.InternalNodeArray.Length; currValueIdx++)
        {
            if (tree.InternalPointArray[currValueIdx] == null)
                continue;

            if (isFirstItem)
            {
                returnString += tree.InternalNodeArray[currValueIdx].ToString();
                isFirstItem = false;
            }
            else
            {
                returnString += " " + tree.InternalNodeArray[currValueIdx].ToString();
            }
            foreach (float currfloat in tree.InternalPointArray[currValueIdx])
            {
                returnString += " " + currfloat.ToString();
            }
        }
        return returnString;
    }
}
