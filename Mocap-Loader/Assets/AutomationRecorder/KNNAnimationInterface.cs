using System.Collections;
using System.Collections.Generic;
using UnityEditor.Recorder;
using UnityEngine;

public class KNNAnimationInterface : AnimationInterface
{
    public KNNRig KNNRig;
    // Start is called before the first frame update
    void Start()
    {
        KNNRig.lHandTarget = animationPlayer.lHand;
        KNNRig.rHandTarget = animationPlayer.rHand;
        KNNRig.headTarget = animationPlayer.head;
        KNNRig.rig = targetRig;
    }

    // Update is called once per frame
    void Update()
    {
        if(recorderController.IsRecording() && animationPlayer.isRunning == false)
        {
            recorderController.StopRecording();
        }

        if (!recorderController.IsRecording() && animationPlayer.isRunning == true)
        {
            recorderController.StartRecording();
        }
    }
}
