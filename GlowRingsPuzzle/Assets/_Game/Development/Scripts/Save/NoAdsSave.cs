using System;
using UnityEngine;

public static class NoAdsSave
{
    public const string OwnershipKey = "GlowRings.Purchases.RemoveAds";
    public static event Action Changed;
    public static bool Owned => PlayerPrefs.GetInt(OwnershipKey, 0) == 1;
    // Called only for a paid or restored non-consumable entitlement.
    public static bool Grant()
    {
        if (Owned) return false;
        PlayerPrefs.SetInt(OwnershipKey, 1); PlayerPrefs.Save(); Changed?.Invoke(); return true;
    }
}
