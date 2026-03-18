using System;
using System.Reflection;
using UnityEngine;

public class JokerSelect : MonoBehaviour
{
    public string code;
    public string name;
    public string description;
    public int price;

    public void Initialize(string code, string name, string description, int price)
    {
        this.code = code;
        this.name = name;
        this.description = description;
        this.price = price;
    }
    public void SellJoker()
    {
        int siblingIndex = transform.GetSiblingIndex();
        JokerHolderUI.Instance.RemoveJoker(siblingIndex);

        if (JokerManager.Instance.jokers != null && siblingIndex >= 0 && siblingIndex < JokerManager.Instance.jokers.Count)
            JokerManager.Instance.jokers.RemoveAt(siblingIndex);
        else
            JokerManager.Instance.jokers.Remove(code);

        if (PlayerStatManager.Instance != null)
        {
            PlayerStatManager.Instance.cash += price / 2;
            RefreshCashState(PlayerStatManager.Instance.cash);
        }

        StatsUpdater.Instance?.UpdateJokerCount();
    }

    private static void RefreshCashState(int cashValue)
    {
        StatsUpdater.Instance?.UpdateCash(cashValue);

        Shop shop = UnityEngine.Object.FindFirstObjectByType<Shop>();
        if (shop != null)
        {
            shop.DisplayMoney();
        }

        TryUpdateCashManager(cashValue);
    }

    private static void TryUpdateCashManager(int cashValue)
    {
        MonoBehaviour[] behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || !behaviour.gameObject.scene.IsValid())
            {
                continue;
            }

            Type behaviourType = behaviour.GetType();
            if (!string.Equals(behaviourType.Name, "CashManager", StringComparison.Ordinal))
            {
                continue;
            }

            if (TryInvokeCashMethod(behaviour, behaviourType, "SetCash", cashValue))
            {
                return;
            }

            if (TryInvokeCashMethod(behaviour, behaviourType, "UpdateCash", cashValue))
            {
                return;
            }

            if (TrySetCashMember(behaviour, behaviourType, "cash", cashValue))
            {
                return;
            }

            if (TrySetCashMember(behaviour, behaviourType, "currentCash", cashValue))
            {
                return;
            }
        }
    }

    private static bool TryInvokeCashMethod(MonoBehaviour behaviour, Type behaviourType, string methodName, int cashValue)
    {
        MethodInfo method = behaviourType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(int) }, null);
        if (method == null)
        {
            return false;
        }

        method.Invoke(behaviour, new object[] { cashValue });
        return true;
    }

    private static bool TrySetCashMember(MonoBehaviour behaviour, Type behaviourType, string memberName, int cashValue)
    {
        PropertyInfo property = behaviourType.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property != null && property.CanWrite && property.PropertyType == typeof(int))
        {
            property.SetValue(behaviour, cashValue);
            return true;
        }

        FieldInfo field = behaviourType.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(int))
        {
            field.SetValue(behaviour, cashValue);
            return true;
        }

        return false;
    }
}
