using Firebase.Database;
using Newtonsoft.Json;
using PimDeWitte.UnityMainThreadDispatcher;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class InventoryManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("Firebase")]
    [SerializeField] string databaseUrl = "https://final-ada3c-default-rtdb.asia-southeast1.firebasedatabase.app/";

    [Header("UI")]
    [SerializeField] Text CokeCountText;
    [SerializeField] Text HelmetCountText;
    [SerializeField] Text BurgerCountText;
    [SerializeField] Text MessageText;

    string userKey;
    Dictionary<string, int> inventory = new Dictionary<string, int>();

    void Start()
    {
        database = FirebaseDatabase.GetInstance(databaseUrl);
        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();

        userKey = PlayerPrefs.GetString("UserKey");

        if (string.IsNullOrEmpty(userKey))
        {
            MessageText.text = "로그인 정보가 없습니다.";
            return;
        }

        LoadInventory();
    }

    public void OnClickGoToMain()
    {
        SceneManager.LoadScene("LoginScene");
    }

    void LoadInventory()
    {
        reference
            .Child("UserInfo")
            .Child(userKey)
            .Child("Inventory")
            .GetValueAsync()
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    dispatcher.Enqueue(() =>
                    {
                        MessageText.text = "인벤토리 불러오기 실패";
                    });
                    return;
                }

                DataSnapshot snapshot = task.Result;

                if (snapshot.Value == null)
                {
                    dispatcher.Enqueue(() =>
                    {
                        MessageText.text = "인벤토리 데이터가 없습니다.";
                    });
                    return;
                }

                string inventoryJson = snapshot.Value.ToString();
                inventory = JsonConvert.DeserializeObject<Dictionary<string, int>>(inventoryJson);

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = "인벤토리 불러오기 완료";
                });
            });
    }

    void RefreshUI()
    {
        CokeCountText.text = "Coke : " + GetItemCount("Coke");
        HelmetCountText.text = "Helmet : " + GetItemCount("Helmet");
        BurgerCountText.text = "Burger : " + GetItemCount("Burger");
    }

    int GetItemCount(string itemName)
    {
        if (inventory.ContainsKey(itemName))
        {
            return inventory[itemName];
        }

        return 0;
    }

    public void OnClickUseCoke()
    {
        UseItem("Coke");
    }

    public void OnClickUseHelmet()
    {
        UseItem("Helmet");
    }

    public void OnClickUseBurger()
    {
        UseItem("Burger");
    }

    void UseItem(string itemName)
    {
        if (!inventory.ContainsKey(itemName) || inventory[itemName] <= 0)
        {
            MessageText.text = itemName + " 개수가 부족합니다.";
            return;
        }

        inventory[itemName]--;
        SaveInventory(itemName);
    }

    string GetUseMessage(string itemName)
    {
        switch (itemName)
        {
            case "Coke":
                return "시원한 Coke를 마셔 목마름을 해소했습니다!";
            case "Helmet":
                return "단단한 Helmet을 착용해 머리를 보호합니다.";
            case "Burger":
                return "맛있는 Burger를 먹어 체력을 든든하게 회복했습니다!";
            default:
                return itemName + " 사용 완료";
        }
    }

    void SaveInventory(string usedItemName)
    {
        string inventoryJson = JsonConvert.SerializeObject(inventory);

        reference
            .Child("UserInfo")
            .Child(userKey)
            .Child("Inventory")
            .SetValueAsync(inventoryJson)
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    dispatcher.Enqueue(() =>
                    {
                        MessageText.text = "인벤토리 저장 실패";
                    });
                    return;
                }

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = GetUseMessage(usedItemName);
                });
            });
    }
}