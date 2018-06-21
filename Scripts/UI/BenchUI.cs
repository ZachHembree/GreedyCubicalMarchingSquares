using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BenchUI : MonoBehaviour
{
    public GameObject volumeGenerator, start, abort, close;
    public Button startButton, abortButton;
    public Text benchDisplay;

    private CmsBench bench;
    private bool complete = true, inProgress = false, abortCalled = false;

    public void Start ()
    {
		bench = volumeGenerator.GetComponent<CmsBench>();
        startButton.onClick.AddListener(delegate { bench.StartBench(); });
        abortButton.onClick.AddListener(delegate { CallBenchAbort(); });
    }

    public void Update()
    {
        if (inProgress && !complete)
            benchDisplay.text = "Benchmark in progress:\nTest " 
                + bench.CurrentTest + "/2: (" + (int)((float)bench.Progress / bench.iterations * 100f) + "%)";
    }

    public void BenchUiStart()
    {
        if (complete)
        {
            complete = false;

            close.SetActive(false);
            start.SetActive(false);
            abort.SetActive(true);          
        }
    }

    public void BenchInProgress() =>
        inProgress = true;

    public void BenchUiEnd()
    {
        if (!complete)
        {
            inProgress = false;
            close.SetActive(true);
            start.SetActive(true);
            abort.SetActive(false);

            if (abortCalled)
            {
                benchDisplay.text = "Benchmark aborted.";
                abortCalled = false;
            }
            else
            {
                benchDisplay.text =
                    "Summary:"
                    + "\nScore: " + bench.Score
                    + "\nReduction Score: " + bench.RedScore
                    + "\nElapsed Time: " + (bench.Time + bench.RedTime) + "ms";
            }

            complete = true;
        }
    }

    private void CallBenchAbort()
    {
        bench.AbortBench();
        abortCalled = true;
        benchDisplay.text = "Aborting benchmark...";
    }
}
