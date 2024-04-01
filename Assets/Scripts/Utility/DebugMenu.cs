using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugMenu : MonoBehaviour
{
    [SerializeField] private GameObject menu;

    [SerializeField] private Slider timeScaleSlider;
    [SerializeField] private TextMeshProUGUI timeScaleText;

    void Start()
    {
        timeScaleSlider.value = Time.timeScale;
        timeScaleText.text = Time.timeScale.ToString();

        timeScaleSlider.onValueChanged.AddListener(OnTimeScaleChange);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            if (menu.activeSelf)
            {
                menu.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                menu.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    public void OnTimeScaleChange(float value)
    {
        Time.timeScale = value;
        timeScaleText.text = value.ToString();
    }
}
