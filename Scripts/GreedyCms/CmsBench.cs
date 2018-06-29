using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;
using UnityEngine;
using GreedyCms;

public class CmsBench : MonoBehaviour
{
    public GameObject BenchUI;
    public int iterations = 100, build;
    public float baseline, redBaseline;
    public string formUrl;
    public string[] formFieldIds;
    public int CurrentTest { get; private set; }
    public int Progress { get; private set; }
    public int Score { get; private set; }
    public int RedScore { get; private set; }
    public float RunTime { get; private set; } = float.MinValue;
    public float RedRunTime { get; private set; } = float.MinValue;

    private bool abort = false;
    private float lastTime;
    private long ticks, cpuCycles;
    private string benchStartTime;
    private BenchUI ui;
    private List<Volume> volumes;
    private Thread bench;
    private ConcurrentQueue<Action> methodQueue;

    public void Start()
    {
        ui = BenchUI.GetComponent<BenchUI>();
        GetVolumeData();
    }

    public void Update()
    {
        Action action;

        while (methodQueue != null && methodQueue.Count > 0 && methodQueue.TryDequeue(out action))
            action();

        if (abort && bench.IsAlive && Time.unscaledDeltaTime - lastTime > 5f)
        {
            bench.Abort();
            EndBench();
        }

        cpuCycles += SystemInfo.processorFrequency;
        ticks++;
    }

    public void StartBench()
    {
        if (volumes != null)
        {
            ticks = 0;
            cpuCycles = 0;
            benchStartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:sszzz");

            Score = int.MinValue;
            abort = false;
            CurrentTest = 1;

            ui.BenchUiStart();
            methodQueue = new ConcurrentQueue<Action>();
            bench = new Thread(GenerateVolumes);
            bench.IsBackground = true;
            bench.Priority = System.Threading.ThreadPriority.Highest; 
            bench.Start();
        }
    }

    public void AbortBench() 
    {
        lastTime = Time.unscaledDeltaTime;
        abort = true;
    }

    private void EndBench()
    {
        if (!abort)
        {
            WriteResultsToLog();
            SendBenchResults();
        }

        abort = false;
    }

    private void GetVolumeData()
    {
        if (volumes == null)
        {
            volumes = new List<Volume>(12)
            {
                new MeshVolume(GetPrimitiveMesh(PrimitiveType.Sphere), new Vector3(.2f, .2f, .2f)),
                new MeshVolume(GetPrimitiveMesh(PrimitiveType.Cylinder), new Vector3(.05f, .05f, .05f)),
                new MeshVolume(Resources.Load<Mesh>("Scope"), new Vector3(.4f, .4f, .4f)),
                new MeshVolume(Resources.Load<Mesh>("Cabinet"), new Vector3(.09f, .09f, .09f))
            };

            MapGenerator mapGenerator = new MapGenerator(110, 110, 110, false, transform, "");
            volumes.Add(new HeightMapVolume(mapGenerator.GenerateMap(), new Vector3(.25f, .25f, .25f)));
        }
    }

    private Mesh GetPrimitiveMesh(PrimitiveType primitive)
    {
        GameObject gameObject = GameObject.CreatePrimitive(primitive);
        Mesh mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
        Destroy(gameObject);

        return mesh;
    }    

    private void GenerateVolumes()
    {
        if (volumes != null && !abort)
        {
            Surface s;
            System.Diagnostics.Stopwatch 
                time = new System.Diagnostics.Stopwatch(),
                redTime = new System.Diagnostics.Stopwatch();

            methodQueue.Enqueue(() => ui.BenchInProgress());
            time.Start();

            for (Progress = 0; (Progress < iterations) && !abort; Progress++)
            {
                foreach (Volume volume in volumes)
                {
                    s = new Surface(volume, false);
                        s.GetMeshData();
                }
            }

            time.Stop();
            redTime.Start();
            CurrentTest++;

            for (Progress = 0; (Progress < iterations) && !abort; Progress++)
            {
                foreach (Volume volume in volumes)
                {
                    s = new Surface(volume, true);
                        s.GetMeshData();
                }
            }

            redTime.Stop();

            Score = time.ElapsedMilliseconds > 0f ? (int)((baseline / time.ElapsedMilliseconds) * 1000f) : int.MaxValue;
            RedScore = redTime.ElapsedMilliseconds > 0f ? (int)((redBaseline / redTime.ElapsedMilliseconds) * 1000f) : int.MaxValue;
            RunTime = time.ElapsedMilliseconds;
            RedRunTime = redTime.ElapsedMilliseconds;

            methodQueue.Enqueue(() => ui.BenchUiEnd());
            methodQueue.Enqueue(() => EndBench());
        }
    }

    private void WriteResultsToLog()
    {
        string results =
            "--Greedy Cubical Marching Squares Benchmark: Build " + build + "--\n" +
            "Device Name: " + SystemInfo.deviceName + "\n" +
            "Device Model: " + SystemInfo.deviceModel + "\n" +
            "Operating System: " + SystemInfo.operatingSystem + "\n" +
            "Processor: " + SystemInfo.processorType + "\n" +
            "Clockspeed: " + (cpuCycles / ticks) + "MHz\n" +
            "System Memory: " + SystemInfo.systemMemorySize + "MB\n\n" +
            "Started: " + benchStartTime + "(en-US)\n" +
            "Runtime: " + RunTime + "\n" +
            "RedRuntime: " + RedRunTime + "\n" +
            "Baseline: " + baseline + "\n" +
            "RedBaseline: " + redBaseline + "\n" +
            "Score: " + Score + "\n" +
            "Reduction Score: " + RedScore + "\n\n";

        string path = Application.dataPath + "/benchLog.txt";
        File.AppendAllText(path, results);
    }

    private void SendBenchResults() =>
        StartCoroutine(Post());

    IEnumerator Post()
    {
        WWWForm form = new WWWForm();
        form.AddField(formFieldIds[0], build); 
        form.AddField(formFieldIds[1], SystemInfo.deviceName);
        form.AddField(formFieldIds[2], SystemInfo.deviceModel);
        form.AddField(formFieldIds[3], SystemInfo.operatingSystem);
        form.AddField(formFieldIds[4], SystemInfo.processorType);
        form.AddField(formFieldIds[5], (cpuCycles / ticks + "MHz"));
        form.AddField(formFieldIds[6], SystemInfo.systemMemorySize + "MB");
        form.AddField(formFieldIds[7], benchStartTime);
        form.AddField(formFieldIds[8], RunTime + "ms");
        form.AddField(formFieldIds[9], RedRunTime + "ms");
        form.AddField(formFieldIds[10], Score);
        form.AddField(formFieldIds[11], RedScore);

        byte[] rawData = form.data;
        WWW www = new WWW(formUrl, rawData);

        yield return www;
    }    
}
