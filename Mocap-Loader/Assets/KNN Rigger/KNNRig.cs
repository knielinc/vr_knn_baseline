using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class KNNRig : MonoBehaviour
{
    public KNNSkeleton skeleton;
    public Transform headTarget;
    public Transform rHandTarget;
    public Transform lHandTarget;
    public string skeletonPath = "Data/KNNSkeletons/knnSkeleton.knnSkeleton";
    // Start is called before the first frame update
    void Start()
    {
        InitRig();
    }

    public void InitRig()
    {
        if(skeleton == null || skeleton.rootBone == null || skeleton.lHandQueryTree == null || skeleton.rHandQueryTree == null)
        {
            if(skeleton == null)
            {
                GameObject kNNSkeleton_ = new GameObject("KNN-Skeleton");
                this.skeleton = kNNSkeleton_.AddComponent<KNNSkeleton>();
            }

            this.skeleton.SetKNNSkeleton(skeletonPath);
            this.skeleton.transform.parent = this.transform;
            this.skeleton.rootBone.transform.parent = this.skeleton.transform;
        }

        BoneRenderer myBoneRenderer;

        if (this.gameObject.GetComponent<BoneRenderer>() == null)
        {
            myBoneRenderer = this.gameObject.AddComponent<BoneRenderer>();
        } else
        {
            myBoneRenderer = this.gameObject.GetComponent<BoneRenderer>();
        }


        List<Transform> boneTransforms = new List<Transform>();
        Stack<KNNBone> boneStack = new Stack<KNNBone>();
        boneStack.Push(skeleton.rootBone);

        while (boneStack.Count > 0)
        {
            KNNBone top = boneStack.Pop();
            foreach (KNNBone child in top.children)
            {
                boneStack.Push(child);
            }
            boneTransforms.Add(top.transform);
        }
        myBoneRenderer.transforms = boneTransforms.ToArray();
        

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
    }

    // Update is called once per frame
    void Update()
    {
        skeleton.SetSkeletonFromRightHandPos(rHandTarget);
    }
}
