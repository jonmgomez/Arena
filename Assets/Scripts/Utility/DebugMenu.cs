using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DebugMenu : UIMenu
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
            TrySetMenuEnabled();
        }
    }

    public override void SetMenuEnabled(bool enabled)
    {
        menu.SetActive(enabled);

        Player player = GameState.Instance.GetLocalPlayer();
        if (player != null)
        {
            player.SetEnableControls(!enabled);
        }
    }

    public void OnTimeScaleChange(float value)
    {
        Time.timeScale = value;
        timeScaleText.text = value.ToString();
    }
}
