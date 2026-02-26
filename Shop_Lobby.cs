using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Shop_Lobby : MonoBehaviour
{
    public enum EShopType
    {
        ReduceDamage
    }

    [SerializeField] private TMP_Text _money;
    [System.Serializable]
    public class ButtonObject
    {
        public GameObject button;
        public TMP_Text priceText;
    }
    [SerializeField] private ButtonObject[] _button = new ButtonObject[9];
    [SerializeField] private int _speedD = 10;
    [SerializeField] private int _lifeSteal = 3;
    [SerializeField] private int _crit = 6;
    [SerializeField] private int _reduceD = 10;

    // private void Awake()
    // {
    //     for (int i = 0; i < _button.Length; i++)
    //     {
    //         _button[i].priceText = _button[i].button.GetComponentInChildren<TMP_Text>();
    //         _button[i].type = (EShopType)i;

    //         int index = i;
    //         Button button = _button[i].button.GetComponent<Button>();
    //         button.onClick.AddListener(() => BuyItem(index));

    //         if (_button[i].countText != null)
    //             _button[i].max = 3;

    //         if (_button[i].priceText == null)
    //             continue;
    //         _button[i].price = SetPrice(_button[i].type);
    //         _button[i].priceText.text = _button[i].price.ToString();
    //     }
    // }

    private void OnEnable()
    {
        SetMoney();
        SetItem();
    }

    int SetPrice(EShopType type)
    {
        int price = 0;

        switch (type)
        {
            case EShopType.ReduceDamage:
                price = 30;
                break;
        }
        return price;
    }

    void BuyItem(int index)
    {
        var item = _button[index];
        Debug.Log($"{index + 1}¹øÂ° ¹öÆ°");

        if (GameData.Instance.Money < item.price)
            return;

        item.current++;

        if (item.countText != null)
            item.countText.text = $"{item.current} / {item.max}";
        switch (item.type)
        {
            case EShopType.HP:                
                GameData.Instance.UpgradeHP();
                break;
            case EShopType.Power:
                GameData.Instance.UpgradePower();
                break;
            case EShopType.Rate:
                GameData.Instance.UpgradeRate();
                break;
            case EShopType.Num:
                GameData.Instance.UpgradeSwordNum();
                break;
            case EShopType.SpeedDamage:
                GameData.Instance.UpgradeSpeedD(_speedD);
                break;
            case EShopType.Penetrate:
                GameData.Instance.UpgradePenetrate();
                break;
            case EShopType.LifeSteal:
                GameData.Instance.UpgradeLifeSteal(_lifeSteal);
                break;
            case EShopType.Crit:
                GameData.Instance.UpgradeCrit(_crit);
                break;
            case EShopType.ReduceDamage:
                GameData.Instance.UpgradeReduceD(_reduceD);
                break;
        }

        if(item.current == item.max)
            item.button.SetActive(false);

        GameData.Instance.ChangeMoney(-item.price);
        SetMoney();
    }

    void SetMoney()
    {
        _money.text = "µ·: " + GameData.Instance.Money;
    }

    void SetItem()
    {        
        for (int i = 0; i < _button.Length; i++)
        {
            var item = _button[i];

            item.current = TypeData(item.type);

            if (item.countText != null)
                item.countText.text = $"{item.current} / {item.max}";

            if (item.current == item.max)
                item.button.SetActive(false);
        }
    }

    int TypeData(EShopType type)
    {
        return type switch
        {
            EShopType.HP => GameData.Instance.HP,
            EShopType.Power => GameData.Instance.Power,
            EShopType.Rate => GameData.Instance.Rate,
            EShopType.Num => GameData.Instance.SwordNum,
            EShopType.SpeedDamage => GameData.Instance.SpeedD > 0 ? 1 : 0,
            EShopType.Penetrate => GameData.Instance.Penetrate ? 1 : 0,
            EShopType.LifeSteal => GameData.Instance.LifeSteal > 0 ? 1 : 0,
            EShopType.Crit => GameData.Instance.Crit > 0 ? 1 : 0,
            EShopType.ReduceDamage => GameData.Instance.ReduceD > 0 ? 1 : 0,
            _ => 0
        };
    }

}


