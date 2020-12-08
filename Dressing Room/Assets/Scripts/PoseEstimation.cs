using UnityEngine;
using TensorFlow;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PoseEstimation : MonoBehaviour
{
    public int Width = 512;
    public int Height = 512;
    public int FPS = 30;

    WebCamTexture webcamTexture;
    Texture2D texture = null;
    int ImageSize = 512;
    PoseNet posenet = new PoseNet();
    PoseNet.Pose[] poses = null;
    float pose_threshold = 0.05f;
    TFSession session;
    TFGraph graph;
    bool isPosing;

    void Start()
    {
        WebCamDevice[] devices = WebCamTexture.devices;
        webcamTexture = new WebCamTexture(devices[0].name, Width, Height, FPS);
        GetComponent<Renderer>().material.mainTexture = webcamTexture;
        webcamTexture.Play();

        TextAsset graphModel = Resources.Load("PoseNet/frozen_model") as TextAsset;
        graph = new TFGraph();
        graph.Import(graphModel.bytes);
        session = new TFSession(graph);

        StartCoroutine("PoseUpdate");
    }

    void Update()
    {
        var color32 = webcamTexture.GetPixels32();

        Texture2D new_texture = new Texture2D(webcamTexture.width, webcamTexture.height);

        new_texture.SetPixels32(color32);
        new_texture.Apply();

        texture = new_texture;

        if (isPosing) return;
        isPosing = true;
    }

    IEnumerator PoseUpdate()
    {
        if (texture != null)
        {
            Texture2D local_texture = scaled(texture, ImageSize, ImageSize);
            var tensor = TransformInput(local_texture.GetPixels32(), ImageSize, ImageSize);
            var runner = session.GetRunner();
            runner.AddInput(graph["image"][0], tensor);
            runner.Fetch(
                graph["heatmap"][0],
                graph["offset_2"][0],
                graph["displacement_fwd_2"][0],
                graph["displacement_bwd_2"][0]
            );

            var result = runner.Run();
            var heatmap = (float[,,,])result[0].GetValue(jagged: false);
            var offsets = (float[,,,])result[1].GetValue(jagged: false);
            var displacementsFwd = (float[,,,])result[2].GetValue(jagged: false);
            var displacementsBwd = (float[,,,])result[3].GetValue(jagged: false);

            poses = posenet.DecodeMultiplePoses(
                heatmap, offsets,
                displacementsFwd,
                displacementsBwd,
                outputStride: 16, maxPoseDetections: 15,
                scoreThreshold: 0.5f, nmsRadius: 20);

            isPosing = false;

            local_texture = null;
            Resources.UnloadUnusedAssets();

            if (poses.Count() > 0)
            {
                PoseNet.Pose detected_pose = poses.Aggregate((maximum, current) => maximum.score > current.score ? maximum : current);
                foreach (PoseNet.Keypoint keypoint in detected_pose.keypoints)
                {
                    Debug.Log(keypoint.part);
                    Debug.Log(keypoint.position[0]);
                    Debug.Log(keypoint.position[1]);
                }
                Debug.Log("");
            }
        }

        yield return new WaitForSeconds(3);

        StartCoroutine("PoseUpdate");
    }

    public static TFTensor TransformInput(Color32[] pic, int width, int height)
    {
        System.Array.Reverse(pic);
        float[] floatValues = new float[width * height * 3];

        for (int i = 0; i < pic.Length; ++i)
        {
            var color = pic[i];
            floatValues[i * 3 + 0] = color.r * (2.0f / 255.0f) - 1.0f;
            floatValues[i * 3 + 1] = color.g * (2.0f / 255.0f) - 1.0f;
            floatValues[i * 3 + 2] = color.b * (2.0f / 255.0f) - 1.0f;

        }

        TFShape shape = new TFShape(1, width, height, 3);

        return TFTensor.FromBuffer(shape, floatValues, 0, floatValues.Length);
    }

    public static Texture2D scaled(Texture2D src, int width, int height, FilterMode mode = FilterMode.Trilinear)
    {
        Rect texR = new Rect(0, 0, width, height);
        _gpu_scale(src, width, height, mode);

        Texture2D result = new Texture2D(width, height, TextureFormat.ARGB32, true);
        result.Resize(width, height);
        result.ReadPixels(texR, 0, 0, true);
        return result;
    }
    static void _gpu_scale(Texture2D src, int width, int height, FilterMode fmode)
    {
        src.filterMode = fmode;
        src.Apply(true);

        RenderTexture rtt = new RenderTexture(width, height, 32);

        Graphics.SetRenderTarget(rtt);

        GL.LoadPixelMatrix(0, 1, 1, 0);

        GL.Clear(true, true, new Color(0, 0, 0, 0));
        Graphics.DrawTexture(new Rect(0, 0, 1, 1), src);
    }
}