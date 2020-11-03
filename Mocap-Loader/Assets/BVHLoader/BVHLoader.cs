using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Supercluster.KDTree;
using UnityEngine.Animations.Rigging;
using System;

public class SkeletonConverter
{
    public GameObject rootObj;
    public float size;
    private float minpos, maxpos;
    public List<Transform> transforms = new List<Transform>();
    public void createFromBVH(BVHParser parser)
    {
        minpos = 0;
        maxpos = 0;
        size = 0;

        BVHParser.BVHBone currBone = parser.root;
        rootObj = new GameObject();
        rootObj.name = currBone.name;
        rootObj.transform.localPosition = new Vector3(-currBone.offsetX, currBone.offsetY, currBone.offsetZ);

        transforms.Add(rootObj.transform);

        foreach(BVHParser.BVHBone currChildBone in currBone.children)
        {
            iterateChildBone(currChildBone, rootObj);
        }

        size = maxpos - minpos;
    }

    private void iterateChildBone(BVHParser.BVHBone currBone, GameObject parent)
    {
        GameObject currObj = new GameObject();
        currObj.name = currBone.name;
        currObj.transform.parent = parent.transform;
        currObj.transform.localPosition = new Vector3(-currBone.offsetX, currBone.offsetY, currBone.offsetZ);

        if (currObj.transform.position.y < minpos)
        {
            minpos = currObj.transform.position.y;
        }

        if(currObj.transform.position.y > maxpos)
        {
            maxpos = currObj.transform.position.y;
        }

        transforms.Add(currObj.transform);

        foreach (BVHParser.BVHBone currChildBone in currBone.children)
        {
            iterateChildBone(currChildBone, currObj);
        }
    }
}


public class BVHLoader : MonoBehaviour
{
    BVHParser bvhParser;
    Transform root;
    float size;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void loadSkeleton(string filename)
    {
        bvhParser = new BVHParser(File.ReadAllText(filename));
        
        SkeletonConverter converter = new SkeletonConverter();
        converter.createFromBVH(bvhParser);
        this.size = converter.size;
        root = converter.rootObj.transform;
        foreach(Transform child in this.transform)
        {
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
        root.parent = this.transform;

        UnityEngine.Animations.Rigging.BoneRenderer myBoneRenderer = this.GetComponent<UnityEngine.Animations.Rigging.BoneRenderer>();
        if (converter != null && converter.rootObj != null && myBoneRenderer != null)
            myBoneRenderer.transforms = converter.transforms.ToArray();
    }

    public int getFrames()
    {
        if (bvhParser == null)
            return 1;

        return bvhParser.frames;
    }
    public int setToFrame(float frame, bool centerAnimation, bool alignHips, bool normalizeSkeleton)
    {
        if(bvhParser != null)
        {
            int currFrame = (int)frame;
            float t = frame - (float) currFrame;

            BVHParser.BVHBone currBone = bvhParser.root;

            if (!alignHips)
            {
                Quaternion rot1 = fromEulerYXZ(new Vector3(currBone.channels[3].values[currFrame],
                                                           currBone.channels[4].values[currFrame],
                                                           currBone.channels[5].values[currFrame]));
                Quaternion rot2 = fromEulerYXZ(new Vector3(currBone.channels[3].values[currFrame + 1],
                                                           currBone.channels[4].values[currFrame + 1],
                                                           currBone.channels[5].values[currFrame + 1]));
                root.localRotation = Quaternion.Slerp(rot1, rot2, t);
            } else
            {
                root.localRotation = new Quaternion();
            }
            
            if (!centerAnimation)
            {
                Vector3 pos1 = new Vector3(-currBone.channels[0].values[currFrame] - currBone.offsetX,
                                           currBone.channels[1].values[currFrame] + currBone.offsetY,
                                           currBone.channels[2].values[currFrame] + currBone.offsetZ);
                Vector3 pos2 = new Vector3(-currBone.channels[0].values[currFrame+1] - currBone.offsetX,
                                           currBone.channels[1].values[currFrame+1] + currBone.offsetY,
                                           currBone.channels[2].values[currFrame+1] + currBone.offsetZ);
                root.localPosition = Vector3.Lerp(pos1, pos2, t);
            } else
            {
                root.localPosition = new Vector3();
            }

            if (normalizeSkeleton)
            {
                root.localPosition = root.localPosition / this.size;
            }


            int i = 0;
            
            foreach (BVHParser.BVHBone currChildBone in currBone.children)
            {
                iterateFrameForChildren(currChildBone, root.GetChild(i) , frame, normalizeSkeleton);
                i++;
            }

            if (centerAnimation)
            {
                Transform head = root.Find("LowerBack/Spine/Spine1/Neck/Neck1/Head");
                if (head != null)
                {
                    this.transform.position -= head.position;// + this.transform.position;
                }
            }

            return currFrame;

        }
        return 0;
    }

    public void createKNNRig(string headBoneName, string rHandBoneName, string lHandBoneName, float slidingWindowSizeInMS, float slidingWindowOffsetInMS, float pollingRate, string outputPath)
    {
        float sourceFrameTime = bvhParser.frameTime;
        float targetFrameTime = 1.0f / pollingRate;
        float totalTime = bvhParser.frames * sourceFrameTime;
        int totalNewFrames = (int)(totalTime / targetFrameTime);
        float slidingWindowOffset = slidingWindowOffsetInMS / 1000.0f;
        float slidingWindowSize = slidingWindowSizeInMS / 1000.0f;

        int totalWindows = (int)((totalTime - (slidingWindowSize))/ (slidingWindowOffset));
        int framesPerWindow = (int)((slidingWindowSize) / targetFrameTime);
        //var rHandTree = new KdTree<float, int>(dimensions: 3 * framesPerWindow, new FloatMath());
        //var lHandTree = new KdTree<float, int>(3 * framesPerWindow, new FloatMath());
        var rHandTreeNodes = new List<int>();
        var rHandTreePoints = new List<float[]>(); 
        
        var lHandTreeNodes = new List<int>();
        var lHandTreePoints = new List<float[]>();

        GameObject rootBoneObj = new GameObject();
        KNNBone rootBone = rootBoneObj.AddComponent<KNNBone>();
        
        rootBone.initKNNBone(root.transform);
        rootBone.DivideOffsetsBy(this.size);

        for (int currentWindow = 0; currentWindow < totalWindows; currentWindow++)
        {
            float[] lHandPoints = new float[framesPerWindow * 2];
            float[] rHandPoints = new float[framesPerWindow * 2];

            for (int currFrame = 0; currFrame < framesPerWindow; currFrame++)
            {
                float startTime = slidingWindowOffset * (float)currentWindow;
                float sourceFrame = getSourceFrameFromTargetTime(sourceFrameTime, startTime + (float)currFrame * targetFrameTime);
                setToFrame(sourceFrame, true, true, true);
                Vector3 rHandPosition = root.Find(rHandBoneName).position;
                Vector3 lHandPosition = root.Find(lHandBoneName).position;

                if(sourceFrame >= 1.0f)
                {
                    setToFrame(sourceFrame - 1.0f, true, true, true);
                }

                Vector3 prevRHandPosition = root.Find(rHandBoneName).position;
                Vector3 prevLHandPosition = root.Find(lHandBoneName).position;

                if (sourceFrame >= 2.0f)
                {
                    setToFrame(sourceFrame - 2.0f, true, true, true);
                }

                Vector3 prevPrevRHandPosition = root.Find(rHandBoneName).position;
                Vector3 prevPrevLHandPosition = root.Find(lHandBoneName).position;

                Vector3 lHandVelocity = (lHandPosition - prevLHandPosition) / sourceFrameTime;
                Vector3 rHandVelocity = (rHandPosition - prevRHandPosition) / sourceFrameTime;
                Vector3 prevLHandVelocity = (prevLHandPosition - prevPrevLHandPosition) / sourceFrameTime;
                Vector3 prevRHandVelocity = (prevLHandPosition - prevPrevRHandPosition) / sourceFrameTime;

                Vector3 lHandAcceleration = (lHandVelocity - prevLHandVelocity) / sourceFrameTime;
                Vector3 rHandAcceleration = (rHandVelocity - prevRHandVelocity) / sourceFrameTime;

                SphericalCoords rHandSpPosition = SphericalCoords.CartesianToSpherical(rHandPosition);
                SphericalCoords lHandSpPosition = SphericalCoords.CartesianToSpherical(lHandPosition);

                float rHandAngle = rHandSpPosition.theta * Mathf.Rad2Deg;
                float lHandAngle = lHandSpPosition.theta * Mathf.Rad2Deg;

                //rotate velocity and acceleration angle around "theta"
                lHandVelocity = Quaternion.AngleAxis(-lHandAngle, Vector3.up) * lHandVelocity;
                rHandVelocity = Quaternion.AngleAxis(-rHandAngle, Vector3.up) * rHandVelocity;

                lHandAcceleration = Quaternion.AngleAxis(-lHandAngle, Vector3.up) * lHandAcceleration;
                rHandAcceleration = Quaternion.AngleAxis(-rHandAngle, Vector3.up) * rHandAcceleration;

                rHandSpPosition.theta = 0;
                lHandSpPosition.theta = 0;

                rHandPosition = rHandSpPosition.ToCartesian();
                lHandPosition = lHandSpPosition.ToCartesian();

                int pointOffset = currFrame * 2;
                lHandPoints[pointOffset]      = lHandPosition.x;
                lHandPoints[pointOffset + 1]  = lHandPosition.y;
                //lHandPoints[pointOffset + 2]  = lHandPosition.z;

                rHandPoints[pointOffset]      = rHandPosition.x;
                rHandPoints[pointOffset + 1]  = rHandPosition.y;
                //rHandPoints[pointOffset + 2]  = rHandPosition.z;
            }
            int lastPositionSourceIndex = (int)((currentWindow * slidingWindowOffset + slidingWindowSize) / sourceFrameTime);
            
            setToFrame(lastPositionSourceIndex, true, true, true);
            rootBone.AddRotationsFromTransform(root);

            //lHandTree.Add(lHandPoints, currentWindow);
            //rHandTree.Add(rHandPoints, currentWindow);
            lHandTreePoints.Add(lHandPoints); lHandTreeNodes.Add(currentWindow);
            rHandTreePoints.Add(rHandPoints); rHandTreeNodes.Add(currentWindow);

        }

        var rHandTree = new KDTree<float, int>(3 * framesPerWindow, rHandTreePoints.ToArray(), rHandTreeNodes.ToArray(), Metrics.L2Norm);//KdTree<float, int>(dimensions: 3 * framesPerWindow, points: rHandTreePoints, nodes: rHandTreeNodes, metric: );
        var lHandTree = new KDTree<float, int>(3 * framesPerWindow, lHandTreePoints.ToArray(), lHandTreeNodes.ToArray(), Metrics.L2Norm);
        
        GameObject kNNSkeleton_ = new GameObject("KNN-Skeleton");
        KNNSkeleton finalKNNSkeleton = kNNSkeleton_.AddComponent<KNNSkeleton>();

        finalKNNSkeleton.SetKNNSkeleton(rootBone, lHandTree, rHandTree, framesPerWindow * 3);
        finalKNNSkeleton.Save(outputPath);
        DestroyImmediate(kNNSkeleton_);
        DestroyImmediate(rootBoneObj);

        GameObject kNNRig = new GameObject("KNN-Rig");

        KNNRig kNNRigComponent = kNNRig.AddComponent<KNNRig>();
        kNNRigComponent.skeletonPath = outputPath;
        //kNNRigComponent.InitRig();

    }

    private void iterateFrameForChildren(BVHParser.BVHBone currBone, Transform currTransform, float frame, bool normalizeSkeleton)
    {
        int currFrame = (int)frame;
        float t = frame - (float)currFrame;
        Quaternion rot1 = fromEulerYXZ(new Vector3(currBone.channels[3].values[currFrame],
                                                   currBone.channels[4].values[currFrame],
                                                   currBone.channels[5].values[currFrame]));
        Quaternion rot2 = fromEulerYXZ(new Vector3(currBone.channels[3].values[currFrame + 1],
                                                   currBone.channels[4].values[currFrame + 1],
                                                   currBone.channels[5].values[currFrame + 1]));
        currTransform.localRotation = Quaternion.Slerp(rot1, rot2, t);
        if (currTransform.gameObject.name != currBone.name)
        {
            print("missmatch found");
        }

        if (normalizeSkeleton)
        {
            currTransform.localPosition = new Vector3(-currBone.offsetX, currBone.offsetY, currBone.offsetZ) / this.size;
        } else
        {
            currTransform.localPosition = new Vector3(-currBone.offsetX, currBone.offsetY, currBone.offsetZ);
        }

        int i = 0;
        foreach (BVHParser.BVHBone currChildBone in currBone.children)
        {
            iterateFrameForChildren(currChildBone, currTransform.GetChild(i), frame, normalizeSkeleton);
            i++;
        }
    }

    private Quaternion fromEulerYXZ(Vector3 euler)
    {
        return Quaternion.AngleAxis(wrapAngle(-euler.z), Vector3.forward) * Quaternion.AngleAxis(wrapAngle(-euler.y), Vector3.up) * Quaternion.AngleAxis(wrapAngle(euler.x), Vector3.right);
    }
    private float wrapAngle(float a)
    {
        if (a > 180.0f)
        {
            return a - 360f;
        }
        if (a < -180f)
        {
            return 360f + a;
        }
        return a;
    }

    private float getSourceFrameFromTargetTime(float sourceFrameTime, float targetTime)
    {
        return targetTime / sourceFrameTime;
    }

}