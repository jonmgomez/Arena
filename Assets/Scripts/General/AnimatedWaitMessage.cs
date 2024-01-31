using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AnimatedWaitMessage : MonoBehaviour
{
    [Tooltip("Assumes the text end with no period.")]
    [SerializeField] TextMeshProUGUI waitText;
    [SerializeField] float animationInterval = 0.3f;
    [SerializeField] int maxDots = 3;

    String originalText = "";
    int textMaxLength = 0;
    float animationTimer = 0f;

    void Start()
    {
        originalText = waitText.text;
        textMaxLength = originalText.Length + maxDots;
    }

    // Update is called once per frame
    void Update()
    {
        animationTimer += Time.deltaTime;

        if (animationTimer >= animationInterval)
        {
            animationTimer = 0f;
            waitText.text += ".";
            if (waitText.text.Length >= textMaxLength)
                waitText.text = originalText;
        }
    }
}
