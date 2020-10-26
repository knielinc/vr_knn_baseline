using KdTree;
using KdTree.Math;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KNNRigger
{
    public class KNNBone : MonoBehaviour
    {
        public List<KNNBone> children;
        public KNNBone parent;
        public List<Quaternion> rotations;
        public Vector3 offset;
        public void initKNNBone(string name)
        {
            this.name = name;
            this.children = new List<KNNBone>();
            this.rotations = new List<Quaternion>();
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

                KNNBone childBone = (KNNBone) childObj.AddComponent(typeof(KNNBone));
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
    public class KNNSkeleton : MonoBehaviour
    {
        public KNNBone rootBone;
        public KdTree<float, int> lHandQueryTree;
        public KdTree<float, int> rHandQueryTree;
        private int featureVectorLength;
        private Transform lHandTarget;
        private Transform rHandTarget;

        public void SetKNNSkeleton(string filePath)
        {
            this.Parse(filePath);
        }

        public void SetKNNSkeleton(KNNBone rootBone, KdTree<float, int> lHandQueryTree, KdTree<float, int> rHandQueryTree, int featureVectorLength)
        {
            this.rootBone = rootBone;
            this.lHandQueryTree = lHandQueryTree;
            this.rHandQueryTree = rHandQueryTree;
            this.featureVectorLength = featureVectorLength;
        }

        public void SetSkeletonFromRightHandPos(Transform rHandTarget)
        {
            float[] posVec = new float[featureVectorLength];

            for (int i = 0; i < featureVectorLength; i++)
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

            stringBuilder.AppendLine(boneList.Count.ToString() + " " + this.rootBone.rotations.Count.ToString() + " " + this.lHandQueryTree.First().Point.Length.ToString());
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

            List<KNNBone> boneList = new List<KNNBone>();

            for (int currBoneIdx = 0; currBoneIdx < nrOfBones; currBoneIdx++)
            {
                currLine = reader.ReadLine();

                words = currLine.Split(' ');

                string currBoneName = words[0];
                int currBoneId = int.Parse(words[1]);
                int currBoneParentId = int.Parse(words[2]);
                Vector3 currBoneOffset = new Vector3(float.Parse(words[3]), float.Parse(words[4]), float.Parse(words[5]));

                GameObject newKNNBoneObj = new GameObject();
                KNNBone currKNNBone = (KNNBone)newKNNBoneObj.AddComponent(typeof(KNNBone));
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
                else if(rootBone == null) // currBoneParentId = -1 && we have no rootBone
                {
                    this.rootBone = currKNNBone;
                }
                currKNNBone.transform.localPosition = currKNNBone.offset;

                boneList.Add(currKNNBone);
            }
            currLine = reader.ReadLine();
            if(this.lHandQueryTree == null)
                this.lHandQueryTree = ParseKDTree(currLine, featureVecLength, timeSteps);

            currLine = reader.ReadLine();
            if (this.rHandQueryTree == null)
                this.rHandQueryTree = ParseKDTree(currLine, featureVecLength, timeSteps);

            reader.Close();
        }

        private KdTree<float, int> ParseKDTree(string file, int featureVecLength, int timeSteps)
        {
            KdTree<float, int> tree = new KdTree<float, int>(featureVecLength, new FloatMath());

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

                tree.Add(featureVec, value);

                startingWindowIndex += featureVecLength + 1;
            }
            return tree;
        }
        private string KdTreeToString(KdTree<float, int> tree)
        {
            string returnString = "";
            bool isFirstItem = true;
            foreach (var currValue in tree)
            {
                if (isFirstItem)
                {
                    returnString += currValue.Value.ToString();
                    isFirstItem = false;
                }
                else
                {
                    returnString += " " + currValue.Value.ToString();
                }
                foreach (float currfloat in currValue.Point)
                {
                    returnString += " " + currfloat.ToString();
                }
            }
            return returnString;
        }
    }

}
