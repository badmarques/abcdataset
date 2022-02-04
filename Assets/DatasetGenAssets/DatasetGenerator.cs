using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.UI;

public class DatasetGenerator : MonoBehaviour
{
    [Header("Config")]
    public string datasetPath;

    public Camera cameraShaded;
    public Camera cameraSketch;

    public Transform cameraTarget;

    [Header("UI elements")]
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

    // Start is called before the first frame update
    void Start()
    {
        if (cameraShaded == null) {
            Debug.LogError("Invalid camera for shaded object");
        }
        renderTextureShaded = cameraShaded.targetTexture;
        bufferedTexShaded = new Texture2D(renderTextureShaded.width, renderTextureShaded.height, TextureFormat.RGB24, false);

        if (cameraSketch == null) {
            Debug.LogError("Invalid camera for sketch object");
        }
        renderTextureSketch = cameraSketch.targetTexture;
        bufferedTexSketch = new Texture2D(renderTextureSketch.width, renderTextureSketch.height, TextureFormat.RGB24, false);       

        buttonGenerateDataset.onClick.AddListener(delegate {OnClickButtonGenerate(); });
        sliderDelay.onValueChanged.AddListener(delegate {OnValueChangeDelay(); });
        sliderDatasetSize.onValueChanged.AddListener(delegate {OnValueChangeDatasetSize(); });
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

        progressText.text = "Generating " + (indexOfCurrentImage+1) + 
                            " of " + sliderDatasetSize.value + " ...\n";
        progressText.text += "Shaded image: " + imgPathShaded + "\n" +
                             "Sketch image: " + imgPathSketch + "\n";
    }


    // Update is called once per frame
    void Update()
    {
        if (!isGenerating)
            return;

        // Check delay between images
        if ((Time.time - timeOfLastSave) * 1000f > sliderDelay.value)
        {
            timeOfLastSave = Time.time;

            if(toggleRandomizeCamPos.isOn)
            {
                // TODO: randomize camera position
                cameraShaded.transform.position = new Vector3(
                    UnityEngine.Random.Range(-4, 4),
                    UnityEngine.Random.Range(-4, 4),
                    UnityEngine.Random.Range(.5f, 2));
                cameraShaded.transform.LookAt(cameraTarget);

                cameraSketch.transform.position = cameraShaded.transform.position;
                cameraSketch.transform.rotation = cameraShaded.transform.rotation;
            }

            if(toggleRandomizeLightPos.isOn)
            {
                // TODO: randomize light position
            }            
            
            SaveTexture();

            indexOfCurrentImage++;
            
            if (indexOfCurrentImage == sliderDatasetSize.value)
            {
                isGenerating = false;
                SetEnabledUIElements(true);
                buttonGenerateDataset.GetComponentInChildren<Text>().text =
                    "Generate dataset";
                progressText.text = "";
            }
        }
    }

    // Set enabled state of all UI elements (except "generate dataset" button)
    void SetEnabledUIElements(bool enabled)
    {
        toggleRandomizeCamPos.enabled = enabled;
        toggleRandomizeLightPos.enabled = enabled;
        sliderDatasetSize.enabled = enabled;
        sliderDelay.enabled = enabled;
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

    }
}
