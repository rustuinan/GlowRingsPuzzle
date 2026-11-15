using System;
using System.Linq;
using TMPro;
using UnityEngine;

[CreateAssetMenu(menuName = "Glow Rings/Coin Shop Config")]
public sealed class CoinShopConfig : ScriptableObject
{
    [Serializable] public sealed class Pack
    {
        public string ProductId;
        public string Title;
        [Min(1)] public int Coins;
        public Sprite Icon;
    }
    public TMP_FontAsset UIFont;
    public TMP_FontAsset BodyFont;
    public Sprite CoinIcon;
    public string NoAdsProductId = "remove_ads";
    public Sprite NoAdsIcon;
    public Pack[] Packs = {
        new Pack { ProductId = "coins_2000", Title = "COIN KESESİ", Coins = 2000 },
        new Pack { ProductId = "coins_6000", Title = "COIN ÇANTASI", Coins = 6000 },
        new Pack { ProductId = "coins_15000", Title = "COIN SANDIĞI", Coins = 15000 }
    };
    public bool IsValid => !string.IsNullOrWhiteSpace(NoAdsProductId) && Packs != null && Packs.Length > 0 &&
        Packs.All(x => x != null && !string.IsNullOrWhiteSpace(x.ProductId) && x.Coins > 0) &&
        Packs.Select(x => x.ProductId).Distinct().Count() == Packs.Length && Packs.All(x => x.ProductId != NoAdsProductId);
    public Pack Find(string id) => Packs != null ? Array.Find(Packs, x => x != null && x.ProductId == id) : null;
}
