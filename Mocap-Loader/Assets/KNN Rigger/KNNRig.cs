using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class KNNRig : MonoBehaviour
{
    public KNNSkeleton lHandSkeleton;
    public KNNSkeleton rHandSkeleton;
    public KNNBone finalSkeletonBones;

    public Transform headTarget;
    public Transform rHandTarget;
    public Transform lHandTarget;

    private List<Tuple<KNNBone, KNNBone, KNNBone>> allBones; // left right final
    private List<Tuple<Transform, Transform, Quaternion>> rigTransforms;
    public Transform rig;

    public string skeletonPath = "Data/KNNSkeletons/knnSkeleton.knnSkeleton";
    // Start is called before the first frame update
    void Start()
    {
        InitRig();
    }

    public void InitRig()
    {
        if(rHandSkeleton == null || rHandSkeleton.rootBone == null || rHandSkeleton.lHandQueryTree == null || rHandSkeleton.rHandQueryTree == null || rHandSkeleton.lHandQueryTree.Count == 0 || rHandSkeleton.rHandQueryTree.Count == 0)
        {
            if(rHandSkeleton == null)
            {
                GameObject kNNSkeleton_ = new GameObject("Right-KNN-Skeleton");
                this.rHandSkeleton = kNNSkeleton_.AddComponent<KNNSkeleton>();
            }

            this.rHandSkeleton.SetKNNSkeleton(skeletonPath);
            this.rHandSkeleton.transform.parent = this.transform;
            this.rHandSkeleton.rootBone.transform.parent = this.rHandSkeleton.transform;
        }
        if (lHandSkeleton == null || lHandSkeleton.rootBone == null || lHandSkeleton.lHandQueryTree == null || lHandSkeleton.rHandQueryTree == null || lHandSkeleton.lHandQueryTree.Count == 0 || lHandSkeleton.rHandQueryTree.Count == 0)
        {
            if (lHandSkeleton == null)
            {
                GameObject kNNSkeleton_ = new GameObject("Left-KNN-Skeleton");
                this.lHandSkeleton = kNNSkeleton_.AddComponent<KNNSkeleton>();
            }

            this.lHandSkeleton.SetKNNSkeleton(skeletonPath);
            this.lHandSkeleton.transform.parent = this.transform;
            this.lHandSkeleton.rootBone.transform.parent = this.lHandSkeleton.transform;
        }

        if (finalSkeletonBones == null)
        {
            GameObject kNNSkeleton_ = new GameObject("final-KNN-Skeleton-Bones");
            GameObject rootBone = new GameObject("final-KNN-Skeleton-Bones");

            this.finalSkeletonBones = rootBone.AddComponent<KNNBone>();
            this.finalSkeletonBones.initKNNBoneFromFile(skeletonPath);
            this.finalSkeletonBones.transform.parent = kNNSkeleton_.transform;
            kNNSkeleton_.transform.parent = this.transform;
        }

        Stack<KNNBone> boneStackFinal = new Stack<KNNBone>();
        Stack<KNNBone> boneStackLeft  = new Stack<KNNBone>();
        Stack<KNNBone> boneStackRight = new Stack<KNNBone>();

        boneStackFinal.Push(this.finalSkeletonBones);
        boneStackLeft.Push(this.lHandSkeleton.rootBone);
        boneStackRight.Push(this.rHandSkeleton.rootBone);

        allBones = new List<Tuple<KNNBone, KNNBone, KNNBone>>();

        //assuming they all have the same hierarchy
        while (boneStackFinal.Count > 0)
        {
            KNNBone topBoneFinal = boneStackFinal.Pop();
            KNNBone topBoneLeft  = boneStackLeft.Pop();
            KNNBone topBoneRight = boneStackRight.Pop();

            foreach (KNNBone childBone in topBoneFinal.children)
            {
                boneStackFinal.Push(childBone);
            }

            foreach (KNNBone childBone in topBoneLeft.children)
            {
                boneStackLeft.Push(childBone);
            }

            foreach (KNNBone childBone in topBoneRight.children)
            {
                boneStackRight.Push(childBone);
            }

            if (topBoneLeft.name == topBoneRight.name && topBoneFinal.name == topBoneLeft.name)
            {
                allBones.Add(Tuple.Create(topBoneLeft, topBoneRight, topBoneFinal));
            }
        }


        rigTransforms = new List<Tuple<Transform, Transform, Quaternion>>();


        Stack<KNNBone> boneStack = new Stack<KNNBone>();
        Stack<Transform> transformStack = new Stack<Transform>();
        boneStack.Push(this.finalSkeletonBones);
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

            if (topTransform != null && topTransform.name == topBone.name)
            {
                rigTransforms.Add(Tuple.Create(topBone.transform, topTransform, topTransform.rotation));
            }
        }

        Transform targets;
        Transform lHandTarget;
        Transform rHandTarget;
        Transform headTarget;

        if (this.transform.Find("Targets") == null)
        {
            targets = new GameObject("Targets").transform;
            lHandTarget = new GameObject("LHandTarget").transform;
            rHandTarget = new GameObject("RHandTarget").transform;
            headTarget = new GameObject("HeadTarget").transform;
        } else
        {
            targets = this.transform.Find("Targets");
            lHandTarget = targets.Find("LHandTarget");
            rHandTarget = targets.Find("RHandTarget");
            headTarget = targets.Find("HeadTarget");
            if (lHandTarget == null)
                lHandTarget = new GameObject("LHandTarget").transform;
            if (rHandTarget == null)
                rHandTarget = new GameObject("RHandTarget").transform;
            if (headTarget == null)
                headTarget = new GameObject("HeadTarget").transform;
        }

        this.headTarget = headTarget;
        this.rHandTarget = rHandTarget;
        this.lHandTarget = lHandTarget;

        targets.transform.parent = this.transform;
        lHandTarget.transform.parent = targets.transform;
        rHandTarget.transform.parent = targets.transform;
        headTarget.transform.parent = targets.transform;

        BoneRenderer leftBoneRenderer  = lHandSkeleton.GetComponent<BoneRenderer>();
        BoneRenderer rightBoneRenderer = rHandSkeleton.GetComponent<BoneRenderer>();
        BoneRenderer finalBoneRenderer = finalSkeletonBones.GetComponent<BoneRenderer>();


        if (leftBoneRenderer != null)
            leftBoneRenderer.boneColor = Color.green;
        if (rightBoneRenderer != null)
            rightBoneRenderer.boneColor = Color.white;
    }

    void SetFinalPoseFromLandR()
    {
        float alpha = rHandSkeleton.rootBone.transform.rotation.eulerAngles.y;
        float beta = lHandSkeleton.rootBone.transform.rotation.eulerAngles.y;
        float gamma = (alpha + beta) / 2; ;

        if (beta - alpha > 180)
        {
            gamma = gamma + 180;
        }
        else if (beta - alpha < -180)
        {
            gamma = gamma - 180;
        }

        float beta_ = beta - gamma;
        float alpha_ = alpha - gamma;

        foreach (Tuple<KNNBone, KNNBone, KNNBone> boneTuple in allBones)
        {
            string leftShoulder = "LeftArm/LeftForeArm/LeftHand";
            string rightShoulder = "RightArm/RightForeArm/RightHand";
            string currBoneName = boneTuple.Item1.name;

            Transform leftBone = boneTuple.Item1.transform;
            Transform rightBone = boneTuple.Item2.transform;
            Transform finalBone = boneTuple.Item3.transform;

            if (currBoneName.Equals("LeftShoulder"))
            {
                //left shoulder
                finalBone.rotation = Quaternion.Euler(0, beta_, 0) * Quaternion.Slerp(rightBone.rotation, leftBone.rotation, 0.5f);
            }
            else if (currBoneName.Equals("RightShoulder"))
            {
                //right shoulder
                finalBone.rotation = Quaternion.Euler(0, alpha_, 0) * Quaternion.Slerp(rightBone.rotation, leftBone.rotation, 0.5f);
            }
            else if (leftShoulder.Contains(currBoneName))
            {
                //part of left arm
                finalBone.rotation = Quaternion.Slerp(rightBone.rotation, leftBone.rotation, 1.0f);
            }
            else if (rightShoulder.Contains(currBoneName))
            {
                //part of right arm
                finalBone.rotation = Quaternion.Slerp(rightBone.rotation, leftBone.rotation, 0.0f);
            }
            else if (currBoneName.Equals("Hips"))
            {
                //hips
                finalBone.rotation = Quaternion.Slerp(rightBone.rotation, leftBone.rotation, 0.5f);
            }
            else
            {
                //part of remaining body
                finalBone.rotation = Quaternion.Slerp(rightBone.rotation, leftBone.rotation, 0.5f);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        rHandSkeleton.SetSkeletonFromRightHandPos(rHandTarget);
        lHandSkeleton.SetSkeletonFromLeftHandPos(lHandTarget);
        SetFinalPoseFromLandR();

        AssignRig();

    }

    public void AssignRig()
    {
        foreach (var currElement in rigTransforms)
        {
            currElement.Item2.rotation = currElement.Item1.rotation * currElement.Item3;
        }
    }
}
