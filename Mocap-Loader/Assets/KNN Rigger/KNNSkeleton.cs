
using Supercluster.KDTree;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class KNNSkeleton : MonoBehaviour
{
    private bool ignoreRotation = false;
    public KNNBone rootBone;
    public KDTree<float, int> lHandQueryTree;
    public KDTree<float, int> rHandQueryTree;

    private List<Transform> boneTransforms;
    private int featureVectorLength;

    public void SetKNNSkeleton(string filePath)
    {
        this.Parse(filePath);
        SetBoneRenderer();
    }

    public void SetKNNSkeleton(KNNBone rootBone, bool ignoreRotation, KDTree<float, int> lHandQueryTree, KDTree<float, int> rHandQueryTree, int featureVectorLength)
    {
        this.ignoreRotation = ignoreRotation;
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

        Stack<KNNBone> boneStack = new Stack<KNNBone>();
        boneStack.Push(this.rootBone);

        while (boneStack.Count > 0)
        {
            KNNBone topBone = boneStack.Pop();

            foreach (KNNBone childBone in topBone.children)
            {
                boneStack.Push(childBone);
            }

            boneTransforms.Add(topBone.transform);
        }

        myBoneRenderer.transforms = boneTransforms.ToArray();
    }

    public void SetSkeletonFromHandPos(KDTree<float,int> tree, LinkedList<FeatureItem> features, string handName, int k)
    {
        float[] featureVec = new float[featureVectorLength];
        Transform headTransform = rootBone.transform.Find("LowerBack/Spine/Spine1/Neck/Neck1/Head");
        Transform neckTransform = rootBone.transform.Find("LowerBack/Spine/Spine1/Neck/Neck1/Head");

        rootBone.transform.position -= headTransform.position;

        float handTargetSpPositionAngle = SphericalCoords.GetYRotFromVec(features.Last().position) * Mathf.Rad2Deg;

        var currFeatureContainer = features.Last;

        Debug.Assert(featureVectorLength % 9 == 0);
        for (int i = 0; i < featureVectorLength / 9; i++)
        {
            FeatureItem currFeatureItem = currFeatureContainer.Value;

            Vector3 handPos = currFeatureItem.position;
            Vector3 handVel = currFeatureItem.velocity;
            Vector3 handAcc = currFeatureItem.acceleration;

            if (ignoreRotation)
            {
                float handYRotValue = SphericalCoords.GetYRotFromVec(handPos) * Mathf.Rad2Deg;

                SphericalCoords sphCoords = SphericalCoords.CartesianToSpherical(handPos);
                sphCoords.theta = 0;
                Vector3 outVec = sphCoords.ToCartesian();

                handPos = Quaternion.AngleAxis(handYRotValue, Vector3.up) * handPos;
                handVel = Quaternion.AngleAxis(handYRotValue, Vector3.up) * handVel;
                handAcc = Quaternion.AngleAxis(handYRotValue, Vector3.up) * handAcc;
            }

            int startIndex = 9 * i;
            featureVec[startIndex]     = handPos.x;
            featureVec[startIndex + 1] = handPos.y;
            featureVec[startIndex + 2] = handPos.z;

            featureVec[startIndex + 3] = handVel.x;
            featureVec[startIndex + 4] = handVel.y;
            featureVec[startIndex + 5] = handVel.z;

            featureVec[startIndex + 6] = handAcc.x;
            featureVec[startIndex + 7] = handAcc.y;
            featureVec[startIndex + 8] = handAcc.z;

            if(currFeatureContainer.Previous != null)
            {
                currFeatureContainer = currFeatureContainer.Previous;
            }
        }

        Tuple<float[], int>[] poseIndices = tree.NearestNeighbors(featureVec, k);
        
        int index = poseIndices[0].Item2;

        RotationIndex[] rotations = new RotationIndex[poseIndices.Length];

        for(int i = 0; i < poseIndices.Length; i++)
        {
            double distance = Metrics.WeightedL2Norm(poseIndices[i].Item1, featureVec);
            RotationIndex currIdx = new RotationIndex(poseIndices[i].Item2, (float)(1.0 / distance));
            rotations[i] = currIdx;
        }

        rootBone.SetToRotations(rotations);
        //rootBone.SetToRotation(index);

        Transform handTransform = rootBone.transform.Find(handName);
        SphericalCoords handTransformSpPosition = SphericalCoords.CartesianToSpherical(handTransform.position - headTransform.position);
        float handTransformSpPositionAngle = handTransformSpPosition.theta * Mathf.Rad2Deg;
        rootBone.transform.localRotation *= Quaternion.Euler(0, -handTargetSpPositionAngle + handTransformSpPositionAngle, 0);
    }

    public void SetSkeletonFromRightHandPos(LinkedList<FeatureItem> rHandFeatures)
    {
        SetSkeletonFromHandPos(rHandQueryTree, rHandFeatures, "LowerBack/Spine/Spine1/RightShoulder/RightArm/RightForeArm/RightHand", 5);
    }

    public void SetSkeletonFromLeftHandPos(LinkedList<FeatureItem> lHandFeatures)
    {
        SetSkeletonFromHandPos(lHandQueryTree, lHandFeatures, "LowerBack/Spine/Spine1/LeftShoulder/LeftArm/LeftForeArm/LeftHand", 5);
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

        stringBuilder.AppendLine("#bones timesteps framesPerTimeStep ignoreRotation");

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
        
        stringBuilder.AppendLine(boneList.Count.ToString() + " " + this.rootBone.rotations.Count.ToString() + " " + this.lHandQueryTree.Navigator.Point.Length.ToString() + " " + ignoreRotation.ToString());
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
        this.featureVectorLength = int.Parse(words[2]);
        this.ignoreRotation = bool.Parse(words[3]);
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
            this.lHandQueryTree = ParseKDTree(currLine, featureVectorLength, timeSteps);

        currLine = reader.ReadLine();
        if (this.rHandQueryTree == null || this.rHandQueryTree.Count == 0)
            this.rHandQueryTree = ParseKDTree(currLine, featureVectorLength, timeSteps);

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
        return new KDTree<float, int>(featureVecLength, treePoints.ToArray(), treeNodes.ToArray(), Metrics.WeightedL2Norm);
    }
    private string KdTreeToString(KDTree<float, int> tree)
    {
        StringBuilder stringBuilder = new StringBuilder();
        bool isFirstItem = true;
        for (int currValueIdx = 0; currValueIdx < tree.InternalPointArray.Length && currValueIdx < tree.InternalNodeArray.Length; currValueIdx++)
        {
            if (tree.InternalPointArray[currValueIdx] == null)
                continue;

            if (isFirstItem)
            {
                stringBuilder.Append(tree.InternalNodeArray[currValueIdx].ToString());
                isFirstItem = false;
            }
            else
            {
                stringBuilder.Append(" "); stringBuilder.Append(tree.InternalNodeArray[currValueIdx].ToString());
            }
            foreach (float currfloat in tree.InternalPointArray[currValueIdx])
            {
                stringBuilder.Append(" "); stringBuilder.Append(currfloat.ToString());
            }
        }
        return stringBuilder.ToString();
    }
}
