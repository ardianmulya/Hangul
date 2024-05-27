using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;



public class BoxManager : MonoBehaviour
{
    [System.Serializable]
    public class Answer
    {
        public List<string> correctImageOrder; // Urutan gambar yang benar
        public List<Button> buttons; // Daftar tombol yang terkait dengan pertanyaan
        public GameObject correctGameObject; // GameObject yang harus aktif untuk jawaban yang benar
    }

    [Header("Box Jawaban")]
    public GameObject[] boxParents; // Array untuk menyimpan parent BOX
    public Text statusText; // Text UI untuk menampilkan status
    private GameObject currentActiveParent; // Parent BOX yang aktif
    private int currentIndex = 0; // Index kotak yang sedang disorot

    [Header("Box huruf Korea")]
    public Sprite[] images; // Array untuk menyimpan gambar
    public GameObject buttonPrefab; // Prefab button yang akan digunakan
    public Transform buttonParent; // Parent tempat button akan dibuat
    public ScrollRect scrollRect;

    [Header("Soal Quiz")]
    public Text questionText; // Referensi ke teks pertanyaan
    private StreamReader reader; // StreamReader untuk membaca file teks
    
    public string questionsFilePath; // Path ke file teks berisi pertanyaan

    [Header("Jawaban")]
    public List<Answer> answers; // Daftar semua pertanyaan
    private int currentQuestionIndex = 0; // Indeks pertanyaan saat ini
    public GameObject parentObject;

    [Header("Benar/Salah")]
    public GameObject Benar;
    public GameObject Salah;

    void Start()
    {
        CreateButtons();
        questionsFilePath = Path.Combine(Application.streamingAssetsPath, "soal.txt");
        LoadQuestionsFromFile();
        // Mulai dengan menampilkan pertanyaan pertama
    }

    void Update()
    {
        UpdateActiveParent();
        HandleMouseInput();
    }

    private void UpdateActiveParent()
    {
        for (int i = 0; i < boxParents.Length; i++)
        {
            if (boxParents[i].activeSelf)
            {
                currentActiveParent = boxParents[i];
                int activeChildCount = 0;
                foreach (Transform child in currentActiveParent.transform)
                {
                    if (child.gameObject.activeSelf)
                    {
                        activeChildCount++;
                    }
                }
                break;
            }
        }
    }

    public void OnBoxClicked(GameObject clickedBox)
    {
        for (int i = 0; i < currentActiveParent.transform.childCount; i++)
        {
            if (clickedBox == currentActiveParent.transform.GetChild(i).gameObject)
            {
                currentIndex = i;
                ActivateHighlight(currentIndex);
                break;
            }
        }
    }

    private void ActivateHighlight(int index)
    {
        int childCount = currentActiveParent.transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = currentActiveParent.transform.GetChild(i);
            Transform highlight = child.Find("Highlight");
            if (highlight != null)
            {
                highlight.gameObject.SetActive(i == index);
            }
        }
    }

    private void CreateButtons()
    {
        for (int i = 0; i < images.Length; i++)
        {
            GameObject newButton = Instantiate(buttonPrefab, buttonParent); // Buat button baru dari prefab
            newButton.GetComponent<Image>().sprite = images[i]; // Set sprite pada image komponen button
            int index = i; // Capture index untuk digunakan dalam lambda

            newButton.GetComponent<Button>().onClick.AddListener(() => OnButtonClick(index));
        }

        // Adjust the scroll view content size
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f; // Reset scroll position to top
    }


    private void OnButtonClick(int index)
    {
        Debug.Log("Button " + index + " clicked.");
        CopyImageToHighlightedBox(images[index]);
    }

    private int FindActiveHighlightIndex()
    {
        if (currentActiveParent != null)
        {
            int childCount = currentActiveParent.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = currentActiveParent.transform.GetChild(i);
                Transform highlight = child.Find("Highlight");
                if (highlight != null && highlight.gameObject.activeSelf)
                {
                    return i;
                }
            }
        }
        return -1; // Return -1 if no active highlight is found
    }

    private void CopyImageToHighlightedBox(Sprite image)
    {
        if (currentActiveParent != null)
        {
            int activeHighlightIndex = FindActiveHighlightIndex();
            if (activeHighlightIndex != -1)
            {
                Transform highlightedBox = currentActiveParent.transform.GetChild(activeHighlightIndex);
                Image boxImage = highlightedBox.GetComponent<Image>();
                if (boxImage != null)
                {
                    boxImage.sprite = image;
                    boxImage.color = Color.white; // Ensure the image is visible (if it was transparent)
                }
            }
        }
    }
    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0)) // Deteksi klik kiri mouse
        {
            Vector2 mousePosition = Input.mousePosition;
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(mousePosition), Vector2.zero);

            if (hit.collider != null)
            {
                Transform clickedTransform = hit.transform;
                for (int i = 0; i < currentActiveParent.transform.childCount; i++)
                {
                    if (clickedTransform == currentActiveParent.transform.GetChild(i))
                    {
                        currentIndex = i;
                        ActivateHighlight(currentIndex);
                        break;
                    }
                }
            }
        }
    }
    void LoadQuestionsFromFile()
    {
        if (File.Exists(questionsFilePath))
        {
            // Buka file teks untuk dibaca
            StartCoroutine(ReadTextFile(questionsFilePath));
        }
        else
        {
            Debug.LogError("Questions file not found at path: " + questionsFilePath);
        }
    }

    private IEnumerator ReadTextFile(string filePath)
    {
        if (filePath.Contains("://") || filePath.Contains(":///"))
        {
            // Membaca file dari URL atau jalur jarak jauh
            using (WWW www = new WWW(filePath))
            {
                yield return www;
                if (string.IsNullOrEmpty(www.error))
                {
                    reader = new StreamReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(www.text)));
                }
                else
                {
                    Debug.LogError("Error reading file from URL: " + www.error);
                    yield break;
                }
            }
        }
        else
        {
            try
            {
                reader = new StreamReader(filePath);
            }
            catch (Exception ex)
            {
                Debug.LogError("Error reading file from local path: " + ex.Message);
                yield break;
            }
        }

        // Menampilkan pertanyaan pertama
        DisplayNextQuestion();
    }


    private void OnDestroy()
    {
        // Pastikan untuk menutup StreamReader ketika objek dihancurkan
        if (reader != null)
        {
            reader.Close();
        }
    }
    public void CheckAnswer()
    {
        Answer currentQuestion = answers[currentQuestionIndex];
        List<string> correctOrder = currentQuestion.correctImageOrder;

        // Cek jumlah button pada pertanyaan
        int numberOfButtons = currentQuestion.buttons.Count;

        // Menggunakan List untuk menyimpan nama gambar dari setiap button
        List<string> buttonNames = new List<string>();

        for (int i = 0; i < numberOfButtons; i++)
        {
            buttonNames.Add(GetImageName(currentQuestion.buttons[i]));
            print(buttonNames[i]);
        }

        // Mengecek apakah urutan gambar benar
        bool isOrderCorrect = true;

        if (correctOrder.Count != buttonNames.Count)
        {
            isOrderCorrect = false;
        }
        else
        {
            for (int i = 0; i < correctOrder.Count; i++)
            {
                if (correctOrder[i] != buttonNames[i])
                {
                    isOrderCorrect = false;
                    break;
                }
            }
        }

        // Mengecek apakah game object yang dimaksud aktif
        bool isGameObjectActive = currentQuestion.correctGameObject != null && currentQuestion.correctGameObject.activeSelf;

        // Log hasil pengecekan
        if (isOrderCorrect)
        {
            Debug.Log("Urutan gambar benar!");
        }
        else
        {
            Debug.Log("Urutan gambar salah!");
        }

        if (isGameObjectActive)
        {
            Debug.Log("GameObject yang benar aktif!");
        }
        else
        {
            Debug.Log("GameObject yang benar tidak aktif!");
        }

        // Final check for answer correctness
        if (isOrderCorrect && isGameObjectActive)
        {
            Debug.Log("Jawaban Benar!");
            StartCoroutine(Activate(Benar, 2));
        }
        else
        {
            Debug.Log("Jawaban Salah!");
            StartCoroutine(Activate(Salah, 2));
        }
        currentQuestionIndex++;
    }

    private IEnumerator Activate(GameObject gameObject, float seconds)
    {
        gameObject.SetActive(true);
        yield return new WaitForSeconds(seconds);
        gameObject.SetActive(false);
    }

    string GetImageName(Button button)
    {
        Image buttonImage = button.GetComponent<Image>();

        if (buttonImage != null && buttonImage.sprite != null)
        {
            return buttonImage.sprite.name;
        }

        return "";
    }
    public void DisplayNextQuestion()
    {
        if (reader != null && !reader.EndOfStream)
        {
            // Baca baris berikutnya dari file teks dan tampilkan sebagai pertanyaan
            string nextQuestion = reader.ReadLine();
            questionText.text = nextQuestion;
        }
        else
        {
            Debug.LogWarning("End of questions file reached.");
        }
        ResetActiveGameObjects();
    }
    void ResetActiveGameObjects()
    {
        // Periksa apakah parentObject tidak null
        if (parentObject != null)
        {
            // Loop melalui semua children dari parentObject
            foreach (Transform child in parentObject.transform)
            {
                // Menonaktifkan setiap child
                child.gameObject.SetActive(false);
            }
        }
    }
}

