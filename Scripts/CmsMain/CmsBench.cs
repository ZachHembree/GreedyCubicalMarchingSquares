using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;
using UnityEngine;
using CmsMain;

public class CmsBench : MonoBehaviour
{
    public GameObject BenchUI;
    public int iterations = 100, build;
    public float baseline, redBaseline;
    public string url;
    public int CurrentTest { get; private set; }
    public int Progress { get; private set; }
    public int Score { get; private set; }
    public int RedScore { get; private set; }
    public float Time { get; private set; } = float.MinValue;
    public float RedTime { get; private set; } = float.MinValue;

    private bool abort = false;
    private long ticks, cpuCycles;
    private string benchStartTime, benchEndTime;
    private BenchUI ui;
    private List<List<Octant>[][]> volumes;
    private ConcurrentQueue<Action> methodQueue;

    public void Start()
    {
        ui = BenchUI.GetComponent<BenchUI>();
        GetVolumeData();
    }

    public void Update()
    {
        Action action;

        if (methodQueue != null && methodQueue.Count > 0 && methodQueue.TryDequeue(out action))
            action();

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
            ThreadPool.QueueUserWorkItem(GenerateVolumes, this);
        }
    }

    public void AbortBench() =>
        abort = true;

    private void ResetAbort() =>
        abort = false;

    private void GetVolumeData()
    {
        Volume v = new Volume();

        if (volumes == null)
        {
            volumes = new List<List<Octant>[][]>(12)
            {
                v.GetMeshOctants(GetPrimitiveMesh(PrimitiveType.Sphere), new Vector3(.2f, .2f, .2f)),
                v.GetMeshOctants(GetPrimitiveMesh(PrimitiveType.Cylinder), new Vector3(.05f, .05f, .05f)),
                v.GetMeshOctants(Resources.Load<Mesh>("Scope"), new Vector3(.4f, .4f, .4f)),
                v.GetMeshOctants(Resources.Load<Mesh>("Cabinet"), new Vector3(.09f, .09f, .09f))
            };

            MapGenerator mapGenerator = new MapGenerator(110, 110, 110, false, transform, "");
            volumes.Add(v.GetHeightmapOctants(mapGenerator.GenerateMap(), new Vector3(.25f, .25f, .25f)));
        }
    }

    private Mesh GetPrimitiveMesh(PrimitiveType primitive)
    {
        GameObject gameObject = GameObject.CreatePrimitive(primitive);
        Mesh mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
        Destroy(gameObject);

        return mesh;
    }    

    private static void GenerateVolumes(object obj)
    {
        CmsBench bench = (CmsBench)obj;

        if (bench.volumes != null && !bench.abort)
        {
            Volume v = new Volume();
            System.Diagnostics.Stopwatch 
                time = new System.Diagnostics.Stopwatch(),
                redTime = new System.Diagnostics.Stopwatch();

            bench.methodQueue.Enqueue(() => bench.ui.BenchInProgress());
            time.Start();

            for (bench.Progress = 0; (bench.Progress < bench.iterations) && !bench.abort; bench.Progress++)
            {
                foreach (IList<Octant>[][] volume in bench.volumes)
                {
                    v.ContourMesh(volume, false);
                }
            }

            time.Stop();
            redTime.Start();
            bench.CurrentTest++;

            for (bench.Progress = 0; (bench.Progress < bench.iterations) && !bench.abort; bench.Progress++)
            {
                foreach (IList<Octant>[][] volume in bench.volumes)
                {
                    v.ContourMesh(volume, true);
                }
            }

            redTime.Stop();

            bench.benchEndTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:sszzz");
            bench.Score = time.ElapsedMilliseconds > 0f ? (int)((bench.baseline / time.ElapsedMilliseconds) * 1000f) : int.MaxValue;
            bench.RedScore = redTime.ElapsedMilliseconds > 0f ? (int)((bench.redBaseline / redTime.ElapsedMilliseconds) * 1000f) : int.MaxValue;
            bench.Time = time.ElapsedMilliseconds;
            bench.RedTime = redTime.ElapsedMilliseconds;

            bench.methodQueue.Enqueue(() => bench.ui.BenchUiEnd());
            bench.methodQueue.Enqueue(() => bench.ResetAbort());
            bench.methodQueue.Enqueue(() => bench.WriteResultsToLog());
            bench.methodQueue.Enqueue(() => bench.SendBenchResults());
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
            "Average Clockspeed: " + (cpuCycles / ticks) + "MHz\n" +
            "System Memory: " + SystemInfo.systemMemorySize + "MB\n\n" +
            "Started: " + benchStartTime + "(en-US)\n" +
            "Completed: " + benchEndTime + "\n" +
            "Score: " + Score + "\n" +
            "Reduction Score: " + RedScore + "\n\n";

        string path = Application.dataPath + "benchLog.txt";
        File.AppendAllText(path, results);
    }

    private void SendBenchResults() =>
        StartCoroutine(Post());

    IEnumerator Post()
    {
        WWWForm form = new WWWForm();
        form.AddField("entry.680069692", build);
        form.AddField("entry.49765099", SystemInfo.deviceName);
        form.AddField("entry.332383728", SystemInfo.deviceModel);
        form.AddField("entry.1076755852", SystemInfo.operatingSystem);
        form.AddField("entry.445009460", SystemInfo.processorType);
        form.AddField("entry.1513421479", (cpuCycles / ticks + "MHz"));
        form.AddField("entry.1447181164", SystemInfo.systemMemorySize + "MB");
        form.AddField("entry.983081004", benchStartTime);
        form.AddField("entry.1618691135", benchEndTime);
        form.AddField("entry.1457928902", Score);
        form.AddField("entry.2077377379", RedScore);

        byte[] rawData = form.data;
        WWW www = new WWW(url, rawData);

        yield return www;
    }    
}
