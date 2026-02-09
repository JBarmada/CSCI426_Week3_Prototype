using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;
using Mechanics; 

public class NewScrollMechanic : MonoBehaviour, IDropHandler, IDragHandler, IBeginDragHandler, IPointerExitHandler, IPointerEnterHandler
{
    [Header("Test variables")]
    public bool initTest; 
    public bool isInfinite; 
    public string[] testData; 

    [Header("Text prefab")]
    public GameObject templateValues;

    [Header("Required objects")]
    public Camera targetCamera; 
    public RectTransform targetCanvas; 

    public RectTransform contentTarget; 
    public AutoSizeLayoutScrollFlow contentSize; 

    [Header("Settings")]
    [Space(20)]
    public float heightTemplate = 50; 

    public AnimationCurve curve; 
    public AnimationCurve curveShift; 

    public float speedLerp = 5; 
    public float minVelocity = 0.2f; 

    public float shiftUp = 32; 
    public float shiftDown = 32; 
    public float padding = 0; 
    [Range(0, 1)]
    public float colorPad = 0.115f; 
    public float maxFontSize = 48.2f; 

    public bool isElastic = true; 
    public float maxElastic = 50; 

    public float inertiaSense = 4; 

    [Header("Mouse Wheel and Touchpad scroll methods")]
    public bool isCanUseMouseWheel;
    public bool isInvertMouseWheel;
    public float mouseWheelSensibility = 0.5f;
    public float touchpadSensibility = 0.5f;

    // --- NEW AUDIO MODES ---
    public enum PitchMode { SpeedBased, SineWaveLoop, LaunchPattern }

    [Header("Audio Settings")]
    public AudioSource audioSource; 
    public AudioClip tickSound;     
    public PitchMode pitchBehavior = PitchMode.SpeedBased; // Dropdown in Inspector

    [Range(0.1f, 3f)] public float minPitch = 0.9f;
    [Range(0.1f, 3f)] public float maxPitch = 1.2f;

    // Variables for new modes
    [Header("Advanced Audio Tuning")]
    public float loopSpeed = 2f; // How fast the sine wave cycles
    public float launchThreshold = 20f; // Speed needed to trigger "Launch Pattern"
    public float[] launchPattern = new float[] { 1.0f, 1.2f, 1.5f, 1.8f, 2.0f }; // Array of pitches to cycle through
    private int launchPatternIndex = 0;

    private int _lastCenterIndex = -1; 
    private int sessionSeed; 

    bool isDragging;
    float inertia;
    public float Inertia // Property
    {
        get { return inertia; }
        set { inertia = value; }
    }

    float startPosContent;
    float startPosMouse;
    float middle;
    float heightText = 27;

    int countCheck = 4;
    int currentCenter;
    bool isInitialized;
    int countTotal;
    int padCount;
    float _padScroll;

    public float MouseScroll
    {
        get
        {
            float mouseScroll = Input.mouseScrollDelta.y;
            return (mouseScroll != 0) ? mouseScroll : _padScroll;
        }
    }

    public void AddInertia(float amount)
    {
        inertia += amount;
    }

    void OnGUI()
    {
        if (GameMenusManager.Instance != null && GameMenusManager.Instance.IsPaused) return;

        if (Event.current.type == EventType.ScrollWheel)
            _padScroll = (-Event.current.delta.y / 10) * touchpadSensibility;
        else
            _padScroll = 0;
    }

    private void Start()
    {
        sessionSeed = UnityEngine.Random.Range(0, 1000000);
        heightText = heightTemplate / 2;
        middle = GetComponent<RectTransform>().sizeDelta.y / 2;
        contentSize.topPad = middle - heightText;
        contentSize.bottomPad = middle - heightText;
        countCheck = Mathf.CeilToInt((middle * 2) / heightTemplate);
    }

    public void Initialize(List<string> dataToInit, bool isInfinite = false, int firstTarget = 0)
    {
        countTotal = dataToInit.Count;
        for (int i = 0; i < contentTarget.childCount; i++) Destroy(contentTarget.GetChild(i).gameObject);

        this.isInfinite = isInfinite;

        if (isInfinite)
        {
            int half = (int)(countCheck / 2) + 1;
            if (dataToInit.Count > half)
            {
                padCount = half;
                for (int i = dataToInit.Count - half; i < dataToInit.Count; i++) CreatePostObject(dataToInit[i], i);
            }
            else
            {
                padCount = dataToInit.Count;
                for (int j = 0; j < Mathf.CeilToInt((float)half / (float)dataToInit.Count); j++)
                    for (int i = 0; i < dataToInit.Count; i++) CreatePostObject(dataToInit[i], i);
            }
            isElastic = false;
            contentTarget.anchoredPosition = new Vector2(0, (firstTarget + padCount) * (heightText * 2));
        }
        else
        {
            padCount = (int)(countCheck / 2) + 1;
            contentTarget.anchoredPosition = new Vector2(0, firstTarget * (heightText * 2));
        }

        for (int i = 0; i < dataToInit.Count; i++) CreatePostObject(dataToInit[i], i);

        if (isInfinite)
        {
            int half = (int)(countCheck / 2) + 1;
            if (dataToInit.Count > half)
            {
                for (int i = 0; i < half; i++) CreatePostObject(dataToInit[i], i);
            }
            else
            {
                for (int j = 0; j < Mathf.CeilToInt((float)half / (float)dataToInit.Count); j++)
                    for (int i = 0; i < dataToInit.Count; i++) CreatePostObject(dataToInit[i], i);
            }
        }

        contentSize.UpdateLayout();
        isInitialized = true;
    }

    // HELPER TO ATTACH POST INFO
    private void CreatePostObject(string text, int index)
    {
        GameObject instance = Instantiate(templateValues, contentTarget.transform);
        PostInfo postInfo = instance.GetComponent<PostInfo>();
        if (postInfo == null) postInfo = instance.AddComponent<PostInfo>();
        
        var previousState = UnityEngine.Random.state; 
        unchecked { UnityEngine.Random.InitState(sessionSeed + (index * 777)); }
        postInfo.InitializeRandom();
        UnityEngine.Random.state = previousState; 

        var textComponent = instance.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        
        textComponent.text = postInfo.GetDisplayLabel(index);

        instance.name = index + "";
        instance.GetComponent<RectTransform>().sizeDelta = new Vector2(GetComponent<RectTransform>().sizeDelta.x, heightTemplate);
    }

    public GameObject GetCurrentCenterObject()
    {
        if (contentTarget.childCount == 0 || currentCenter < 0 || currentCenter >= contentTarget.childCount) return null;
        return contentTarget.GetChild(currentCenter).gameObject;
    }

    public int ConvertNegativeToSpecial(PostInfo.PostSpecial special, float fraction)
    {
        if (contentTarget == null || fraction <= 0f) return 0;

        var candidates = new List<PostInfo>();
        for (int i = 0; i < contentTarget.childCount; i++)
        {
            var postInfo = contentTarget.GetChild(i).GetComponent<PostInfo>();
            if (postInfo == null) continue;
            if (postInfo.currentType != PostInfo.PostType.Negative) continue;
            if (postInfo.currentSpecial != PostInfo.PostSpecial.None) continue;
            candidates.Add(postInfo);
        }

        int toConvert = Mathf.FloorToInt(candidates.Count * fraction);
        if (toConvert <= 0) return 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            int swapIndex = UnityEngine.Random.Range(i, candidates.Count);
            var temp = candidates[i];
            candidates[i] = candidates[swapIndex];
            candidates[swapIndex] = temp;
        }

        for (int i = 0; i < toConvert; i++)
        {
            var postInfo = candidates[i];
            postInfo.SetSpecial(special);

            var textComponent = postInfo.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            if (textComponent != null)
            {
                int index = 0;
                int.TryParse(postInfo.gameObject.name, out index);
                textComponent.text = postInfo.GetDisplayLabel(index);
            }
        }

        return toConvert;
    }

    public int GetCurrentValue()
    {
        return int.Parse(contentTarget.GetChild(currentCenter).name);
    }

    private void Update()
    {
        if (GameMenusManager.Instance != null && GameMenusManager.Instance.IsPaused) return;

        if (Input.GetMouseButtonUp(0)) isDragging = false;
        if (isCanUseMouseWheel && isInArea && Input.mouseScrollDelta.y != 0) isDragging = true;
        else if (!Input.GetMouseButton(0)) isDragging = false;

        if (initTest)
        {
            initTest = false;
            var newList = new List<string>();
            for (int i = 0; i < testData.Length; i++) newList.Add(testData[i]);
            Initialize(newList, isInfinite);
        }

        if (isInitialized)
        {
            if (!isDragging)
            {
                if (contentTarget.anchoredPosition.y + inertia < 0)
                {
                    if (isElastic)
                    {
                        contentTarget.anchoredPosition = new Vector2(0, contentTarget.anchoredPosition.y + inertia);
                        inertia = inertia * Mathf.Clamp(1 - Mathf.Abs(contentTarget.anchoredPosition.y) / maxElastic, 0, 1);
                    }
                    else
                    {
                        contentTarget.anchoredPosition = new Vector2(0, 0);
                        inertia = 0;
                    }
                }
                else if (contentTarget.anchoredPosition.y + inertia > contentTarget.sizeDelta.y - middle * 2)
                {
                    if (isElastic)
                    {
                        contentTarget.anchoredPosition = new Vector2(0, contentTarget.anchoredPosition.y + inertia);
                        inertia = inertia * Mathf.Clamp(1 - Mathf.Abs((contentTarget.sizeDelta.y - middle * 2) - contentTarget.anchoredPosition.y) / maxElastic, 0, 1);
                    }
                    else
                    {
                        contentTarget.anchoredPosition = new Vector2(0, contentTarget.sizeDelta.y - middle * 2);
                        inertia = 0;
                    }
                }
                else
                {
                    contentTarget.anchoredPosition = new Vector2(0, contentTarget.anchoredPosition.y + inertia);
                    inertia = Mathf.Lerp(inertia, 0, inertiaSense * Time.deltaTime);
                }
            }
            else
            {
                // Mouse Scroll Logic Omitted (Standard)
                if (isCanUseMouseWheel && isInArea && MouseScroll != 0)
                {
                    if(isElastic)
                    {
                         // Basic elasticity logic ...
                         inertia += ((isInvertMouseWheel ? -1 : 1) * MouseScroll * mouseWheelSensibility);
                         contentTarget.anchoredPosition = new Vector2(0, contentTarget.anchoredPosition.y + ((isInvertMouseWheel ? -1 : 1) * MouseScroll * mouseWheelSensibility));
                    } 
                    else 
                    {
                        inertia += ((isInvertMouseWheel ? -1 : 1) * MouseScroll * mouseWheelSensibility);
                         contentTarget.anchoredPosition = new Vector2(0, Mathf.Clamp(contentTarget.anchoredPosition.y + ((isInvertMouseWheel ? -1 : 1) * MouseScroll * mouseWheelSensibility), 0, contentTarget.sizeDelta.y - middle * 2));
                    }
                }
                else
                {
                     // Drag Logic Omitted (Standard)
                     inertia = startPosContent + (-startPosMouse + (Input.mousePosition.y / targetCamera.pixelHeight) * targetCanvas.sizeDelta.y) - contentTarget.anchoredPosition.y;
                     contentTarget.anchoredPosition = new Vector2(0, startPosContent + (-startPosMouse + (Input.mousePosition.y / targetCamera.pixelHeight) * targetCanvas.sizeDelta.y));
                }
            }

            if (isInfinite)
            {
                if (contentTarget.anchoredPosition.y < middle)
                {
                    contentTarget.anchoredPosition = new Vector2(0, contentTarget.anchoredPosition.y + (padCount + (countTotal - padCount)) * (heightText * 2));
                    for (int i = 0; i < (padCount + (countTotal - padCount)); i++)
                        contentTarget.GetChild(i).GetChild(0).GetComponent<TextMeshProUGUI>().fontSize = 0;
                    startPosMouse = (Input.mousePosition.y / targetCamera.pixelHeight) * targetCanvas.sizeDelta.y;
                    startPosContent = contentTarget.anchoredPosition.y;
                }
                else if (contentTarget.anchoredPosition.y > contentTarget.sizeDelta.y - middle * 3)
                {
                    contentTarget.anchoredPosition = new Vector2(0, contentTarget.anchoredPosition.y - (padCount + (countTotal - padCount)) * (heightText * 2));
                    for (int i = contentTarget.childCount - 1; i >= contentTarget.childCount - (padCount + (countTotal - padCount)); i--)
                        contentTarget.GetChild(i).GetChild(0).GetComponent<TextMeshProUGUI>().fontSize = 0;
                    startPosMouse = (Input.mousePosition.y / targetCamera.pixelHeight) * targetCanvas.sizeDelta.y;
                    startPosContent = contentTarget.anchoredPosition.y;
                }
            }

            // Calculation Logic
            float contentPos = contentTarget.anchoredPosition.y;
            currentCenter = Mathf.Clamp(Mathf.RoundToInt(contentPos / (heightText * 2)), 0, contentTarget.childCount - 1);
            
            // --- NEW AUDIO LOGIC ---
            if (currentCenter != _lastCenterIndex)
            {
                if (_lastCenterIndex != -1)
                {
                     if (GameStatsManager.Instance != null) GameStatsManager.Instance.TrackScroll();
                }

                if (_lastCenterIndex != -1 && audioSource != null && tickSound != null)
                {
                    float finalPitch = 1f;

                    // MODE SELECTION
                    switch (pitchBehavior)
                    {
                        case PitchMode.SpeedBased: // ORIGINAL
                            float speedRatio = Mathf.Clamp01(Mathf.Abs(inertia) / 50f);
                            finalPitch = Mathf.Lerp(minPitch, maxPitch, speedRatio);
                            finalPitch += UnityEngine.Random.Range(-0.05f, 0.05f);
                            break;

                        case PitchMode.SineWaveLoop: // IDEA 1: Low-High loop independent of speed
                            // Uses Time.time to create a constant wave
                            float wave = Mathf.Sin(Time.time * loopSpeed); // -1 to 1
                            wave = (wave + 1f) / 2f; // 0 to 1
                            finalPitch = Mathf.Lerp(minPitch, maxPitch, wave);
                            break;

                        case PitchMode.LaunchPattern: // IDEA 2: Pattern when fast
                            if (Mathf.Abs(inertia) > launchThreshold)
                            {
                                // Cycle through the array
                                finalPitch = launchPattern[launchPatternIndex % launchPattern.Length];
                                launchPatternIndex++;
                            }
                            else
                            {
                                // Fallback to normal behavior when slow
                                float slowRatio = Mathf.Clamp01(Mathf.Abs(inertia) / 50f);
                                finalPitch = Mathf.Lerp(minPitch, maxPitch, slowRatio);
                                launchPatternIndex = 0; // Reset pattern when slowed down
                            }
                            break;
                    }

                    audioSource.pitch = finalPitch;
                    audioSource.PlayOneShot(tickSound);
                }
                _lastCenterIndex = currentCenter;
            }
            // -----------------------

            // Visual Curve Update
            int startPoint = Mathf.CeilToInt((contentPos - (middle + heightText)) / (heightText * 2));
            int minID = Mathf.Clamp(Mathf.Max(0, startPoint), 0, int.MaxValue);
            int maxID = Mathf.Clamp(Mathf.Min(contentTarget.transform.childCount, startPoint + countCheck + 1), 0, int.MaxValue);

            if (maxID > minID)
            {
                for (int i = minID; i < maxID; i++)
                {
                    var currentRect = contentTarget.transform.GetChild(i).GetComponent<RectTransform>();
                    var currentText = contentTarget.transform.GetChild(i).GetChild(0).GetComponent<TextMeshProUGUI>();
                    float ratio = Mathf.Clamp(1 - Mathf.Abs(contentPos + currentRect.anchoredPosition.y + middle) / (middle - padding), 0, 1);
                    if (contentPos + currentRect.anchoredPosition.y + middle > 0)
                        currentText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -curveShift.Evaluate(1 - ratio) * shiftUp);
                    else
                        currentText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, curveShift.Evaluate(1 - ratio) * shiftDown);
                    
                    currentText.fontSize = maxFontSize * curve.Evaluate(ratio);
                    currentText.color = new Vector4(currentText.color.r, currentText.color.g, currentText.color.b, Mathf.Clamp((ratio - colorPad) / (1 - colorPad), 0, 1));
                }
            }

            if (Mathf.Abs(inertia) < minVelocity && !Input.GetMouseButton(0))
            {
                inertia = 0;
                contentTarget.anchoredPosition = new Vector2(0, Mathf.Lerp(contentTarget.anchoredPosition.y, -contentTarget.transform.GetChild(currentCenter).GetComponent<RectTransform>().anchoredPosition.y - middle, speedLerp * Time.deltaTime));
            }
        }
    }

    public void OnDrop(PointerEventData eventData) { isDragging = false; }
    public void OnDrag(PointerEventData eventData) { }
    public void OnBeginDrag(PointerEventData eventData) 
    { 
        isDragging = true; 
        startPosMouse = (Input.mousePosition.y / targetCamera.pixelHeight) * targetCanvas.sizeDelta.y;
        startPosContent = contentTarget.anchoredPosition.y;
    }

    bool isInArea = true;
    public void OnPointerEnter(PointerEventData eventData) { isInArea = true; }
    public void OnPointerExit(PointerEventData eventData) { isInArea = false; }
}