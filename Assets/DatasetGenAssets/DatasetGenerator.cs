using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class DatasetGenerator : MonoBehaviour
{


    [Header("Config")]
    public string datasetPath;

    [Header("Output Image")]
    public int width=512;
    public int height=512;
    [Space(10)]


    private Camera renderCam;
    private RenderTexture renderTex;
    private Texture2D bufferedTex;
    // Start is called before the first frame update
    void Start()
    {

        renderCam = GetComponentInChildren<Camera>();
        if (renderCam == null){
            Debug.Log("Camera not attached to Dataset Generator");
        }

        renderTex = new RenderTexture(width, height, 0);
        bufferedTex = new Texture2D(renderTex.width, renderTex.height, TextureFormat.RGB24, false);

        renderCam.targetTexture = renderTex;
    }

    void SaveTexture()
    {
     RenderTexture.active = renderCam.targetTexture;
     bufferedTex.ReadPixels(new Rect(0, 0, bufferedTex.width, bufferedTex.height), 0, 0);
     bufferedTex.Apply();
     RenderTexture.active = null;
     byte[] bytes = bufferedTex.EncodeToPNG();

     DateTime localDate = DateTime.Now;
     
     var imgPath = Path.Combine(datasetPath, localDate.ToString("yyyy-MM-dd HH-mm-ss-ffff") + ".png");
     //Debug.Log(imgPath);
     File.WriteAllBytes(imgPath, bytes);
    }


    // Update is called once per frame
    void Update()
    {
        SaveTexture();
    }


}
