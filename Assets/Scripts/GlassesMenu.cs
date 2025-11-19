using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class GlassesMenu : MonoBehaviour
{
    private string apiUrl;
    private string apiUrlSecondary;
    private int petId;

    public GameObject[] toggles;
    public GameObject[] pricePanel;
    private bool[] bought;
    private int selected;
    public GameObject notEnoughCoinsPanel;
    public GameObject ConfirmPanel;
    public TMP_Text Confirmtext;
    public Button ConfirmTransact;
    public TMP_Text[] Coins;
    public SoundManager SM;
    public GameObject Glasses;
    private bool isTogglingProgrammatically = false;

    private string[] glassesName = new string[] { "BLACK GLASSES", "CATEYE GLASSES", "RIZZ GLASSES", "FUNNY GLASSES" };

    private void Start()
    {
        bought = new bool[4];
        
        string petKey = PlayerPrefKeys.PetPrefix;
        apiUrl = PlayerPrefs.GetString(PlayerPrefKeys.API_URL);
        apiUrlSecondary = PlayerPrefs.GetString(PlayerPrefKeys.API_URL_Secondary);
        petId = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetID);

        for (int i = 0; i < toggles.Length; i++)
        {
            int index = i;
            Toggle toggle = toggles[i].GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener((isOn) => OnToggle(index));
            }
        }

        StartCoroutine(GetAccessories());
    }

    private IEnumerator GetAccessories()
    {
        string url = apiUrlSecondary + "/pets/" + petId + "/accessories";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Accessory[] accessories = JsonUtility.FromJson<AccessoryList>("{\"items\":" + responseText + "}").items;

                if (accessories == null || accessories.Length == 0)
                {
                    HideAllToggles();
                    ShowAllPricePanels();
                    
                    for (int i = 0; i < bought.Length; i++)
                    {
                        bought[i] = false;
                    }

                    yield break;
                }

                RemoveAllToggleListeners();

                for (int i = 0; i < accessories.Length; i++)
                {
                    Accessory accessory = accessories[i];

                    if (accessory.accessory_category == "glasses" && accessory.accessory_id >= 5 && accessory.accessory_id <= 8)
                    {
                        int toggleIndex = accessory.accessory_id - 5;
                        int pricePanelIndex = accessory.accessory_id - 5;

                        if (accessory.accessory_category != null)
                        {
                            toggles[toggleIndex].SetActive(true);
                            
                            Toggle toggle = toggles[toggleIndex].GetComponent<Toggle>();
                            if (toggle != null)
                            {
                                toggle.isOn = false;
                                toggle.interactable = true;
                            }
                            
                            pricePanel[pricePanelIndex].SetActive(false);
                            bought[accessory.accessory_id - 5] = true;
                        }
                        else
                        {
                            toggles[toggleIndex].SetActive(false);
                            pricePanel[pricePanelIndex].SetActive(true);
                            bought[accessory.accessory_id - 5] = false;
                        }
                    }
                }

                AddAllToggleListeners();
            }
            else
            {
                Debug.LogError("Error: " + request.error);
            }
        }
    }

    public void ShowConfirm(int glasses)
    {
        if (bought[glasses])
        {
            return;
        }

        selected = glasses;
        ConfirmPanel.SetActive(true);
        Confirmtext.text = $"Are you sure you want to buy {glassesName[selected]} for 35 Coins?";
    }

    public void OnConfirm()
    {
        if(!gameObject.activeInHierarchy)
        {
            return;
        }
        ReduceCoinsAndUpdateAccessory();
    }

    private void ReduceCoinsAndUpdateAccessory()
    {
        string petKey = PlayerPrefKeys.PetPrefix;
        int currentCoins = PlayerPrefs.GetInt(petKey + PlayerPrefKeys.PetCoins);
        int accessoryCost = 35;

        if (currentCoins >= accessoryCost)
        {
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetCoins, currentCoins - accessoryCost);
            SM.PlayCoinSound();

            string url = $"{apiUrlSecondary}/pets/{petId}/buyAccessory";
            string jsonData = JsonUtility.ToJson(new AccPayload { accessory_id = selected + 5 });
            
            byte[] byteData = System.Text.Encoding.UTF8.GetBytes(jsonData);
            UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT)
            {
                uploadHandler = new UploadHandlerRaw(byteData),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");

            StartCoroutine(SendRequest(request, currentCoins));
        }
        else
        {
            ConfirmPanel.SetActive(false);
            ShowNotEnoughCoinsPanel();
        }
    }

    private IEnumerator SendRequest(UnityWebRequest request, int currentCoins)
    {
        yield return request.SendWebRequest();
    
        if (request.result == UnityWebRequest.Result.Success)
        {
            string petkey = PlayerPrefKeys.PetPrefix;
            
            bought[selected] = true;
    
            RemoveAllToggleListeners();
    
            for (int i = 0; i < toggles.Length; i++)
            {
                if (toggles[i] != null)
                {
                    Toggle toggle = toggles[i].GetComponent<Toggle>();
                    if (toggle != null)
                    {
                        toggle.isOn = false;
                        toggle.interactable = true;
                    }
                }
            }
            
            toggles[selected].SetActive(true);
            
            Toggle purchasedToggle = toggles[selected].GetComponent<Toggle>();
            if (purchasedToggle != null)
            {
                purchasedToggle.isOn = true;
                purchasedToggle.interactable = true;
            }

            pricePanel[selected].SetActive(false);

            AddAllToggleListeners();
    
            PlayerPrefs.SetInt(petkey + PlayerPrefKeys.PetEyes, selected + 5);
            PlayerPrefs.Save();
            StartCoroutine(UpdateAccessoryOnServer(selected + 5));
            
            Glasses.SetActive(true);
    
            foreach (var coin in Coins)
            {
                coin.text = PlayerPrefs.GetInt(petkey + PlayerPrefKeys.PetCoins).ToString();
            }
        }
        else
        {
            Debug.LogError("Error purchasing accessory: " + request.error);
            string petKey = PlayerPrefKeys.PetPrefix;
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetCoins, currentCoins);
            PlayerPrefs.Save();
        }
        ConfirmPanel.SetActive(false);
    }

    private void ShowNotEnoughCoinsPanel()
    {
        notEnoughCoinsPanel.SetActive(true);
    }

    private void DeactivateAllToggles()
    {
        foreach (var toggle in toggles)
        {
            if (toggle != null)
            {
                Toggle toggleComponent = toggle.GetComponent<Toggle>();
                if (toggleComponent != null)
                {
                    toggleComponent.isOn = false;
                    toggleComponent.interactable = true;
                }
            }
        }
    }

    private void HideAllToggles()
    {
        foreach (var toggle in toggles)
        {
            toggle.SetActive(false);
        }
    }

    private void ShowAllPricePanels()
    {
        foreach (var panel in pricePanel)
        {
            panel.SetActive(true);
        }
    }

    private void RemoveAllToggleListeners()
    {
        for (int i = 0; i < toggles.Length; i++)
        {
            if (toggles[i] != null)
            {
                Toggle toggle = toggles[i].GetComponent<Toggle>();
                if (toggle != null)
                {
                    toggle.onValueChanged.RemoveAllListeners();
                }
            }
        }
    }

    private void AddAllToggleListeners()
    {
        for (int i = 0; i < toggles.Length; i++)
        {
            int index = i;
            if (toggles[i] != null)
            {
                Toggle toggle = toggles[i].GetComponent<Toggle>();
                if (toggle != null)
                {
                    toggle.onValueChanged.AddListener((isOn) => OnToggle(index));
                }
            }
        }
    }

    public void OnToggle(int index)
    {
        if (toggles[index] == null)
        {
            Debug.LogError($"Toggle at index {index} is null!");
            return;
        }

        Toggle currentToggle = toggles[index].GetComponent<Toggle>();

        if (currentToggle == null)
        {
            Debug.LogError($"Toggle component at index {index} is null!");
            return;
        }

        string petKey = PlayerPrefKeys.PetPrefix;

        if (!currentToggle.isOn)
        {
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetEyes, 0);
            PlayerPrefs.Save();
            Glasses.SetActive(false);
            StartCoroutine(UpdateAccessoryOnServer(0));
        }
        else
        {
            RemoveAllToggleListeners();

            for (int i = 0; i < toggles.Length; i++)
            {
                if (i == index) continue;

                if (toggles[i] != null && toggles[i].activeInHierarchy)
                {
                    Toggle otherToggle = toggles[i].GetComponent<Toggle>();
                    if (otherToggle != null)
                    {
                        otherToggle.isOn = false;
                    }
                }
            }

            AddAllToggleListeners();

            int accessoryId = index + 5;
            PlayerPrefs.SetInt(petKey + PlayerPrefKeys.PetEyes, accessoryId);
            PlayerPrefs.Save();
            Glasses.SetActive(true);
            StartCoroutine(UpdateAccessoryOnServer(accessoryId));
        }
    }

    private IEnumerator UpdateAccessoryOnServer(int accessory_id)
    {
        string url = $"{apiUrlSecondary}/pets/{petId}/accessory";

        string jsonData = JsonUtility.ToJson(new AccTogPayload { 
            accessory_id = accessory_id, 
            accessory_category = "glasses" 
        });

        UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPUT)
        {
            uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(jsonData)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();
    }
}