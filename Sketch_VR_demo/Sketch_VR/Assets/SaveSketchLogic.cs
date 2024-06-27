using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.IO;
using System;
using UnityEngine.UI;
using Dummiesman;
using System.Linq;
using UnityEngine.EventSystems;

public class SaveSketchLogic : MonoBehaviour
{
    private GameObject PointLineManager;
    public bool save_meta_info;
    public bool saved;
    //public string filename;
    public PredictionClient client;
    // private string prediction = null;
    private List<string> shapes_list;
    // private int shape_index = 0;
    private bool update = false;
    // private GameObject[] loadedObject;
    public GameObject[] loadedObject;
    private GameObject shape_space;
    private GameObject sketch_space;

    private float size;
    public Slider retrive_sizeSlider;
    [SerializeField] private TextMeshProUGUI saveinfo;

    private GameObject exhibit_space;
    private GameObject ExhibitObject;
    // private int selected = null;
    // [SerializeField] private TextMeshProUGUI modelname;

    private char[] separator = { ',', ' ' };
    // private Int32 count = 10;

    private Vector3 temp = new Vector3(0,0,0.4f);
    private GameObject shape_anchor;
    private GameObject exhibit_shape_anchor;
    private GameObject sketch_anchor;
    private string model_dir;
    private GameObject[] grids;

    private List<int> shape_loc_list = new List<int>() { 2, 1, 3, 0, 4 };
    private OVRGrabbable grab;
    private void Start()
    {
        PointLineManager = GameObject.Find("PointLineManager");
        saved = false;
        shape_space = GameObject.Find("retrieval_space");
        size = retrive_sizeSlider.value;
        // loadedObject = new GameObject();
        loadedObject = new GameObject[5];
        shapes_list = new List<string>();
        shape_anchor = GameObject.Find("retrieve_shape_ReferenceAnchor");
        exhibit_space = GameObject.Find("shape_space");
        exhibit_shape_anchor = GameObject.Find("shape_ReferenceAnchor");
        sketch_anchor = GameObject.Find("sketch_ReferenceAnchor");
        // TODO: change to relative directory
        // model_dir = "D://Ling//03001627";
        // model_dir = "E://data//ShapeNetCore.v2//03001627";
        model_dir = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), @"..\ShapeNetCore.v2\03001627");
        grids = GameObject.FindGameObjectsWithTag("grid");
        sketch_space = GameObject.Find("sketch_space");
        grab = sketch_space.GetComponent<OVRGrabbable>();

    }
    
    private void Update()
    {
        // if (!(String.IsNullOrEmpty(shapes_list)) && update)
        if ((shapes_list!=null) && update)
        {
            Debug.Log("Load Prediction!");

            loadModel();
            exhibit(0);

            shape_space.SetActive(true);

            // grab.M_GrabPoints = shape_space.GetComponentsInChildren<BoxCollider>();
            grab.M_GrabPoints = loadedObject[0].GetComponentsInChildren<BoxCollider>();

            foreach (GameObject go in grids)
            {
                go.SetActive(false);
            }
            update = false;
        }
    }

    private void AddEventTrigger(GameObject shape, int id)
    {
        // BoxCollider BC = shape.AddComponent<BoxCollider>();
        // BC.center = new Vector3(0, 0, 0);
        // Debug.Log("shape.bounds.center: " + shape.transform.localPosition);
        // BC.size = shape.transform.localScale;
        // Debug.Log("shape.bounds.size: " + shape.transform.localScale);

        Renderer[] allRenderers = shape.GetComponentsInChildren<Renderer>();
        foreach(Renderer R in allRenderers)
         {
             R.gameObject.AddComponent<BoxCollider>();
         }
        // Rigidbody gameObjectsRigidBody = shape.AddComponent<Rigidbody>();
        // gameObjectsRigidBody.isKinematic = true;
        if(shape.GetComponent<EventTrigger>() == null)
        {
            EventTrigger trigger = shape.AddComponent<EventTrigger>();
            // EventTrigger trigger = shape.GetComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((setid) => { clicked(id); });
            trigger.triggers.Add(entry);
        }

    }
    public void SaveSketch()
    {
        GameObject[] sketch = GameObject.FindGameObjectsWithTag("Dynamic_Line");
        if (sketch.Length == 0)
        {
            saveinfo.text = "Sketch doesn't exist.";
            Debug.Log("Sketch doesn't exist.");
        }
        else
        {
            string folder = PlayerManager.save_dir;
            string filename = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");

            ObjExporter ObjExporter = GameObject.Find("ObjExporter").GetComponent<ObjExporter>();


            ObjExporter.DoExportsPointsFromGame(sketch, folder, filename);

            Debug.Log("Save a Sketch with " + sketch.Length + " strokes!");

            // if (save_meta_info)
            // {
            //     ObjExporter.DoExportsMetaInfo(PointLineManager.GetComponent<PointLineManager>().all_timestamps, folder, filename);
            // }
            saveinfo.text = "Save:" + filename;
            saved = true;
            //string save_dir = "D://Ling//new version//Sketch_VR//demo_savedir";
            ////string sketch_path = PlayerManager.save_dir + Path.DirectorySeparatorChar + filename + "_sketch.obj";
            //string sketch_path = save_dir + Path.DirectorySeparatorChar + filename + "_sketch.obj";

            //Debug.Log("Save path: " + sketch_path);
            Predict(folder, filename);
        }
    }

    public void ClearSketch()
    {
        Debug.Log("clear all");
        GameObject[] delete = GameObject.FindGameObjectsWithTag("Dynamic_Line");
        int deleteCount = delete.Length;//.Length();
        for (int i = deleteCount - 1; i >= 0; i--)
            Destroy(delete[i]);

        foreach (GameObject go in grids)
        {
            go.SetActive(true);
        }
        grab.M_GrabPoints = sketch_space.GetComponentsInChildren<BoxCollider>();

        shape_space.SetActive(false);
        //Clear timestamps
        // PointLineManager.GetComponent<PointLineManager>().all_timestamps.Clear();
    }

    //public void SearchShape()
    //{
    //    Debug.Log("Search for shapes!");
    //    if (saved == true)
    //    {
    //        sketch_path = folder + Path.DirectorySeparatorChar + filename + "_sketch.obj";
    //        // run python inference script
    //        List result = run_cmd("D://Ling//retrieval_inference//inference.py", "-- " + sketch_path);
    //        Debug.log(result);

    //        // Load 3D shapes according to names
    //        mesh_dir = "";
    //        // Show top-k retrieved shapes in specific space

    //    }
    //}
    private void Predict(string folder, string filename)
    {
        // string save_dir = "D://Ling//new version//Sketch_VR//demo_savedir";
        // string save_dir = PlayerManager.save_dir;
        //string filename = "1a8bbf2994788e2743e99e0cae970928_model_Sketcher_2022-06-07-19-42-12";
        //string sketch_path = PlayerManager.save_dir + Path.DirectorySeparatorChar + filename + "_sketch.obj";
        string sketch_path = folder + Path.DirectorySeparatorChar + filename + "_sketch.obj";
        Debug.Log("Load sketch_path:" + sketch_path);
        //string prediction = null;
        client.Predict(sketch_path, output =>
        {
            //var outputMax = output.Max();
            //var maxIndex = Array.IndexOf(output, outputMax);
            //prediction = "Prediction: " + Convert.ToChar(64 + output);
            // prediction = output;
            Debug.Log("Received prediction: " + output);
            // Using the Method
            // shapes_list = output.Split(separator, count, StringSplitOptions.None);
            shapes_list = output.Split(' ').ToList();
            Debug.Log("shapes list 0: " + shapes_list[0]);
            update = true;
            // loadModel();
            // Debug.Log("Load Prediction 0!");

        }, error =>
        {
            // TODO: when i am not lazy
            Debug.Log("Predict error!");
        });


    }
    private void exhibit(int id)
    {
        // string targetPath = model_dir + Path.DirectorySeparatorChar + shapes_list[id] + Path.DirectorySeparatorChar + "model.obj";
        string targetPath = model_dir + Path.DirectorySeparatorChar + shapes_list[id] + Path.DirectorySeparatorChar + "models//model_normalized.obj";


        if (ExhibitObject != null)
            Destroy(ExhibitObject);
        ExhibitObject = new OBJLoader().Load(targetPath);
        ExhibitObject.transform.SetParent(exhibit_space.transform);
        // ExhibitObject.transform.SetPositionAndRotation(exhibit_shape_anchor.transform.position, exhibit_shape_anchor.transform.rotation);
        ExhibitObject.transform.localPosition = exhibit_shape_anchor.transform.localPosition;
        ExhibitObject.transform.localRotation = exhibit_shape_anchor.transform.localRotation;
        ExhibitObject.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);

        // BoxCollider BC = ExhibitObject.AddComponent<BoxCollider>();
        Renderer[] allRenderers = ExhibitObject.GetComponentsInChildren<Renderer>();
        foreach(Renderer R in allRenderers)
         {
             R.gameObject.AddComponent<BoxCollider>();
         }
        // BC.center = new Vector3(0, 0, 0);
        // Debug.Log("shape.bounds.center: " + shape.transform.localPosition);
        // BC.size = ExhibitObject.transform.localScale;

        Rigidbody gameObjectsRigidBody = ExhibitObject.AddComponent<Rigidbody>();
        gameObjectsRigidBody.mass = 10;
        Debug.Log("Exhibit model from " + targetPath);

    }
    // public void NextModel()
    // {
    //     if (shape_index < shapes_list.Count - 1)
    //     {
    //         // TODO: Load next model in top-5 to show
    //         shape_index = shape_index + 1;
    //         loadModel();
    //     }

    // }
    // public void PreviousModel()
    // {
    //     if (shape_index > 0)
    //     {
    //         // TODO: Load previous model in top-5 to show
    //         shape_index = shape_index - 1;
    //         loadModel();
    //     }

    // }
    public void clicked(int id)
    {
        if(id >= 0 && id < shapes_list.Count)
        {
            Debug.Log( "Clicked!!!   " + shapes_list[id]); 
            // selected = id;
            exhibit(id);
        }
        
// Debug.Log( "Clicked!!!  work "); 
    }
    private void loadModel()
    {
        // Debug.Log( (shape_index + 1) + "/" + shapes_list.Count); 
        // modelname.text = (shape_index + 1) + "/" + shapes_list.Count;


        // loop through the string to extract all other tokens
  


        for (int i = 0; i < shapes_list.Count; i++)
        {
            //file path
        // string targetPath = model_dir + Path.DirectorySeparatorChar + shapes_list[i] + Path.DirectorySeparatorChar + "model.obj";
        string targetPath = model_dir + Path.DirectorySeparatorChar + shapes_list[i] + Path.DirectorySeparatorChar + "models//model_normalized.obj";

        Debug.Log( targetPath ); //printing each token

        // if (!File.Exists(targetPath))
        // {
        //     Debug.LogError("File doesn't exist: " + targetPath);
        // }
        // else
        // {
        //     if (loadedObject != null)
        //         Destroy(loadedObject);
        //     loadedObject = new OBJLoader().Load(targetPath);
        //     //loadedObject.tag = "retrieved";
        //     loadedObject.transform.SetParent(shape_space.transform);
            
        //     // loadedObject[i].transform.SetPositionAndRotation(shape_anchor.transform.localPosition + temp * i, shape_anchor.transform.localRotation);
        //     loadedObject.transform.localPosition = shape_anchor.transform.localPosition + temp * i;
        //     Debug.Log(loadedObject.transform.localPosition);
        //     loadedObject.transform.localRotation = shape_anchor.transform.localRotation;
        //     loadedObject.transform.localScale = new Vector3(size, size, size);
        // }
            if (!File.Exists(targetPath))
            {
                Debug.LogError("File doesn't exist: " + targetPath);
            }
            else
            {
                if (loadedObject[i] != null)
                    Destroy(loadedObject[i]);
                loadedObject[i] = new OBJLoader().Load(targetPath);
                if (i != 0)
                {
                    loadedObject[i].SetActive(false);
                    loadedObject[i].tag = "other_shape";
                }
                //loadedObject.tag = "retrieved";
                loadedObject[i].transform.SetParent(shape_space.transform);
                
                // loadedObject[i].transform.SetPositionAndRotation(shape_anchor.transform.localPosition + temp * i, shape_anchor.transform.localRotation);
                loadedObject[i].transform.localPosition = shape_anchor.transform.localPosition + temp * shape_loc_list[i];
                
                Debug.Log("object " + i + " mapped to " + shape_loc_list[i]);
                loadedObject[i].transform.localRotation = shape_anchor.transform.localRotation;
                loadedObject[i].transform.localScale = new Vector3(size, size, size);

                AddEventTrigger(loadedObject[i], i);
            }
        }

    }
    public void Quit()
    {
        Application.Quit();
    }
}
