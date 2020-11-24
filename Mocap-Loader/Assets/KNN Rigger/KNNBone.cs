using Accord;
using Accord.Math;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

public class KNNBone : MonoBehaviour
{
    public List<KNNBone> children;
    public KNNBone parent;
    public List<Quaternion> rotations;
    public UnityEngine.Vector3 offset;
    public void initKNNBone(string name)
    {
        this.name = name;
        this.children = new List<KNNBone>();
        this.rotations = new List<Quaternion>();
    }

    public void initKNNBoneFromFile(string file)
    {
        this.children = new List<KNNBone>();
        this.rotations = new List<Quaternion>();

        StreamReader reader = new StreamReader(file);
        string currLine = reader.ReadLine(); // skip comment
        currLine = reader.ReadLine();
        string[] words = currLine.Split(' ');
        int nrOfBones = int.Parse(words[0]);
        int timeSteps = int.Parse(words[1]);
        int featureVecLength = int.Parse(words[2]);
        currLine = reader.ReadLine(); // skip comment

        List<KNNBone> boneList = new List<KNNBone>();

        for (int currBoneIdx = 0; currBoneIdx < nrOfBones; currBoneIdx++)
        {
            currLine = reader.ReadLine();

            words = currLine.Split(' ');

            string currBoneName = words[0];
            int currBoneId = int.Parse(words[1]);
            int currBoneParentId = int.Parse(words[2]);
            UnityEngine.Vector3 currBoneOffset = new UnityEngine.Vector3(float.Parse(words[3]), float.Parse(words[4]), float.Parse(words[5]));

            KNNBone currKNNBone;

            if (currBoneParentId >= 0)
            {
                GameObject newKNNBoneObj = new GameObject();
                currKNNBone = (KNNBone)newKNNBoneObj.AddComponent(typeof(KNNBone));
            } else
            {
                currKNNBone = this;
            }

            currKNNBone.initKNNBone(currBoneName);

            currKNNBone.offset = currBoneOffset;

            for (int currRotationIdx = 0; currRotationIdx < timeSteps; currRotationIdx++)
            {
                int currRotationFloatIdxOffset = 4 * currRotationIdx;
                float rotX = float.Parse(words[6 + currRotationFloatIdxOffset]);
                float rotY = float.Parse(words[6 + currRotationFloatIdxOffset + 1]);
                float rotZ = float.Parse(words[6 + currRotationFloatIdxOffset + 2]);
                float rotW = float.Parse(words[6 + currRotationFloatIdxOffset + 3]);

                Quaternion currRot = new Quaternion(rotX, rotY, rotZ, rotW);
                currKNNBone.rotations.Add(currRot);
            }

            if (currBoneParentId >= 0)
            {
                currKNNBone.parent = boneList[currBoneParentId];
                currKNNBone.parent.children.Add(currKNNBone);
                currKNNBone.transform.parent = currKNNBone.parent.transform;
            }

            currKNNBone.transform.localPosition = currKNNBone.offset;

            boneList.Add(currKNNBone);
        }
    }
    public void initKNNBone(Transform rootTransform)
    {
        this.name = rootTransform.name;
        this.transform.localPosition = rootTransform.localPosition;
        this.transform.localRotation = rootTransform.localRotation;
        this.offset = this.transform.localPosition;
        this.children = new List<KNNBone>();
        this.rotations = new List<Quaternion>();


        for (int childIdx = 0; childIdx < rootTransform.childCount; childIdx++)
        {
            GameObject childObj = new GameObject();

            KNNBone childBone = (KNNBone)childObj.AddComponent(typeof(KNNBone));
            childBone.parent = this;
            childBone.transform.parent = this.transform;
            childBone.initKNNBone(rootTransform.GetChild(childIdx));
            children.Add(childBone);
        }
    }

    public void DivideOffsetsBy(float factor)
    {
        this.offset /= factor;
        this.transform.localPosition = this.offset;
        foreach (KNNBone child in this.children)
        {
            child.DivideOffsetsBy(factor);
        }
    }

    public void RemoveRotations()
    {
        this.rotations.Clear();
    }
    public Quaternion GetRotation(int index)
    {
        return rotations.ElementAt(index);
    }

    public void SetToRotation(int index)
    {
        this.transform.localRotation = GetRotation(index);
        foreach (KNNBone child in this.children)
        {
            child.SetToRotation(index);
        }
    }

    public void SetToRotations(RotationIndex[] rotationIndices)
    {
        if(rotationIndices.Length == 1)
        {
            SetToRotation(rotationIndices[0].index);
            return;
        }
        var matrixM = Accord.Math.Matrix.Zeros(4,4);

        double weightSum = 0;
        foreach(RotationIndex rotIdx in rotationIndices)
        {
            Quaternion currRot = GetRotation(rotIdx.index).normalized;

            double[] q = { currRot.x, currRot.y, currRot.z, currRot.w };
            if (currRot.x < 0)
                q.Multiply(-1);
            double currWeight = rotIdx.weight;

            matrixM = (q.Outer(q)).Multiply(currWeight).Add(matrixM);

            weightSum += currWeight;
        }

        matrixM.Divide(weightSum);
        var eigDec = new Accord.Math.Decompositions.EigenvalueDecomposition(matrixM, false, true);
        double[] outQuat = eigDec.Eigenvectors.GetColumn(0);

        Quaternion newRot = new Quaternion((float)outQuat[0], (float)outQuat[1], (float)outQuat[2], (float)outQuat[3]);
        this.transform.localRotation = newRot;
        //this.transform.localRotation = GetRotation(index);
        foreach (KNNBone child in this.children)
        {
            child.SetToRotations(rotationIndices);
        }
    }

    public void AddRotationsFromTransform(Transform rootTransform)
    {
        if (rootTransform.name == this.name)
        {
            this.rotations.Add(rootTransform.localRotation);
            foreach (KNNBone child in this.children)
            {
                int childIdx = this.children.IndexOf(child);
                child.AddRotationsFromTransform(rootTransform.GetChild(childIdx));
                child.parent = this;
            }
        }
        else
        {
            Debug.LogWarning("ERROR: trying to set rotations from bone " + rootTransform.name + " to " + this.name + " (KNNBONE), skipping children");
        }
    }
    void Start()
    {
    }
    void Update()
    {
    }
}
