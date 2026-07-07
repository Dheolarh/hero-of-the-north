using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class WebGLVideoPlayerHelper : MonoBehaviour
{
    [Tooltip("The name of the video file inside StreamingAssets folder")]
    public string videoFileName = "placeholder.mp4";

    void Awake()
    {
        VideoPlayer vp = GetComponent<VideoPlayer>();
        
        vp.source = VideoSource.Url;
        vp.url = System.IO.Path.Combine(Application.streamingAssetsPath, videoFileName);
    }
}
