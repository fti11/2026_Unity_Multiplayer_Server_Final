using System;
using Firebase.Database;
using Newtonsoft.Json;
using PimDeWitte.UnityMainThreadDispatcher;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


[Serializable]
public class UnitDataList
{
    public bool Unit2;
    public bool Unit3;
    public bool Unit4;
}
public class ShopManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("Firebase")]
    [SerializeField] string databaseUrl = "https://final-ada3c-default-rtdb.asia-southeast1.firebasedatabase.app/";

    [Header("UI")]
    [SerializeField] Text CoinText;
    [SerializeField] Text MessageText;

    [Header("Unit UI")]
    [SerializeField] int unit2Price = 300;
    [SerializeField] int unit3Price = 300;
    [SerializeField] int unit4Price = 300;

    private UnitDataList myUnitList = new UnitDataList();

    string userKey;
    int currentCoin;
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

        LoadUserData();
    }

    public void OnClickGoToMain()
    {
        SceneManager.LoadScene("LoginScene");
    }

    void LoadUserData()
    {
        reference
            .Child("UserInfo")
            .Child(userKey)
            .GetValueAsync()
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    dispatcher.Enqueue(() =>
                    {
                        MessageText.text = "유저 정보 불러오기 실패";
                    });
                    return;
                }

                DataSnapshot snapshot = task.Result;

                currentCoin = int.Parse(snapshot.Child("Coin").Value.ToString());

                string inventoryJson = snapshot.Child("Inventory").Value.ToString();
                inventory = JsonConvert.DeserializeObject<Dictionary<string, int>>(inventoryJson);

                LoadUnitData(userKey);

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = "유저 정보 불러오기 완료";
                });
            });
    }

    void LoadUnitData(string userKey)
    {
        reference.Child("UserInfo").Child(userKey).Child("UnitList").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsCompleted && !task.IsFaulted)
            {
                DataSnapshot snapshot = task.Result;
                if (snapshot != null && snapshot.Value != null)
                {
                    string jsonStr = snapshot.Value.ToString();
                    myUnitList = JsonUtility.FromJson<UnitDataList>(jsonStr);
                }
                else
                {
                    myUnitList = new UnitDataList { Unit2 = false, Unit3 = false, Unit4 = false };
                }
            }
        });
    }

    public void BuyUnit(int unitId)
    {
        int price = 0;
        bool isAlreadyOwned = false;

        if (unitId == 2) { price = unit2Price; isAlreadyOwned = myUnitList.Unit2; }
        else if (unitId == 3) { price = unit3Price; isAlreadyOwned = myUnitList.Unit3; }
        else if (unitId == 4) { price = unit4Price; isAlreadyOwned = myUnitList.Unit4; }

        if (isAlreadyOwned)
        {
            MessageText.text = $"Unit {unitId}는 이미 보유한 유닛입니다!";
            return;
        }

        if (currentCoin < price)
        {
            MessageText.text = "코인이 부족합니다.";
            return;
        }

        currentCoin -= price;
        if (unitId == 2) myUnitList.Unit2 = true;
        else if (unitId == 3) myUnitList.Unit3 = true;
        else if (unitId == 4) myUnitList.Unit4 = true;

        RefreshUI();

        SaveUnitAndCoinData();
    }

    void SaveUnitAndCoinData()
    {
        string userKey = PlayerPrefs.GetString("UserKey");
        if (string.IsNullOrEmpty(userKey)) return;

        string unitJson = JsonUtility.ToJson(myUnitList);

        reference.Child("UserInfo").Child(userKey).Child("Coin").SetValueAsync(currentCoin);
        reference.Child("UserInfo").Child(userKey).Child("UnitList").SetValueAsync(unitJson).ContinueWith(task =>
        {
            dispatcher.Enqueue(() =>
            {
                if (task.IsCompleted) MessageText.text = "유닛 구매 성공!";
                else MessageText.text = "서버 저장 실패";
            });
        });
    }

    void RefreshUI()
    {
        CoinText.text = "Coin : " + currentCoin;
    }

    public void OnClickBuyCoke()
    {
        BuyItem("Coke", 80);
    }

    public void OnClickBuyHelmet()
    {
        BuyItem("Helmet", 150);
    }

    public void OnClickBuyBurger()
    {
        BuyItem("MagicStone", 220);
    }

    void BuyItem(string itemName, int price)
    {
        if (currentCoin < price)
        {
            MessageText.text = "코인이 부족합니다.";
            return;
        }

        currentCoin -= price;

        if (inventory.ContainsKey(itemName))
        {
            inventory[itemName]++;
        }
        else
        {
            inventory[itemName] = 1;
        }

        SaveUserData(itemName);
    }

    void SaveUserData(string boughtItemName)
    {
        string inventoryJson = JsonConvert.SerializeObject(inventory);

        Dictionary<string, object> updateData = new Dictionary<string, object>();
        updateData["Coin"] = currentCoin;
        updateData["Inventory"] = inventoryJson;

        reference
            .Child("UserInfo")
            .Child(userKey)
            .UpdateChildrenAsync(updateData)
            .ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    dispatcher.Enqueue(() =>
                    {
                        MessageText.text = "구매 저장 실패";
                    });
                    return;
                }

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = boughtItemName + " 구매 완료";
                });
            });
    }
}