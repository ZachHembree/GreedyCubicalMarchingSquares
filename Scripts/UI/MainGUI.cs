using System;
using UnityEngine;
using UnityEngine.UI;

public class MainGUI : MonoBehaviour
{
    public GameObject importerUI, mapGenUI;
    public Dropdown uiSelector;
    public Button quitButton;

    private Action[] menuSelectors;

    public void Start()
    {
        quitButton.onClick.AddListener(delegate { Application.Quit(); });
        uiSelector.onValueChanged.AddListener(delegate { SelectUI(); });

        menuSelectors = new Action[]
        {
            EnableMapGenUI,
            EnableImporterUI
        };
	}	

    private void SelectUI()
    {
        for (int n = 0; n < menuSelectors.Length; n++)
            if (uiSelector.value == n)
                menuSelectors[n]();
    }

    private void EnableImporterUI()
    {
        mapGenUI.SetActive(false);
        importerUI.SetActive(true);
    }

    private void EnableMapGenUI()
    {
        importerUI.SetActive(false);
        mapGenUI.SetActive(true);
    }
}
