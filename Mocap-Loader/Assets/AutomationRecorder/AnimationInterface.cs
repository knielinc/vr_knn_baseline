using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

public class AnimationInterface : MonoBehaviour
{
    private RecorderWindow recorder;
    public AnimationPlayer animationPlayer;

    public Transform targetRig;

    public RenderTexture renderTexture = null;

    public RecorderController recorderController;

    // This function gets called when entering Play Mode. We configure the Recorder and start it.
    private void OnEnable()
    {
        if (renderTexture == null)
        {
            //Debug.LogError($"You must assign a valid renderTexture before entering Play Mode");
            renderTexture = new RenderTexture(512, 512, 32);
        }

        RecorderOptions.VerboseMode = true;

        var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        recorderController = new RecorderController(controllerSettings);

        var videoRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        videoRecorder.name = "My Video Recorder";
        videoRecorder.Enabled = true;
        videoRecorder.OutputFile = "recording";
        videoRecorder.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;

        videoRecorder.ImageInputSettings = new RenderTextureInputSettings()
        {
            OutputWidth = renderTexture.width,
            OutputHeight = renderTexture.height,
            FlipFinalOutput = false,
            RenderTexture = renderTexture
        };
        RecorderOptions.VerboseMode = false;
        videoRecorder.AudioInputSettings.PreserveAudio = true;

        controllerSettings.AddRecorderSettings(videoRecorder);
        //controllerSettings.SetRecordModeToFrameInterval(0, 59); // 2s @ 30 FPS
        controllerSettings.SetRecordModeToManual();
        controllerSettings.FrameRate = 60;
        recorderController.PrepareRecording();
        recorderController.StartRecording();
        
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }


}
