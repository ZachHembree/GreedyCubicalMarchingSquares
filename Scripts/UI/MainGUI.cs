using System;
using UnityEngine;
using UnityEngine.UI;

public class MainGUI : MonoBehaviour
{
    public GameObject importerUI, mapGenUI, background;
    public Dropdown uiSelector;
    public Button quitButton;

    private RectTransform bgRect;
    private Action[] menuSelectors;

    public void Start()
    {
        quitButton.onClick.AddListener(delegate { Application.Quit(); });
        uiSelector.onValueChanged.AddListener(delegate { SelectUI(); });
        bgRect = background.GetComponent<RectTransform>();

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
        bgRect.anchoredPosition = new Vector2(bgRect.position.x, -400f);
        bgRect.sizeDelta = new Vector2(bgRect.sizeDelta.x, 360f);
    }

    private void EnableMapGenUI()
    {
        importerUI.SetActive(false);
        mapGenUI.SetActive(true);
        bgRect.anchoredPosition = new Vector2(bgRect.position.x, -260f);
        bgRect.sizeDelta = new Vector2(bgRect.sizeDelta.x, 240f);
    }
}
