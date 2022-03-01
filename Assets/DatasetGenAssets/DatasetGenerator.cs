using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using System.Collections.Generic;

public class DatasetGenerator : MonoBehaviour
{
    // public enum CameraMode { Random, Step };

    [Header("Config")]
    public string datasetPath;

    public Camera cameraShaded;
    public Camera cameraSketch;

    public Transform cameraTarget;
    // public CameraMode cameraUpdateMode; // Can now be read/written from/to toggleRandomizeCamPos.isOn

    // public float hCamStep = 20, vCamStep = 45; //Vertical Step angle [0-90]; horizontal Step angle[0-180]
    // public bool camHalfSphere = false; //Render only the top view of a half sphere
    // public float radius = 4.0f; // radius (distance) around object

    // hCamStep, vCamStep, camHalfSphere and radius are now sliderVCamStep.value, sliderHCamStep.value,
    // toggleCamHalfSphere.isOn, and sliderRadius.value

    private float hCamAngle = 0, vCamAngle = 0;

    [Header("UI elements")]
    public Slider sliderRadius;
    public Slider sliderHCamStep;
    public Slider sliderVCamStep;
    public Toggle toggleCamHalfSphere;
    public Toggle toggleRandomizeCamPos;
    public Toggle toggleRandomizeLightPos;
    public Slider sliderDatasetSize;
    public Slider sliderDelay;
    public Button buttonGenerateDataset;
    public Text progressText;

    private RenderTexture renderTextureShaded;
    private RenderTexture renderTextureSketch;
    private Texture2D bufferedTexShaded;
    private Texture2D bufferedTexSketch;

    private bool isGenerating;
    private int indexOfCurrentImage;
    private float timeOfLastSave;

    private Logger logger;

    // List of camera transforms that will be used to generate the dataset
    [HideInInspector]
    public List<Transform> cameraTransforms = new List<Transform>();

    // Whether cameraTransforms need to be updated
    private bool isDirty = true;

    // Start is called before the first frame update
    void Start()
    {
        if (cameraShaded == null)
        {
            Debug.LogError("Invalid camera for shaded object");
        }
        renderTextureShaded = cameraShaded.targetTexture;
        bufferedTexShaded = new Texture2D(renderTextureShaded.width, renderTextureShaded.height, TextureFormat.RGB24, false);

        if (cameraSketch == null)
        {
            Debug.LogError("Invalid camera for sketch object");
        }
        renderTextureSketch = cameraSketch.targetTexture;
        bufferedTexSketch = new Texture2D(renderTextureSketch.width, renderTextureSketch.height, TextureFormat.RGB24, false);

        logger = GetComponent<Logger>();

        // Set up callbacks for UI events
        sliderRadius.onValueChanged.AddListener(delegate { OnValueChangedRadius(); });
        sliderHCamStep.onValueChanged.AddListener(delegate { OnValueChangedHCamStep(); });
        sliderVCamStep.onValueChanged.AddListener(delegate { OnValueChangedVCamStep(); });
        toggleRandomizeCamPos.onValueChanged.AddListener(delegate { OnValueChangedRandomizeCamPos(); });
        sliderDatasetSize.onValueChanged.AddListener(delegate { OnValueChangeDatasetSize(); });
        sliderDelay.onValueChanged.AddListener(delegate { OnValueChangeDelay(); });
        buttonGenerateDataset.onClick.AddListener(delegate { OnClickButtonGenerate(); });

        // Force updating the labels of the sliders
        OnValueChangedRadius();
        OnValueChangedHCamStep();
        OnValueChangedVCamStep();
        OnValueChangeDatasetSize();
        OnValueChangeDelay();
    }

    void SaveTexture()
    {
        DateTime localDate = DateTime.Now;
        string timeStamp = localDate.ToString("yyyy-MM-dd HH-mm-ss-ffff");

        RenderTexture.active = renderTextureShaded;
        bufferedTexShaded.ReadPixels(new Rect(0, 0, RenderTexture.active.width, RenderTexture.active.height), 0, 0);
        bufferedTexShaded.Apply();
        RenderTexture.active = null;
        var imgPathShaded = Path.Combine(datasetPath, timeStamp + "-shaded.png");
        File.WriteAllBytes(imgPathShaded, bufferedTexShaded.EncodeToPNG());

        RenderTexture.active = renderTextureSketch;
        bufferedTexSketch.ReadPixels(new Rect(0, 0, RenderTexture.active.width, RenderTexture.active.height), 0, 0);
        bufferedTexSketch.Apply();
        RenderTexture.active = null;
        var imgPathSketch = Path.Combine(datasetPath, timeStamp + "-sketch.png");
        File.WriteAllBytes(imgPathSketch, bufferedTexSketch.EncodeToPNG());

        progressText.text = "Generating " + (indexOfCurrentImage + 1) +
                            " of " + sliderDatasetSize.value + " ...\n";
        progressText.text += "Shaded image: " + imgPathShaded + "\n" +
                             "Sketch image: " + imgPathSketch + "\n";

        if (logger)
        {
            logger.LogSample(Path.GetFileName(imgPathShaded), "shaded", cameraShaded.transform);
            logger.LogSample(Path.GetFileName(imgPathSketch), "sketch", cameraSketch.transform);
        }
    }

    void RebuildTransforms()
    {
        hCamAngle = 0;
        vCamAngle = 0;
        cameraTransforms.Clear();

        for (int i = 0; i < sliderDatasetSize.value; ++i)
        {
            GameObject curGO = new GameObject();
            Transform curTransform = curGO.transform;

            if (toggleRandomizeCamPos.isOn)
            {
                curTransform.position = new Vector3(
                    UnityEngine.Random.Range(-4, 4),
                    UnityEngine.Random.Range(-4, 4),
                    UnityEngine.Random.Range(.5f, 2));
                curTransform.LookAt(cameraTarget);
            }
            else
            {
                curTransform.position = cameraTarget.transform.position + new Vector3(0.0f, 0.0f, -sliderRadius.value);
                curTransform.LookAt(cameraTarget);
                curTransform.RotateAround(cameraTarget.transform.position, Vector3.up, hCamAngle);
                curTransform.RotateAround(cameraTarget.transform.position, curTransform.right, vCamAngle);

                hCamAngle += sliderHCamStep.value;
                if (hCamAngle >= 360.0f)
                {
                    if (toggleCamHalfSphere.isOn)
                    {
                        vCamAngle += sliderVCamStep.value;
                    }
                    else if (vCamAngle > 0)
                    {
                        vCamAngle = -vCamAngle;
                    }
                    else
                    {
                        vCamAngle = -vCamAngle + sliderVCamStep.value;
                    }
                    hCamAngle = 0;
                }

                Debug.Log(vCamAngle);
                if (vCamAngle > 90.0f || vCamAngle < -90.0f)
                { //reset angle to prevent "upside down" camera
                    vCamAngle = 0;
                }
            }

            cameraTransforms.Add(curTransform);
        }

        hCamAngle = 0;
        vCamAngle = 0;
    }

    // Set the transform of the shaded and sketch cameras to the given transform
    void SetCameraTransform(Transform transform)
    {
        cameraSketch.transform.position = transform.position;
        cameraSketch.transform.rotation = transform.rotation;

        cameraShaded.transform.position = transform.position;
        cameraShaded.transform.rotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        if (isDirty)
        {
            RebuildTransforms();
            isDirty = false;
        }

        if (isGenerating)
        {
            // Check delay between images
            if ((Time.time - timeOfLastSave) * 1000f > sliderDelay.value)
            {
                SetCameraTransform(cameraTransforms[indexOfCurrentImage]);

                if (toggleRandomizeLightPos.isOn)
                {
                    // TODO: randomize light position
                }

                SaveTexture();

                // Go to next image
                indexOfCurrentImage++;
                if (indexOfCurrentImage == sliderDatasetSize.value)
                {
                    Reset();
                }

                timeOfLastSave = Time.time;
            }
        }
    }

    void Reset()
    {
        isGenerating = false;
        SetEnabledUIElements(true);
        buttonGenerateDataset.GetComponentInChildren<Text>().text =
            "Generate dataset";
        progressText.text = "";

        hCamAngle = 0;
        vCamAngle = 0;

        logger.CloseLog();
    }

    // Set enabled state of all UI elements (except "generate dataset" button)
    void SetEnabledUIElements(bool enabled)
    {
        sliderRadius.enabled = enabled;
        sliderHCamStep.enabled = enabled;
        sliderVCamStep.enabled = enabled;
        toggleCamHalfSphere.enabled = enabled;
        toggleRandomizeCamPos.enabled = enabled;
        toggleRandomizeLightPos.enabled = enabled;
        sliderDatasetSize.enabled = enabled;
        sliderDelay.enabled = enabled;
    }

    // OnValueChange event of "Distance from target" slider
    public void OnValueChangedRadius()
    {
        sliderRadius.value = Mathf.Round(sliderRadius.value / .1f) * .1f;

        Text textComponent = sliderRadius.transform.GetChild(4).GetComponent<Text>();
        if (textComponent == null)
        {
            Debug.LogError("Invalid text component");
        }
        textComponent.text = sliderRadius.value.ToString();

        isDirty = true;
    }

    // OnValueChange event of "Horizontal step angle" slider
    public void OnValueChangedHCamStep()
    {
        sliderHCamStep.value = Mathf.Round(sliderHCamStep.value / 5) * 5;

        Text textComponent = sliderHCamStep.transform.GetChild(4).GetComponent<Text>();
        if (textComponent == null)
        {
            Debug.LogError("Invalid text component");
        }
        textComponent.text = sliderHCamStep.value.ToString();

        isDirty = true;
    }

    // OnValueChange event of "Vertical step angle" slider
    public void OnValueChangedVCamStep()
    {
        sliderVCamStep.value = Mathf.Round(sliderVCamStep.value / 5) * 5;

        Text textComponent = sliderVCamStep.transform.GetChild(4).GetComponent<Text>();
        if (textComponent == null)
        {
            Debug.LogError("Invalid text component");
        }
        textComponent.text = sliderVCamStep.value.ToString();

        isDirty = true;
    }

    // OnValueChange event of "Randomize camera position" checkbox
    public void OnValueChangedRandomizeCamPos()
    {
        sliderRadius.gameObject.SetActive(!toggleRandomizeCamPos.isOn);
        sliderHCamStep.gameObject.SetActive(!toggleRandomizeCamPos.isOn);
        sliderVCamStep.gameObject.SetActive(!toggleRandomizeCamPos.isOn);

        isDirty = true;
    }

    // OnValueChange event of "Dataset size" slider
    public void OnValueChangeDatasetSize()
    {
        sliderDatasetSize.value = Mathf.Round(sliderDatasetSize.value / 5) * 5;

        Text textComponent = sliderDatasetSize.transform.GetChild(4).GetComponent<Text>();
        if (textComponent == null)
        {
            Debug.LogError("Invalid text component");
        }
        textComponent.text = sliderDatasetSize.value.ToString();

        isDirty = true;
    }

    // OnValueChange event of "Delay" slider
    public void OnValueChangeDelay()
    {
        sliderDelay.value = Mathf.Round(sliderDelay.value / 50) * 50;

        Text textComponent = sliderDelay.transform.GetChild(4).GetComponent<Text>();
        if (textComponent == null)
        {
            Debug.LogError("Invalid text component");
        }
        textComponent.text = sliderDelay.value.ToString() + " ms";
    }

    // OnClick event of "Generate dataset" button
    public void OnClickButtonGenerate()
    {
        if (!isGenerating)
        {
            buttonGenerateDataset.GetComponentInChildren<Text>().text = "Stop";
            isGenerating = true;
            indexOfCurrentImage = 0;
            SetEnabledUIElements(false);
        }
        else
        {
            buttonGenerateDataset.GetComponentInChildren<Text>().text = "Generate dataset";
            isGenerating = false;
            SetEnabledUIElements(true);
            progressText.text = "";
        }
    }
}
