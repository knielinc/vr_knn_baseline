using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;

public struct PosRot
{
    public PosRot(Vector3 pos, Quaternion rot)
    {
        position = pos;
        rotation = rot;
    }

    public Vector3 position { get; }
    public Quaternion rotation { get; }

    public override string ToString() => "position: " + position.ToString() + " , rotation: " + rotation.ToString();
}

public struct RigPosRot
{
    public RigPosRot(PosRot lHand, PosRot rHand, PosRot head)
    {
        this.lHand = lHand;
        this.rHand = rHand;
        this.head = head;
    }

    public PosRot lHand { get; }
    public PosRot rHand { get; }
    public PosRot head { get; }

    public override string ToString() => "lHand: " + lHand.ToString() + " , rHand: " + rHand.ToString() + " , head: " + head.ToString();
}

public class AnimationRecorder : MonoBehaviour
{
    public float updateFrequency;

    public string outputPath;

    public Transform lHand;
    public Transform rHand;
    public Transform head;

    private float startingTime;
    private float lastUpdateTime;

    private LinkedList<PosRot> lHandPosRots; //pos vel acc
    private LinkedList<PosRot> rHandPosRots; //pos vel acc
    private LinkedList<PosRot> headPosRots; //pos vel acc
    private LinkedList<RigPosRot> rigPosRots;

    private Vector3 lastLHandPos;
    private Vector3 lastRHandPos;
    private Vector3 lastHeadPos;
    private Quaternion lastLHandRot;
    private Quaternion lastRHandRot;
    private Quaternion lastHeadRot;

    private bool started = false;

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    // Update is called once per frame
    void Update()
    {
        if (!started && OVRInput.GetDown(OVRInput.RawButton.A))
        {
            StartRecording();
        } else if (started && OVRInput.GetDown(OVRInput.RawButton.A))
        {
            StopRecording();
        }

        if (started)
        {
            UpdatePosRots(lHand, rHand, head);
        }
    }

    private void Init()
    {
        lastUpdateTime = Time.time;

        lastLHandPos = lHand.position;
        lastRHandPos = rHand.position;
        lastHeadPos = head.position;

        lastLHandRot = lHand.rotation;
        lastRHandRot = rHand.rotation;
        lastHeadRot = head.rotation;

        rigPosRots = new LinkedList<RigPosRot>();
    }

    private void StartRecording()
    {
        startingTime = Time.time;
        started = true;
    }

    private void StopRecording()
    {
        started = false;
        Save(outputPath);
        Init();
    }

    private void Save(string outputPath)
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

        stringBuilder.AppendLine("Frequency: " + updateFrequency.ToString() + " Duration: " + (Time.time - startingTime).ToString() + "s");

        foreach(RigPosRot currRigPosRot in rigPosRots)
        {
            stringBuilder.AppendLine("lHandPos: " + currRigPosRot.lHand.position.x + " " + currRigPosRot.lHand.position.y + " " + currRigPosRot.lHand.position.z + " " +
                                     "lHandRot: " + currRigPosRot.lHand.rotation.x + " " + currRigPosRot.lHand.rotation.y + " " + currRigPosRot.lHand.rotation.z + " " + currRigPosRot.lHand.rotation.w);
            stringBuilder.AppendLine("rHandPos: " + currRigPosRot.rHand.position.x + " " + currRigPosRot.rHand.position.y + " " + currRigPosRot.rHand.position.z + " " +
                                     "rHandRot: " + currRigPosRot.rHand.rotation.x + " " + currRigPosRot.rHand.rotation.y + " " + currRigPosRot.rHand.rotation.z + " " + currRigPosRot.rHand.rotation.w);
            stringBuilder.AppendLine("headPos: " + currRigPosRot.head.position.x   + " " + currRigPosRot.head.position.y  + " " + currRigPosRot.head.position.z  + " " +
                                     "headRot: " + currRigPosRot.head.rotation.x   + " " + currRigPosRot.head.rotation.y  + " " + currRigPosRot.head.rotation.z  + " " + currRigPosRot.head.rotation.w);
        }

        return stringBuilder.ToString();
    }

    private void UpdatePosRots(Transform lHand, Transform rHand, Transform head)
    {
        float deltaT = 1.0f / updateFrequency;
        float currTime = Time.time;
        if (currTime - lastUpdateTime >= deltaT)
        {
            float targetTime = lastUpdateTime + deltaT;
            float t = (targetTime - lastUpdateTime) / (currTime - lastUpdateTime);

            Vector3 lHandTargetPos = Vector3.Lerp(lastLHandPos, lHand.position, t);
            Vector3 rHandTargetPos = Vector3.Lerp(lastRHandPos, rHand.position, t);
            Vector3 headTargetPos  = Vector3.Lerp(lastHeadPos,  head.position, t);

            Quaternion lHandTargetRot = Quaternion.Lerp(lastLHandRot, lHand.rotation, t);
            Quaternion rHandTargetRot = Quaternion.Lerp(lastRHandRot, rHand.rotation, t);
            Quaternion headTargetRot =  Quaternion.Lerp(lastHeadRot,  head.rotation, t);

            rigPosRots.AddLast(new RigPosRot(new PosRot(lHandTargetPos, lHandTargetRot),
                                             new PosRot(rHandTargetPos, rHandTargetRot),
                                             new PosRot(headTargetPos, headTargetRot)));

            lastUpdateTime = currTime;
        }
        lastLHandPos = lHand.position;
        lastRHandPos = rHand.position;
        lastHeadPos  = head.position;

        lastLHandRot = lHand.rotation;
        lastRHandRot = rHand.rotation;
        lastHeadRot  = head.rotation;

        lastUpdateTime = currTime;
    }
}
