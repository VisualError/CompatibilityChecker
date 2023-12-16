using CompatibilityChecker.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CompatibilityChecker.MonoBehaviours
{
    public class SearchBox : MonoBehaviour // This isn't being used. This is just here just incase the way im creating the searchbar messes things up and I have to use this instead.
    {
        private TMP_InputField searchInputField;
        private GameObject canvas;
        public string Text = "";

        public void Awake()
        {
            ModNotifyBase.Logger.LogInfo("SearchBox: I have awoken");
        }

        public void Start()
        {
            ModNotifyBase.Logger.LogInfo("SearchBox: Start method called");
            LoadCanvas();
        }

        private void LoadCanvas()
        {
            canvas = GameObject.Find("/Canvas/MenuContainer/LobbyList");

            if (canvas == null)
            {
                ModNotifyBase.Logger.LogError("SearchBox: Canvas not found. Make sure the hierarchy and names are correct.");
                return;
            }

            ModNotifyBase.Logger.LogInfo("SearchBox: Canvas found. Creating search box.");
            CreateSearchBox();
        }

        private void CreateSearchBox()
        {
            ModNotifyBase.Logger.LogInfo("SearchBox: Creating search box");

            // Create an InputField
            searchInputField = CreateInputField("Search..", Vector2.zero); // Centered at (0, 0)

            // Attach a listener to the OnValueChanged event
            searchInputField.onEndEdit.AddListener(OnEndEdit);

            // Set RectTransform properties for the InputField
            RectTransform rectTransform = searchInputField.GetComponent<RectTransform>();

            // Set the size of the InputField (adjust as needed)
            rectTransform.sizeDelta = new Vector2(200f, 30f);
            rectTransform.anchoredPosition = new Vector2(0f, 0f); // Adjust the position as needed

            // Set the anchored position to center of the canvas
            rectTransform.anchoredPosition = new Vector2(canvas.GetComponent<RectTransform>().sizeDelta.x / 2f, canvas.GetComponent<RectTransform>().sizeDelta.y / 2f);

            // Attach the InputField to the canvas
            searchInputField.transform.SetParent(canvas.transform, false);
            searchInputField.gameObject.SetActive(false);
            searchInputField.gameObject.SetActive(true); // funny.
        }

        private TMP_InputField CreateInputField(string placeholder, Vector2 position)
        {
            TextMeshProUGUI reference = GameObject.Find("/Canvas/MenuContainer/LobbyList/ListPanel/ListHeader").GetComponent<TextMeshProUGUI>();
            // Create the TMP_InputField
            GameObject inputFieldGO = new GameObject("SearchInputField");
            RectTransform rectTransform = inputFieldGO.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = position;

            // Add TMP_InputField component
            TMP_InputField inputField = inputFieldGO.AddComponent<TMP_InputField>();
            inputField.interactable = true;

            // Create a child GameObject for TextMeshProUGUI (placeholder)
            GameObject placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(inputFieldGO.transform, false);
            TextMeshProUGUI placeholderText = placeholderGO.AddComponent<TextMeshProUGUI>();
            placeholderText.text = placeholder;
            placeholderText.font = reference.font;
            placeholderText.color = reference.color;              // Adjust color as needed
                                                                 // Set font, font size, or any other properties for TextMeshProUGUI as needed
            inputField.placeholder = placeholderText;

            // Create a child GameObject for TextMeshProUGUI (Text)
            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(inputFieldGO.transform, false);
            TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
            // Set font, font size, or any other properties for TextMeshProUGUI as needed
            text.font = reference.font;
            text.color = reference.color;
            inputField.textComponent = text;

            // Set up the RectTransform properties for the TMP_InputField
            RectTransform inputFieldRectTransform = inputField.GetComponent<RectTransform>();
            inputFieldRectTransform.sizeDelta = new Vector2(200f, 30f); // Adjust the size as needed
            inputFieldRectTransform.anchoredPosition = new Vector2(0f, 0f); // Adjust the position as needed
            
            return inputField;
        }

        private void OnEndEdit(string newValue)
        {
            // Handle the search value change here
            Text = newValue;
            ModNotifyBase.Logger.LogInfo("SearchBox: Search Value Changed - " + newValue);
            SteamLobbyManager lobbyManager = FindObjectOfType<SteamLobbyManager>();
            lobbyManager.LoadServerList();
        }
    }
}