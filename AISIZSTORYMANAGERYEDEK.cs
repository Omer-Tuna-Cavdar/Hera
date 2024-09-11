using UnityEngine;
using TMPro;
using UnityEngine.XR.ARFoundation;
using System.Collections.Generic;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;
using ArtPieceNamespace;

public class ARStoryManager : MonoBehaviour
{
    public ARTrackedImageManager trackedImageManager; // ARTrackedImageManager reference
    public GameObject textPrefab; // Prefab for displaying 3D text (TextMeshPro)
    public GameObject backgroundPrefab; // Prefab for dynamic background (e.g., a Quad or Plane)

    // Reference to the GameObject that contains the four custom buttons in a grid layout
    public GameObject buttonGridPrefab;

    public List<ArtPieceData> artPieces; // List of ArtPieceData ScriptableObjects
    public XRReferenceImageLibrary referenceImageLibrary; // Image library for AR tracked images

    private GameObject descriptionTextInstance; // Instance of 3D text for description
    private GameObject storyTextInstance; // Instance of 3D text for story
    private GameObject quizTextInstance; // Instance of 3D text for quiz
    private GameObject feedbackTextInstance; // Instance for feedback after answering
    private GameObject backgroundInstance; // Instance of background
    private GameObject buttonGridInstance; // Instance of the button grid

    private List<GameObject> buttonInstances = new List<GameObject>(); // Instances of 3D buttons
    private ArtPieceData currentArtPiece; // The currently recognized art piece

    private Dictionary<int, ArtPieceData> artPieceDataMap = new Dictionary<int, ArtPieceData>();
    public Vector3 uiOffset = new Vector3(0, 0.1f, -0.05f); // Offset for positioning UI elements relative to the tracked image
    public float padding = 0.2f; // Padding around text for background resizing

    private int currentQuizQuestionIndex = 0; // Track current quiz question
    private bool quizAnsweredCorrectly = false; // Track if the quiz was answered correctly

    private void Awake()
    {
        InitializeArtPieces();
    }

    private void InitializeArtPieces()
    {
        for (int i = 0; i < referenceImageLibrary.count; i++)
        {
            if (i < artPieces.Count)
            {
                artPieceDataMap[i] = artPieces[i];
            }
        }
    }

    private void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    private void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (ARTrackedImage trackedImage in eventArgs.added)
        {
            HandleImageRecognition(trackedImage);
        }

        foreach (ARTrackedImage trackedImage in eventArgs.updated)
        {
            UpdateUIPosition(trackedImage);
        }

        foreach (ARTrackedImage trackedImage in eventArgs.removed)
        {
            DestroyUI(trackedImage);
        }
    }

    private void HandleImageRecognition(ARTrackedImage trackedImage)
    {
        int trackedImageIndex = GetImageIndex(trackedImage.referenceImage);

        if (trackedImageIndex != -1 && artPieceDataMap.TryGetValue(trackedImageIndex, out currentArtPiece))
        {
            Debug.Log("Recognized art piece: " + currentArtPiece.description);

            currentQuizQuestionIndex = 0; // Reset quiz index when a new art piece is recognized
            quizAnsweredCorrectly = false; // Reset quiz answer status

            // Create and anchor separate UI components
            CreateUIElements(trackedImage);
            UpdateUIForArtPiece(currentArtPiece);
        }
        else
        {
            Debug.LogError("ArtPieceData not found for the recognized image.");
        }
    }

    private void CreateUIElements(ARTrackedImage trackedImage)
{
    // Adjust positions to avoid overlapping
    Vector3 descriptionOffset = new Vector3(0, 0.2f, 0);  // Above the image
    Vector3 storyOffset = new Vector3(0.2f, 0, 0);        // To the right of the image
    Vector3 quizOffset = new Vector3(0, -0.2f, 0);        // Below the image
    Vector3 feedbackOffset = new Vector3(-0.2f, 0, 0);    // To the left of the image
    Vector3 buttonGridOffset = new Vector3(0, -0.5f, 0);  // Below the quiz text, with more space

    // Create and anchor description text
    if (descriptionTextInstance == null)
    {
        descriptionTextInstance = Instantiate(textPrefab, trackedImage.transform.position + descriptionOffset, Quaternion.identity);
        descriptionTextInstance.transform.SetParent(trackedImage.transform, false);
        descriptionTextInstance.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        descriptionTextInstance.transform.localScale = Vector3.one * 0.01f;
        var descriptionTMP = descriptionTextInstance.GetComponent<TextMeshPro>();
        descriptionTMP.alignment = TextAlignmentOptions.Center;
    }

    // Create and anchor story text
    if (storyTextInstance == null)
    {
        storyTextInstance = Instantiate(textPrefab, trackedImage.transform.position + storyOffset, Quaternion.identity);
        storyTextInstance.transform.SetParent(trackedImage.transform, false);
        storyTextInstance.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        storyTextInstance.transform.localScale = Vector3.one * 0.01f;
        var storyTMP = storyTextInstance.GetComponent<TextMeshPro>();
        storyTMP.alignment = TextAlignmentOptions.Center;
    }

    // Create and anchor quiz text
    if (quizTextInstance == null)
    {
        quizTextInstance = Instantiate(textPrefab, trackedImage.transform.position + quizOffset, Quaternion.identity);
        quizTextInstance.transform.SetParent(trackedImage.transform, false);
        quizTextInstance.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        quizTextInstance.transform.localScale = Vector3.one * 0.01f;
        var quizTMP = quizTextInstance.GetComponent<TextMeshPro>();
        quizTMP.alignment = TextAlignmentOptions.Center;
    }

    // Create and anchor feedback text
    if (feedbackTextInstance == null)
    {
        feedbackTextInstance = Instantiate(textPrefab, trackedImage.transform.position + feedbackOffset, Quaternion.identity);
        feedbackTextInstance.transform.SetParent(trackedImage.transform, false);
        feedbackTextInstance.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        feedbackTextInstance.transform.localScale = Vector3.one * 0.01f;
        var feedbackTMP = feedbackTextInstance.GetComponent<TextMeshPro>();
        feedbackTMP.alignment = TextAlignmentOptions.Center;
    }

    // Instantiate and position the button grid
    if (buttonGridInstance == null)
    {
        buttonGridInstance = Instantiate(buttonGridPrefab, trackedImage.transform.position + buttonGridOffset, Quaternion.identity);
        buttonGridInstance.transform.SetParent(trackedImage.transform, false);

        // Make the entire button grid face the camera
        buttonGridInstance.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);

        // Link each button to its corresponding handler using EventTrigger
        SetupButtonHandlers(buttonGridInstance);
    }

    // Create and anchor background
    if (backgroundInstance == null)
    {
        backgroundInstance = Instantiate(backgroundPrefab, trackedImage.transform.position, Quaternion.identity);
        backgroundInstance.transform.SetParent(trackedImage.transform, false);
        backgroundInstance.transform.localScale = new Vector3(0.5f, 0.5f, 0f); // Adjust background size
    }
}


    // Setup each button's EventTrigger for click events
private void SetupButtonHandlers(GameObject buttonGrid)
{
    for (int i = 0; i < 4; i++)
    {
        string buttonName = "Button" + i;
        Transform buttonTransform = buttonGrid.transform.Find(buttonName);

        if (buttonTransform != null)
        {
            GameObject button = buttonTransform.gameObject;

            // Add an EventTrigger component to handle click/tap
            EventTrigger trigger = button.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };

            // Assign the corresponding public method to each button based on index
            switch (i)
            {
                case 0:
                    entry.callback.AddListener((data) => { OnButton0Clicked(); });
                    break;
                case 1:
                    entry.callback.AddListener((data) => { OnButton1Clicked(); });
                    break;
                case 2:
                    entry.callback.AddListener((data) => { OnButton2Clicked(); });
                    break;
                case 3:
                    entry.callback.AddListener((data) => { OnButton3Clicked(); });
                    break;
            }
            trigger.triggers.Add(entry);

            // Ensure the button has a BoxCollider (for raycasting purposes)
            if (button.GetComponent<BoxCollider>() == null)
            {
                button.AddComponent<BoxCollider>();
            }

            // Make the button face the camera (same as text objects)
            button.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        }
        else
        {
            Debug.LogError($"Button {i} not found in button grid.");
        }
    }
}


    // Public parameterless methods for EventTrigger
    public void OnButton0Clicked()
    {
        OnButtonClicked(0);
    }

    public void OnButton1Clicked()
    {
        OnButtonClicked(1);
    }

    public void OnButton2Clicked()
    {
        OnButtonClicked(2);
    }

    public void OnButton3Clicked()
    {
        OnButtonClicked(3);
    }

    // Original OnButtonClicked method, now called by the specific button methods
    private void OnButtonClicked(int selectedAnswerIndex)
    {
        Debug.Log("Button clicked! Answer: " + selectedAnswerIndex);

        bool isCorrect = selectedAnswerIndex == currentArtPiece.correctAnswerIndices[currentQuizQuestionIndex];
        var feedbackTMP = feedbackTextInstance.GetComponent<TextMeshPro>();

        if (isCorrect)
        {
            feedbackTMP.SetText("Correct!");

            if (currentQuizQuestionIndex < currentArtPiece.quizQuestions.Length - 1)
            {
                currentQuizQuestionIndex++;
                UpdateQuizUI();
                UpdateStoryUI();
            }
            else
            {
                feedbackTMP.SetText("Quiz completed!");
            }
        }
        else
        {
            feedbackTMP.SetText("Incorrect. Try again.");
        }
    }

    private void UpdateUIForArtPiece(ArtPieceData artPiece)
    {
        var descriptionTMP = descriptionTextInstance.GetComponent<TextMeshPro>();
        descriptionTMP.SetText(artPiece.description);

        var storyTMP = storyTextInstance.GetComponent<TextMeshPro>();
        storyTMP.SetText(artPiece.storyStages.Length > 0 ? artPiece.storyStages[0] : "No story available.");

        var quizTMP = quizTextInstance.GetComponent<TextMeshPro>();
        if (currentArtPiece.quizQuestions.Length > 0)
        {
            quizTMP.SetText(currentArtPiece.quizQuestions[currentQuizQuestionIndex]);
        }

        var feedbackTMP = feedbackTextInstance.GetComponent<TextMeshPro>();
        feedbackTMP.SetText("");  // Clear feedback text at start

        UpdateQuizUI();
    }

    private void UpdateQuizUI()
    {
        var quizTMP = quizTextInstance.GetComponent<TextMeshPro>();
        if (currentArtPiece.quizQuestions.Length > 0)
        {
            quizTMP.SetText(currentArtPiece.quizQuestions[currentQuizQuestionIndex]);
        }

        for (int i = 0; i < 4; i++)
        {
            var buttonTMP = buttonGridInstance.transform.Find("Button" + i).GetComponentInChildren<TextMeshPro>();
            buttonTMP.SetText(currentArtPiece.quizAnswers[currentQuizQuestionIndex].answers[i]);

            // Force the TextMeshPro component to update its layout
            buttonTMP.ForceMeshUpdate();

            // Get the preferred values for the text
            Vector2 textSize = buttonTMP.GetPreferredValues();

            // Adjust the size of the button based on the text size
            RectTransform buttonRectTransform = buttonTMP.GetComponent<RectTransform>();
            if (buttonRectTransform != null)
            {
                buttonRectTransform.sizeDelta = new Vector2(textSize.x + 20, textSize.y + 10); // Add some padding
            }
        }
    }

    private void UpdateStoryUI()
    {
        var storyTMP = storyTextInstance.GetComponent<TextMeshPro>();
        if (currentArtPiece.storyStages.Length > currentQuizQuestionIndex)
        {
            storyTMP.SetText(currentArtPiece.storyStages[currentQuizQuestionIndex]);
        }
    }

    private void UpdateUIPosition(ARTrackedImage trackedImage)
    {
        // Adjust position offsets for UI elements
        Vector3 descriptionOffset = new Vector3(0, 0.2f, 0);
        Vector3 storyOffset = new Vector3(0.2f, 0, 0);
        Vector3 quizOffset = new Vector3(0, -0.2f, 0);
        Vector3 feedbackOffset = new Vector3(-0.2f, 0, 0);

        if (descriptionTextInstance != null)
        {
            descriptionTextInstance.transform.position = trackedImage.transform.position + uiOffset + descriptionOffset;
        }

        if (storyTextInstance != null)
        {
            storyTextInstance.transform.position = trackedImage.transform.position + uiOffset + storyOffset;
        }

        if (quizTextInstance != null)
        {
            quizTextInstance.transform.position = trackedImage.transform.position + uiOffset + quizOffset;
        }

        if (feedbackTextInstance != null)
        {
            feedbackTextInstance.transform.position = trackedImage.transform.position + uiOffset + feedbackOffset;
        }

        if (buttonGridInstance != null)
        {
            buttonGridInstance.transform.position = trackedImage.transform.position + new Vector3(0, -0.5f, 0); // Position grid slightly below the image
        }

        if (backgroundInstance != null)
        {
            backgroundInstance.transform.position = trackedImage.transform.position;
        }
    }

    private void DestroyUI(ARTrackedImage trackedImage)
    {
        if (descriptionTextInstance != null && descriptionTextInstance.transform.parent == trackedImage.transform)
        {
            Destroy(descriptionTextInstance);
            descriptionTextInstance = null;
        }

        if (storyTextInstance != null && storyTextInstance.transform.parent == trackedImage.transform)
        {
            Destroy(storyTextInstance);
            storyTextInstance = null;
        }

        if (quizTextInstance != null && quizTextInstance.transform.parent == trackedImage.transform)
        {
            Destroy(quizTextInstance);
            quizTextInstance = null;
        }

        if (feedbackTextInstance != null && feedbackTextInstance.transform.parent == trackedImage.transform)
        {
            Destroy(feedbackTextInstance);
            feedbackTextInstance = null;
        }

        if (buttonGridInstance != null && buttonGridInstance.transform.parent == trackedImage.transform)
        {
            Destroy(buttonGridInstance);
            buttonGridInstance = null;
        }

        if (backgroundInstance != null && backgroundInstance.transform.parent == trackedImage.transform)
        {
            Destroy(backgroundInstance);
            backgroundInstance = null;
        }
    }

    private int GetImageIndex(XRReferenceImage referenceImage)
    {
        for (int i = 0; i < referenceImageLibrary.count; i++)
        {
            if (referenceImageLibrary[i].guid == referenceImage.guid)
            {
                return i;
            }
        }
        return -1;
    }
}