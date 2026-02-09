using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System;
using Mechanics; // To access PostInfo

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


    [Header("Audio Settings")]
    public AudioSource audioSource; 
    public AudioClip tickSound;     
    [Range(0.1f, 2f)] public float minPitch = 0.9f;
    [Range(0.1f, 2f)] public float maxPitch = 1.2f;

    private int _lastCenterIndex = -1; 
    private int sessionSeed; // A unique seed for this play session

    bool isDragging;
    float inertia;
    public float Inertia
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

            if (mouseScroll != 0)
                return mouseScroll;
            else
                return _padScroll;
        }
    }

    public void AddInertia(float amount)
    {
        inertia += amount;
    }

    void OnGUI()
    {
        if (Event.current.type == EventType.ScrollWheel)
            _padScroll = (-Event.current.delta.y / 10) * touchpadSensibility;
        else
            _padScroll = 0;
    }

    private void Start()
    {
        // Generate a random seed for this specific play session
        sessionSeed = UnityEngine.Random.Range(0, 1000000);

        heightText = heightTemplate / 2;
        middle = GetComponent<RectTransform>().sizeDelta.y / 2;
        contentSize.topPad = middle - heightText;
        contentSize.bottomPad = middle - heightText;
        countCheck = Mathf.CeilToInt((middle * 2) / heightTemplate);
    }

    // UPDATED INITIALIZE METHOD
    public void Initialize(List<string> dataToInit, bool isInfinite = false, int firstTarget = 0)
    {
        countTotal = dataToInit.Count;
        for (int i = 0; i < contentTarget.childCount; i++)
        {
            Destroy(contentTarget.GetChild(i).gameObject);
        }

        this.isInfinite = isInfinite;

        if (isInfinite)
        {
            int half = (int)(countCheck / 2) + 1;

            if (dataToInit.Count > half)
            {
                padCount = half;
                for (int i = dataToInit.Count - half; i < dataToInit.Count; i++)
                {
                    CreatePostObject(dataToInit[i], i);
                }
            }
            else
            {
                padCount = dataToInit.Count;
                for (int j = 0; j < Mathf.CeilToInt((float)half / (float)dataToInit.Count); j++)
                {

                    for (int i = 0; i < dataToInit.Count; i++)
                    {
                        CreatePostObject(dataToInit[i], i);
                    }
                }
            }
            isElastic = false;
            contentTarget.anchoredPosition = new Vector2(0, (firstTarget + padCount) * (heightText * 2));
        }
        else
        {
            padCount = (int)(countCheck / 2) + 1;
            contentTarget.anchoredPosition = new Vector2(0, firstTarget * (heightText * 2));
        }

        // Main content generation
        for (int i = 0; i < dataToInit.Count; i++)
        {
            CreatePostObject(dataToInit[i], i);
        }

        if (isInfinite)
        {
            int half = (int)(countCheck / 2) + 1;
            if (dataToInit.Count > half)
            {
                for (int i = 0; i < half; i++)
                {
                    CreatePostObject(dataToInit[i], i);
                }
            }
            else
            {
                for (int j = 0; j < Mathf.CeilToInt((float)half / (float)dataToInit.Count); j++)
                {
                    for (int i = 0; i < dataToInit.Count; i++)
                    {
                        CreatePostObject(dataToInit[i], i);
                    }
                }
            }
        }

        contentSize.UpdateLayout();
        isInitialized = true;
    }

    // HELPER TO ATTACH POST INFO
    private void CreatePostObject(string text, int index)
    {
        GameObject instance = Instantiate(templateValues, contentTarget.transform);

        // --- NEW LOGIC: Attach PostInfo and Initialize ---
        PostInfo postInfo = instance.GetComponent<PostInfo>();
        if (postInfo == null) postInfo = instance.AddComponent<PostInfo>();
        
        // FIX: Use SessionSeed + Index to ensure randomness per game, but consistency per index
        var previousState = UnityEngine.Random.state; 
        
        // Use unchecked to allow overflow wrapping safely
        unchecked 
        {
             UnityEngine.Random.InitState(sessionSeed + (index * 777)); 
        }

        postInfo.InitializeRandom();
        UnityEngine.Random.state = previousState; 
        // -------------------------------------

        var textComponent = instance.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        // depending on post type, set different text.
        if(postInfo.currentType == PostInfo.PostType.Gold)
            textComponent.text = $"Gold Post #{index}";
        else if(postInfo.currentType == PostInfo.PostType.Positive)
            textComponent.text = $"Positive Post #{index}";
        else if(postInfo.currentType == PostInfo.PostType.Negative)
            textComponent.text = $"Negative Post #{index}";
        else
            textComponent.text = $"Neutral Post #{index}";

        instance.name = index + "";
        instance.GetComponent<RectTransform>().sizeDelta =
            new Vector2(GetComponent<RectTransform>().sizeDelta.x, heightTemplate);
    }

    public GameObject GetCurrentCenterObject()
    {
        if (contentTarget.childCount == 0 || currentCenter < 0 || currentCenter >= contentTarget.childCount)
            return null;

        return contentTarget.GetChild(currentCenter).gameObject;
    }

    public int GetCurrentValue()
    {
        return int.Parse(contentTarget.GetChild(currentCenter).name);
    }

    private void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        if (isCanUseMouseWheel && isInArea && Input.mouseScrollDelta.y != 0)
        {
            isDragging = true;
        }
        else if (!Input.GetMouseButton(0))
        {
            isDragging = false;
        }

        if (initTest)
        {
            initTest = false;
            var newList = new List<string>();
            for (int i = 0; i < testData.Length; i++)
            {
                newList.Add(testData[i]);
            }
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
                        inertia = inertia * Mathf.Clamp(1 - Mathf.Abs(contentTarget.anchoredPosition.y) /
                               maxElastic, 0, 1);
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
                        inertia = inertia * Mathf.Clamp(1 - Mathf.Abs((contentTarget.sizeDelta.y - middle * 2) -
                        contentTarget.anchoredPosition.y) /
                           maxElastic, 0, 1);
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
                if (isCanUseMouseWheel && isInArea && MouseScroll != 0)
                {
                    if (isElastic)
                    {
                        if (contentTarget.anchoredPosition.y < 0)
                        {
                            inertia = 0;
                            contentTarget.anchoredPosition = new Vector2(0,
                                contentTarget.anchoredPosition.y + ((isInvertMouseWheel ? -1 : 1) * MouseScroll * mouseWheelSensibility)
                                * Mathf.Clamp(1 - Mathf.Abs(contentTarget.anchoredPosition.y) /
                                   maxElastic, 0, 1));
                        }
                        else if (contentTarget.anchoredPosition.y > contentTarget.sizeDelta.y - middle * 2)
                        {
                            inertia = 0;
                            contentTarget.anchoredPosition = new Vector2(0,
                                contentTarget.anchoredPosition.y + ((isInvertMouseWheel ? -1 : 1) * MouseScroll * mouseWheelSensibility)
                                * Mathf.Clamp(1 - Mathf.Abs((contentTarget.sizeDelta.y - middle * 2) -
                                contentTarget.anchoredPosition.y) /
                                   maxElastic, 0, 1));
                        }
                        else
                        {
                            inertia += ((isInvertMouseWheel ? -1 : 1) * MouseScroll
                                * mouseWheelSensibility);
                            contentTarget.anchoredPosition = new Vector2(0,
                                contentTarget.anchoredPosition.y + ((isInvertMouseWheel ? -1 : 1) * MouseScroll * mouseWheelSensibility));
                        }

                    }
                    else
                    {
                        inertia += ((isInvertMouseWheel ? -1 : 1) * MouseScroll
                            * mouseWheelSensibility);
                        contentTarget.anchoredPosition = new Vector2(0, Mathf.Clamp(
                            contentTarget.anchoredPosition.y + ((isInvertMouseWheel ? -1 : 1) * MouseScroll * mouseWheelSensibility),
                            0, contentTarget.sizeDelta.y - middle * 2));
                    }
                }
                else
                {
                    if (isElastic)
                    {
                        if (contentTarget.anchoredPosition.y < 0)
                        {
                            inertia = 0;
                            contentTarget.anchoredPosition = new Vector2(0,
                                startPosContent + (-startPosMouse + (Input.mousePosition.y / targetCamera.pixelHeight)
                                * targetCanvas.sizeDelta.y) * Mathf.Clamp(1 - Mathf.Abs(contentTarget.anchoredPosition.y) /
                                   maxElastic, 0, 1));
                        }
                        else if (contentTarget.anchoredPosition.y > contentTarget.sizeDelta.y - middle * 2)
                        {
                            inertia = 0;
                            contentTarget.anchoredPosition = new Vector2(0,
                                startPosContent + (-startPosMouse + (Input.mousePosition.y / targetCamera.pixelHeight)
                                * targetCanvas.sizeDelta.y) * Mathf.Clamp(1 - Mathf.Abs((contentTarget.sizeDelta.y - middle * 2) -
                                contentTarget.anchoredPosition.y) /
                                   maxElastic, 0, 1));
                        }
                        else
                        {
                            inertia = startPosContent + (-startPosMouse + (Input.mousePosition.y / targetCamera.pixelHeight) * targetCanvas.sizeDelta.y) -
                            contentTarget.anchoredPosition.y;
                            contentTarget.anchoredPosition = new Vector2(0,
                                startPosContent + (-startPosMouse + (Input.mousePosition.y /
                                targetCamera.pixelHeight) * targetCanvas.sizeDelta.y));
                        }

                        startPosMouse = (Input.mousePosition.y / targetCamera.pixelHeight) * targetCanvas.sizeDelta.y;
                        startPosContent = contentTarget.anchoredPosition.y;
                    }
                    else
                    {
                        inertia = startPosContent + (-startPosMouse + (Input.mousePosition.y / targetCamera.pixelHeight) * targetCanvas.sizeDelta.y) -
                            contentTarget.anchoredPosition.y;
                        contentTarget.anchoredPosition = new Vector2(0, Mathf.Clamp(
                            startPosContent + (-startPosMouse + (Input.mousePosition.y /
                            targetCamera.pixelHeight) * targetCanvas.sizeDelta.y), 0, contentTarget.sizeDelta.y - middle * 2));
                    }
                }
            }
            if (isInfinite)
            {
                if (contentTarget.anchoredPosition.y < middle)
                {
                    contentTarget.anchoredPosition = new Vector2(0, contentTarget.anchoredPosition.y +
                        (padCount + (countTotal - padCount)) *
                        (heightText * 2));
                    for (int i = 0; i < (padCount + (countTotal - padCount)); i++)
                    {
                        contentTarget.GetChild(i).GetChild(0).GetComponent<TextMeshProUGUI>().fontSize = 0;
                    }
                    startPosMouse = (Input.mousePosition.y / targetCamera.pixelHeight) * targetCanvas.sizeDelta.y;
                    startPosContent = contentTarget.anchoredPosition.y;
                }
                else if (contentTarget.anchoredPosition.y > contentTarget.sizeDelta.y - middle * 3)
                {
                    contentTarget.anchoredPosition = new Vector2(0, contentTarget.anchoredPosition.y -
                        (padCount + (countTotal - padCount)) *
                        (heightText * 2));
                    for (int i = contentTarget.childCount - 1;
                        i >= contentTarget.childCount -
                        (padCount + (countTotal - padCount)); i--)
                    {
                        contentTarget.GetChild(i).GetChild(0).GetComponent<TextMeshProUGUI>().fontSize = 0;
                    }
                    startPosMouse = (Input.mousePosition.y / targetCamera.pixelHeight) * targetCanvas.sizeDelta.y;
                    startPosContent = contentTarget.anchoredPosition.y;
                }
            }

            float contentPos = contentTarget.anchoredPosition.y;

            int startPoint = Mathf.CeilToInt((contentPos - (middle + heightText)) / (heightText * 2));
            int minID = Mathf.Max(0, startPoint);
            int maxID = Mathf.Min(contentTarget.transform.childCount, startPoint + countCheck + 1);
            minID = Mathf.Clamp(minID, 0, int.MaxValue);
            maxID = Mathf.Clamp(maxID, 0, int.MaxValue);
            
            currentCenter = Mathf.Clamp(Mathf.RoundToInt(contentPos / (heightText * 2)), 0, contentTarget.childCount - 1);
            
            // --- Audio and stats LOGIC HERE ---
            if (currentCenter != _lastCenterIndex)
            {
                // Ensure we only count stats if we have initialized (> -1)
                // and avoid counting the same post rapidly if logic flickers (handled by center check)
                if (_lastCenterIndex != -1)
                {
                     if (GameStatsManager.Instance != null)
                        GameStatsManager.Instance.TrackScroll();
                }

                // audio logic
                if (_lastCenterIndex != -1 && audioSource != null && tickSound != null)
                {
                    float speedRatio = Mathf.Clamp01(Mathf.Abs(inertia) / 50f);
                    audioSource.pitch = Mathf.Lerp(minPitch, maxPitch, speedRatio);
                    audioSource.pitch += UnityEngine.Random.Range(-0.05f, 0.05f);
                    audioSource.PlayOneShot(tickSound);
                }
                _lastCenterIndex = currentCenter;
            }
            // ---------------------------

            if (maxID > minID)
            {
                for (int i = minID; i < maxID; i++)
                {
                    var currentRect = contentTarget.transform.GetChild(i).GetComponent<RectTransform>();
                    var currentText = contentTarget.transform.GetChild(i).GetChild(0).GetComponent<TextMeshProUGUI>();
                    float ratio = Mathf.Clamp(1 - Mathf.Abs(contentPos + currentRect.anchoredPosition.y + middle) / (middle - padding), 0, 1);
                    if (contentPos + currentRect.anchoredPosition.y + middle > 0)
                    {
                        currentText.GetComponent<RectTransform>().anchoredPosition =
                            new Vector2(0, -curveShift.Evaluate(1 - ratio) * shiftUp);
                    }
                    else
                    {
                        currentText.GetComponent<RectTransform>().anchoredPosition =
                            new Vector2(0, curveShift.Evaluate(1 - ratio) * shiftDown);
                    }
                    currentText.fontSize = maxFontSize * curve.Evaluate(ratio);
                    currentText.color = new Vector4(currentText.color.r, currentText.color.g, currentText.color.b,
                        Mathf.Clamp((ratio - colorPad) / (1 - colorPad), 0, 1));
                }
            }

            if (Mathf.Abs(inertia) < minVelocity && !Input.GetMouseButton(0))
            {
                inertia = 0;
                contentTarget.anchoredPosition = new Vector2(0,
                    Mathf.Lerp(contentTarget.anchoredPosition.y, -contentTarget.transform.GetChild(currentCenter).
                    GetComponent<RectTransform>().anchoredPosition.y - middle, speedLerp * Time.deltaTime));
            }
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        isDragging = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        startPosMouse = (Input.mousePosition.y / targetCamera.pixelHeight) * targetCanvas.sizeDelta.y;
        startPosContent = contentTarget.anchoredPosition.y;
    }

    bool isInArea = true;

    public void OnPointerEnter(PointerEventData eventData)
    {
        isInArea = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isInArea = false;
    }
}