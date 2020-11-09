using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public struct FeatureItem
{
    public FeatureItem(Vector3 pos, Vector3 vel, Vector3 acc)
    {
        position = pos;
        velocity = vel;
        acceleration = acc;
    }

    public Vector3 position { get; }
    public Vector3 velocity { get; }
    public Vector3 acceleration { get; }

    public override string ToString() => "position: " + position.ToString() + " , velocity: " + velocity.ToString() + " , acceleration: " + acceleration.ToString();
}

public class KNNRig : MonoBehaviour
{
    public KNNSkeleton lHandSkeleton;
    public KNNSkeleton rHandSkeleton;
    public KNNBone finalSkeletonBones;

    public Transform headTarget;
    public Transform rHandTarget;
    public Transform lHandTarget;

    public float scale = 1.3f;

    private List<Tuple<KNNBone, KNNBone, KNNBone>> allBones; // left right final
    private List<Tuple<Transform, Transform, Quaternion>> rigTransforms;
    public Transform rig;

    public string skeletonPath = "Data/KNNSkeletons/knnSkeleton.knnSkeleton";
    // Start is called before the first frame update

    public float updateFrequency = 90; // herz
    private LinkedList<FeatureItem> leftArmFeatureVec; //pos vel acc
    private LinkedList<FeatureItem> rightArmFeatureVec; //pos vel acc
    private Vector3 lastPosRightHand;
    private Vector3 lastPosLeftHand;


    private float lastFeatureTime = 0;
    private float lastUpdateTime = 0;
    private int iterationCounter = 0;
    
    void Start()
    {
        lastFeatureTime = Time.time;
        lastUpdateTime = lastFeatureTime;
        InitRig();
    }

    public void InitRig()
    {
        leftArmFeatureVec = new LinkedList<FeatureItem>();
        rightArmFeatureVec = new LinkedList<FeatureItem>();

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

        Transform targets = null;
        Transform lHandTmpTarget = null;
        Transform rHandTmpTarget = null;
        Transform headTmpTarget = null;

        if (this.transform.Find("Targets") == null)
        {
            targets = new GameObject("Targets").transform;
            targets.transform.parent = this.transform;
        }
        else
        {
            targets = this.transform.Find("Targets");
            lHandTmpTarget = targets.Find("LHandTarget");
            rHandTmpTarget = targets.Find("RHandTarget");
            headTmpTarget = targets.Find("HeadTarget");
        }

        if (lHandTarget == null)
        {
            if(lHandTmpTarget == null)
            {
                lHandTmpTarget = new GameObject("LHandTarget").transform;
                lHandTmpTarget.transform.parent = targets.transform;
            }
            lHandTarget = lHandTmpTarget;
        }
        if (rHandTarget == null)
        {
            if(rHandTmpTarget == null)
            {
                rHandTmpTarget = new GameObject("RHandTarget").transform;
                rHandTmpTarget.transform.parent = targets.transform;
            }
            rHandTarget = rHandTmpTarget;
        }
        if (headTarget == null)
        {
            if (headTmpTarget == null)
            {
                headTmpTarget = new GameObject("HeadTarget").transform;
                headTmpTarget.transform.parent = targets.transform;
            }
            headTarget = headTmpTarget;
        }

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

            Quaternion currRotation;

            if (currBoneName.Equals("LeftShoulder"))
            {
                //left shoulder
                currRotation = Quaternion.Euler(0, beta_, 0) * Quaternion.Slerp(rightBone.rotation, leftBone.rotation, 0.5f);
            }
            else if (currBoneName.Equals("RightShoulder"))
            {
                //right shoulder
                currRotation = Quaternion.Euler(0, alpha_, 0) * Quaternion.Slerp(rightBone.rotation, leftBone.rotation, 0.5f);
            }
            else if (leftShoulder.Contains(currBoneName))
            {
                //part of left arm
                currRotation = Quaternion.Slerp(rightBone.rotation, leftBone.rotation, 1.0f);
            }
            else if (rightShoulder.Contains(currBoneName))
            {
                //part of right arm
                currRotation = Quaternion.Slerp(rightBone.rotation, leftBone.rotation, 0.0f);
            }
            else if (currBoneName.Equals("Hips"))
            {
                //hips
                currRotation = Quaternion.Slerp(rightBone.rotation, leftBone.rotation, 0.5f);
            }
            else
            {
                //part of remaining body
                currRotation = Quaternion.Slerp(rightBone.rotation, leftBone.rotation, 0.5f);
            }

            if(rightArmFeatureVec.Count > 3)
            {
                finalBone.rotation = Quaternion.Slerp(finalBone.rotation, currRotation, 0.3f);
            } else
            {
                finalBone.rotation = currRotation;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

        iterationCounter++;
        Vector3 headPosition = this.headTarget.position;
        Vector3 lHandPosition = this.lHandTarget.position;
        Vector3 rHandPosition = this.rHandTarget.position;

        Vector3 relativeRHandPositionsWithScale = getPositionsWithScale(rHandPosition, headPosition, this.scale);
        Vector3 relativeLHandPositionsWithScale = getPositionsWithScale(lHandPosition, headPosition, this.scale);

        UpdateFeatureVectors(relativeLHandPositionsWithScale, relativeRHandPositionsWithScale);

        if(rightArmFeatureVec.Count > 0 && leftArmFeatureVec.Count > 0)
        {
            rHandSkeleton.SetSkeletonFromRightHandPos(rightArmFeatureVec);
            lHandSkeleton.SetSkeletonFromLeftHandPos(leftArmFeatureVec);
        }

        SetFinalPoseFromLandR();

        AssignRig();

    }

    private void UpdateFeatureVectors(Vector3 currLeftHandPos, Vector3 currRightHandPos)
    {
        float currTime = Time.time;
        if(currTime - lastFeatureTime >= 1.0f / updateFrequency)
        {
            float targetTime = lastFeatureTime + 1.0f / updateFrequency;
            float t = (targetTime - lastUpdateTime) / (currTime - lastUpdateTime);

            Vector3 targetPosLeftHand = Vector3.Lerp(lastPosLeftHand, currLeftHandPos, t);
            Vector3 targetPosRightHand = Vector3.Lerp(lastPosRightHand, currRightHandPos, t);

            Vector3 leftHandVel;
            Vector3 rightHandVel;

            Vector3 leftHandAcc;
            Vector3 rightHandAcc;

            if (rightArmFeatureVec.Count > 0 &&leftArmFeatureVec.Count > 0)
            {
                leftHandVel = leftArmFeatureVec.Last.Value.position - targetPosLeftHand;
                rightHandVel = rightArmFeatureVec.Last.Value.position - targetPosRightHand;

                leftHandAcc = leftArmFeatureVec.Last.Value.velocity - leftHandVel;
                rightHandAcc = rightArmFeatureVec.Last.Value.velocity - rightHandVel;

            }
            else
            {
                leftHandVel = new Vector3();
                rightHandVel = new Vector3();

                leftHandAcc = new Vector3();
                rightHandAcc = new Vector3();
            }


            leftArmFeatureVec.AddLast(new FeatureItem(targetPosLeftHand, leftHandVel, leftHandAcc));
            rightArmFeatureVec.AddLast(new FeatureItem(targetPosRightHand, rightHandVel, rightHandAcc));

            if(leftArmFeatureVec.Count > 30)
            {
                leftArmFeatureVec.RemoveFirst();
            }

            if(rightArmFeatureVec.Count > 30)
            {
                rightArmFeatureVec.RemoveFirst();
            }
            //Debug.Log(leftArmFeatureVec.Last.Value.ToString());
            lastFeatureTime = currTime;
        }
        lastPosLeftHand = currLeftHandPos;
        lastPosRightHand = currRightHandPos;
        lastUpdateTime = currTime;
    }

    private Vector3 getPositionsWithScale(Vector3 handPositions, Vector3 headPositions, float scale)
    {
        Vector3 relativePositionsWithScale = (handPositions - headPositions) * scale;
        
        return relativePositionsWithScale;
    }
    public void AssignRig()
    {
        Transform head = null;
        Transform hips = null;

        foreach (var currElement in rigTransforms)
        {
            if (currElement.Item2.name.Equals("Head"))
            {
                head = currElement.Item2;
            } else if (currElement.Item2.name.Equals("Hips"))
            {
                hips = currElement.Item2;
            }
            
            currElement.Item2.rotation = currElement.Item1.rotation * currElement.Item3;
        }
        if(hips != null)
            hips.position = headTarget.position + (hips.position - head.position);
    }
}
