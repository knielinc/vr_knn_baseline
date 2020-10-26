﻿using JetBrains.Annotations;
using KdTree;
using KdTree.Math;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace KNNRigger
{
    public class KNNBone
    {
        public Transform transform;
        public List<KNNBone> children;
        public KNNBone parent;
        public string name;
        public List<Quaternion> rotations;
        public Vector3 offset;
        public KNNBone(string name)
        {
            this.name = name;
            this.children = new List<KNNBone>();
            this.rotations = new List<Quaternion>();

        }
        public KNNBone(Transform rootTransform)
        {
            this.name = rootTransform.name;
            this.transform = rootTransform;
            this.offset = rootTransform.localPosition;
            this.children = new List<KNNBone>();
            this.rotations = new List<Quaternion>();


            for (int childIdx = 0; childIdx < rootTransform.childCount; childIdx++)
            {
                children.Add(new KNNBone(rootTransform.GetChild(childIdx)));
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

    }
    public class KNNSkeleton
    {
        public KNNBone rootBone;
        public KdTree<float, int> lHandQueryTree;
        public KdTree<float, int> rHandQueryTree;
        private int featureVectorLength;
        private Transform lHandTarget;
        private Transform rHandTarget;
        public KNNSkeleton(KNNBone rootBone, KdTree<float, int> lHandQueryTree, KdTree<float, int> rHandQueryTree, int featureVectorLength)
        {
            this.rootBone = rootBone;
            this.lHandQueryTree = lHandQueryTree;
            this.rHandQueryTree = rHandQueryTree;
            this.featureVectorLength = featureVectorLength;
        }

        public void SetSkeletonFromRightHandPos(Transform rHandTarget)
        {
            float[] posVec = new float[featureVectorLength];

            for(int i = 0; i < featureVectorLength; i++)
            {
                switch (i % 3)
                {
                    case 0:
                        posVec[i] = rHandTarget.position.x;
                        break;
                    case 1:
                        posVec[i] = rHandTarget.position.y;
                        break;
                    case 2:
                        posVec[i] = rHandTarget.position.z;
                        break;
                }
            }

            KdTreeNode<float, int>[] poseIndex = rHandQueryTree.GetNearestNeighbours(posVec, 1);
            int index = poseIndex[0].Value;
            rootBone.SetToRotation(index);
        }
    }
    public class KNNRig : MonoBehaviour
    {
        public Transform rightHandTarget;
        public KNNSkeleton skeleton;

        
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        public void saveSkeleton()
        {
            StreamWriter writer = new StreamWriter("test.txt", false);
            writer.WriteLine("#bones timesteps framesPerTimeStep");

            List<KNNBone> boneList = new List<KNNBone>();
            Stack<KNNBone> boneStack = new Stack<KNNBone>();
            boneStack.Push(skeleton.rootBone);

            while (boneStack.Count > 0)
            {
                KNNBone top = boneStack.Pop();
                foreach(KNNBone child in top.children)
                {
                    boneStack.Push(child);
                }
                boneList.Add(top);
            }

            writer.WriteLine(boneList.Count.ToString() + " " + skeleton.rootBone.rotations.Count.ToString() + " " + skeleton.lHandQueryTree.First().Point.Length.ToString());
            writer.WriteLine("#name id parentid offsetx offsety offsetz rotations(quaternions * timesteps)");

            foreach(KNNBone currBone in boneList)
            {
                string currLine = currBone.name + " ";

                currLine += boneList.IndexOf(currBone) + " ";

                currLine += boneList.IndexOf(currBone.parent) + " ";

                currLine += currBone.offset.x.ToString() + " " + currBone.offset.y.ToString() + " " + currBone.offset.z.ToString();

                foreach(Quaternion currRot in currBone.rotations)
                {
                    currLine += " " + currRot.x + " " + currRot.y + " " + currRot.z + " " + currRot.w;
                }
                writer.WriteLine(currLine);
            }

            string nextLine = "";
            bool isFirstItem = true;
            foreach(var currValue in skeleton.lHandQueryTree)
            {
                if (isFirstItem)
                {
                    nextLine += currValue.Value.ToString();
                    isFirstItem = false;
                }
                else
                {
                    nextLine += " " + currValue.Value.ToString();
                }
                foreach (float currfloat in currValue.Point)
                {
                    nextLine += " " + currfloat.ToString();
                }
            }
            writer.WriteLine(nextLine);

            nextLine = "";
            isFirstItem = true;
            foreach (var currValue in skeleton.rHandQueryTree)
            {
                if (isFirstItem)
                {
                    nextLine += currValue.Value.ToString();
                    isFirstItem = false;
                }
                else
                {
                    nextLine += " " + currValue.Value.ToString();
                }
                foreach (float currfloat in currValue.Point)
                {
                    nextLine += " " + currfloat.ToString();
                }
            }

            writer.WriteLine(nextLine);

            writer.Close();
        }

        public void loadSkeleton() {
            StreamReader reader = new StreamReader("test.txt");
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
                Vector3 currBoneOffset = new Vector3(float.Parse(words[3]), float.Parse(words[4]), float.Parse(words[5]));

                KNNBone currKNNBone = new KNNBone(currBoneName);
                currKNNBone.offset = currBoneOffset;

                for(int currRotationIdx = 0; currRotationIdx < timeSteps; currRotationIdx++)
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
                }

                boneList.Add(currKNNBone);
            }
            KdTree<float, int> lHandQueryTree = new KdTree<float, int>(featureVecLength, new FloatMath());
            currLine = reader.ReadLine();
            words = currLine.Split(' ');
            int startingWindowIndex = 0;
            for(int currWindow = 0; currWindow < timeSteps; currWindow++)
            {
                int value = int.Parse(words[startingWindowIndex]);
                float[] featureVec = new float[featureVecLength];

                for(int currFeatureIdx = 0; currFeatureIdx < featureVecLength; currFeatureIdx++)
                {
                    featureVec[currFeatureIdx] = float.Parse(words[startingWindowIndex + 1 + currFeatureIdx]);
                }

                lHandQueryTree.Add(featureVec, value);

                startingWindowIndex += featureVecLength + 1;
            }

            KdTree<float, int> rHandQueryTree = new KdTree<float, int>(featureVecLength, new FloatMath());
            currLine = reader.ReadLine();
            words = currLine.Split(' ');
            startingWindowIndex = 0;
            for (int currWindow = 0; currWindow < timeSteps; currWindow++)
            {
                int value = int.Parse(words[startingWindowIndex]);
                float[] featureVec = new float[featureVecLength];

                for (int currFeatureIdx = 0; currFeatureIdx < featureVecLength; currFeatureIdx++)
                {
                    featureVec[currFeatureIdx] = float.Parse(words[startingWindowIndex + 1 + currFeatureIdx]);
                }

                rHandQueryTree.Add(featureVec, value);

                startingWindowIndex += featureVecLength + 1;
            }

            reader.Close();
        }

        public void updateSkeleton()
        {
            if (skeleton != null && rightHandTarget != null)
            {
                skeleton.SetSkeletonFromRightHandPos(rightHandTarget);
            }
        }
    }
}//namespance KNNRigger

