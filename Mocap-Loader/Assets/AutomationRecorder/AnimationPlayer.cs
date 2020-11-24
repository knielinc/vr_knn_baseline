using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class AnimationPlayer : MonoBehaviour
{
    private float updateFrequency;
    private LinkedList<RigPosRot> rigPosRots;
    private LinkedListNode<RigPosRot> currRigPosRotEntry;
    private int currRigPosRotIdx;

    public Transform lHand;
    public Transform rHand;
    public Transform head;
    private float lastUpdateTime;

    public string filePath;

    public bool isRunning { get; set; }

    // Start is called before the first frame update
    void Start()
    {
        isRunning = false;
        rigPosRots = new LinkedList<RigPosRot>();
        LoadAnimation(filePath);
        StartAnimation();
    }

    // Update is called once per frame
    void Update()
    {
        if (isRunning)
        {
            UpdateAnimation();
        }
    }

    private void Init()
    {
        lastUpdateTime = Time.time;
    }

    private void SetRig(RigPosRot currRigRotPos)
    {
        lHand.position = currRigRotPos.lHand.position;
        lHand.rotation = currRigRotPos.lHand.rotation;

        rHand.position = currRigRotPos.rHand.position;
        rHand.rotation = currRigRotPos.rHand.rotation;

        head.position = currRigRotPos.head.position;
        head.rotation = currRigRotPos.head.rotation;
    }

    private void StartAnimation()
    {
        Init();
        currRigPosRotEntry = rigPosRots.First;
        currRigPosRotIdx = 0;

        SetRig(currRigPosRotEntry.Value);

        isRunning = true;
    }

    private void StopAnimation()
    {
        isRunning = false;
    }

    private void UpdateAnimation()
    {
        float deltaTime = 1.0f / updateFrequency;

        while(lastUpdateTime + deltaTime < Time.time)
        {
            if(currRigPosRotEntry.Next == null)
            {
                StopAnimation();
                break;
            }
            currRigPosRotEntry = currRigPosRotEntry.Next;
            currRigPosRotIdx++;
            lastUpdateTime += deltaTime;

            SetRig(currRigPosRotEntry.Value);
        }
    }

    private void LoadAnimation(string filePath)
    {
        StreamReader reader = new StreamReader(filePath);
        string currLine = reader.ReadLine();
        string[] currWords = currLine.Split(' ');
        updateFrequency = float.Parse(currWords[1]);

        rigPosRots = new LinkedList<RigPosRot>();

        while (!reader.EndOfStream)
        {
            currLine = reader.ReadLine();
            currWords = currLine.Split(' ');

            Vector3 lHandPos = new Vector3(float.Parse(currWords[1]), float.Parse(currWords[2]), float.Parse(currWords[3]));
            Quaternion lHandRot = new Quaternion(float.Parse(currWords[5]), float.Parse(currWords[6]), float.Parse(currWords[7]), float.Parse(currWords[8]));

            currLine = reader.ReadLine();
            currWords = currLine.Split(' ');

            Vector3 rHandPos = new Vector3(float.Parse(currWords[1]), float.Parse(currWords[2]), float.Parse(currWords[3]));
            Quaternion rHandRot = new Quaternion(float.Parse(currWords[5]), float.Parse(currWords[6]), float.Parse(currWords[7]), float.Parse(currWords[8]));

            currLine = reader.ReadLine();
            currWords = currLine.Split(' ');

            Vector3 headPos = new Vector3(float.Parse(currWords[1]), float.Parse(currWords[2]), float.Parse(currWords[3]));
            Quaternion headRot = new Quaternion(float.Parse(currWords[5]), float.Parse(currWords[6]), float.Parse(currWords[7]), float.Parse(currWords[8]));

            rigPosRots.AddLast(new RigPosRot(new PosRot(lHandPos, lHandRot),
                                             new PosRot(rHandPos, rHandRot),
                                             new PosRot(headPos, headRot)));
        }
    }
}
