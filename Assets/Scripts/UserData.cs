using System.Collections.Generic;
using Newtonsoft.Json;

[System.Serializable]
public class UserData
{
    public string NickName;
    public int Coin;
    public int Score;

    public string UnitList;
    public string Inventory;

    public UserData()
    {
    }

    public UserData(string nickName)
    {
        NickName = nickName;
        Coin = 600;
        Score = 0;

        Dictionary<string, bool> unitList = new Dictionary<string, bool>();
        unitList["Unit1"] = true;
        for (int i = 2; i <= 6; i++)
        {
            unitList["Unit" + i] = false;
        }

        Dictionary<string, int> inventory = new Dictionary<string, int>();
        inventory["EnergyDrink"] = 0;
        inventory["Shield"] = 0;
        inventory["MagicStone"] = 0;

        UnitList = JsonConvert.SerializeObject(unitList);
        Inventory = JsonConvert.SerializeObject(inventory);
    }
}