using System;
using UnityEngine;
using TensorFlow;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PoseEstimation : MonoBehaviour
{
    public int Width = 512;
    public int Height = 512;
    public int FPS = 2000;

    private Camera camera;

    public Texture2D inputTexture;
    WebCamTexture webcamTexture;
    Texture2D texture = null;
    int ImageSize = 512;
    PoseNet posenet = new PoseNet();
    PoseNet.Pose[] poses = null;
    TFSession session;
    TFGraph graph;
    bool isPosing;

    public GameObject clothMesh = null;

    private static string getJointSuperior(string jointName)
    {
        switch (jointName)
        {
            case "leftEye" : return "none";
            case "leftEar" : return "none";
            case "leftShoulder" : return "leftEye";
            case "leftElbow" : return "leftShoulder";
            case "leftWrist" : return "leftElbow";
            case "leftHip" : return "leftShoulder";
            case "leftKnee" : return "leftHip";
            case "leftAnkle" : return "leftKnee";
            
            case "rightEye" : return "none";
            case "rightEar" : return "none";
            case "rightShoulder" : return "rightEye";
            case "rightElbow" : return "rightShoulder";
            case "rightWrist" : return "rightElbow";
            case "rightHip" : return "rightShoulder";
            case "rightKnee" : return "rightHip";
            case "rightAnkle" : return "rightKnee";
            
            default: return "none";
        }
    }

    void Start()
    {
        this.camera = Camera.main;
        
        WebCamDevice[] devices = WebCamTexture.devices;
        webcamTexture = new WebCamTexture(devices[0].name, Screen.width, Screen.height, FPS);
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

    private static int MAX_AVAILABLE_DEPTH = 30;

    void recParse(Transform jointTransform, Dictionary<string, Vector3> joints, int depth)
    {
        if (depth > PoseEstimation.MAX_AVAILABLE_DEPTH)
        {
            return;
        }
        if (joints.ContainsKey(jointTransform.name))
        {
            jointTransform.position = joints[jointTransform.name];
        }

        for (int i = 0; i < jointTransform.childCount; i++)
        {
            recParse(jointTransform.GetChild(i), joints, depth + 1);
        }
    }

    static Dictionary<string, Vector3> adjustInvalidJoints(Dictionary<string, Vector3> joints)
    {
        List<string> toBeRemoved = new List<string>();
        foreach (var keyValue in joints)
        {
            if( !  joints.ContainsKey(PoseEstimation.getJointSuperior(keyValue.Key))  )
                continue;
            
            if ( keyValue.Value.y > joints[PoseEstimation.getJointSuperior(keyValue.Key)].y)
            {
                toBeRemoved.Add(keyValue.Key);
                // joints.Remove(keyValue.Key);
            }
        }

        foreach (var jointName in toBeRemoved)
        {
            joints.Remove(jointName);
        } 

        return joints;
    }

    static string jointsToString(Dictionary<string, Vector3> joints)
    {
        string str = string.Empty;

        foreach (var kv in joints)
        {
            str += "n=" + kv.Key + "p=" + kv.Value.ToString() + ";";
        }

        return str;
    }
    
    void updateMesh(Dictionary<string, Vector3> joints)
    {
        GameObject p = this.clothMesh;
        PoseEstimation.adjustInvalidJoints(joints);
        Debug.Log(PoseEstimation.jointsToString(joints));
        for (int i = 0; i < p.transform.childCount; i++)
        {
            if (p.transform.GetChild(i).name == "body")
            {
                recParse(p.transform.GetChild(i), joints, 1);
            }
        }

        try
        {
            float bodyWidth = Mathf.Abs(joints["rightShoulder"].x - joints["leftShoulder"].x) * 7500;
            float bodyHeight = (Mathf.Abs(joints["rightShoulder"].y - joints["rightAnkle"].y) + Mathf.Abs(joints["leftShoulder"].y - joints["leftAnkle"].y)) * 350;

            float widthScale = bodyWidth / Screen.width;
            float heightScale = bodyHeight / Screen.height;

            p.transform.localScale = new Vector3(widthScale, heightScale, p.transform.localScale.z);
        }
        catch (Exception) {}
    }

    private class AxisInverter
    {
        private float maxSize;
        private float minSize;

        public AxisInverter(float min, float max)
        {
            this.minSize = min;
            this.maxSize = max;
        }

        public float invert(float pos)
        {
            return this.maxSize - pos + this.minSize;
        }
    }
    
    private class Normalizer
    {
        private float min;
        private float max;
        private float returnStep;

        public Normalizer(float min, float max, float returnStep = 1.0f)
        {
            this.min = min;
            this.max = max;
            this.returnStep = returnStep;
        }

        public float normalize(float val)
        {
            return (val - this.min) / (this.max - this.min) * returnStep;
        }
    }

    IEnumerator PoseUpdate()
    {
        Normalizer xNorm = new Normalizer(0, this.Width, Screen.currentResolution.width);
        Normalizer yNorm = new Normalizer(0, this.Height, Screen.currentResolution.height);

        AxisInverter yAxisInv = new AxisInverter(0, this.Height);
        
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

            string poseStr = string.Empty;

            var jointsDict = new Dictionary<string, Vector3>();

            if (poses.Count() > 0)
            {
                Tuple<float, float> leftEyePos = null;
                Tuple<float, float> rightEyePos = null;
                
                PoseNet.Pose detected_pose = poses.Aggregate((maximum, current) => maximum.score > current.score ? maximum : current);
                foreach (PoseNet.Keypoint keypoint in detected_pose.keypoints)
                {
                    var name = keypoint.part;
                    var x = keypoint.position[0];
                    var y = keypoint.position[1];

                    if (name == "leftWrist")
                    {
                        Debug.Log(xNorm.normalize(x).ToString() + ", " + yNorm.normalize(y).ToString());
                    }

                    var xCam = this.camera.ScreenToWorldPoint(new Vector3(xNorm.normalize(x), yNorm.normalize(yAxisInv.invert(y)), camera.nearClipPlane + 0.5f));
                 
                    if(name == "leftEar" || name == "rightEar")
                        continue;

                    if (name == "leftEye")
                        leftEyePos = new Tuple<float, float>(x, y);

                    if (name == "rightEye")
                        rightEyePos = new Tuple<float, float>(x, y);
                    
                    jointsDict.Add(name, xCam);
                    
                }

                if (leftEyePos != null && rightEyePos != null)
                {
                    // Debug.Log(Screen.currentResolution.ToString());
                    // Debug.Log(leftEyePos.Item1.ToString() + ", " + rightEyePos.Item1.ToString());
                    
                    float xHead = (xNorm.normalize(leftEyePos.Item1) + xNorm.normalize(rightEyePos.Item1)) / 2;
                    float yHead = (yNorm.normalize(yAxisInv.invert(leftEyePos.Item2)) + yNorm.normalize(yAxisInv.invert(rightEyePos.Item2))) / 2 - 60f;

                    // Debug.Log(xHead.ToString() + ", " + yHead.ToString());

                    jointsDict.Add("body", this.camera.ScreenToWorldPoint(new Vector3(xHead, yHead, camera.nearClipPlane + 0.5f)));
                }
            }
            this.updateMesh(jointsDict);
        }


        yield return new WaitForSeconds(1.5f);

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