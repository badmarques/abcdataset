using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;

[Serializable]
public struct CameraPose{
    public Vector3 position;
    public Quaternion rotation;
}

[Serializable]
public struct Sample{

    public string fileName;
    public string label;
    public CameraPose cameraPose;
}

public class Logger : MonoBehaviour
{
    // Start is called before the first frame update
    private DatasetGenerator datasetGen;
    void Start()
    {
        datasetGen = GetComponent<DatasetGenerator>();
        if(datasetGen == null){
            Debug.Log("logger requires a dataset generator component");
            return;
        }
    }

    public void LogSample(string fileName, string label, Transform cameraPose){

        var sample = new Sample();
        sample.fileName = fileName;
        sample.label = label;
        sample.cameraPose.position = cameraPose.position;
        sample.cameraPose.rotation = cameraPose.rotation;
      
        var jsonSample = JsonUtility.ToJson(sample, true);
        var path = Path.Combine(datasetGen.datasetPath, "log.json");
        var prepend = "";
        if (!File.Exists(path))
        {
            var header = "{\n\"samples\": [\n";
            File.WriteAllText(path, header);
        }else{
            prepend = ",\n";
        }
        
        File.AppendAllText(path, prepend + jsonSample);
        
    }

    public void CloseLog(){
        var path = Path.Combine(datasetGen.datasetPath, "log.json");

        if (!File.Exists(path)){
            Debug.Log("Log file not found");
            return;
        }else{
            var footer = "]\n}";
            File.AppendAllText(path, footer);
        }
    }
}
