using System;
using UnityEngine;
using UnityEngine.UI;

namespace ChickenRoad.Skins
{
    public enum SkinRarity { Common, Rare, Epic, Legendary }

    public sealed class ChickenSkinDefinition
    {
        public readonly string Id;
        public readonly string Name;
        public readonly int Price;
        public readonly SkinRarity Rarity;
        public readonly bool DailyExclusive;
        public readonly Color Tint;

        public ChickenSkinDefinition(string id, string name, int price, SkinRarity rarity, bool dailyExclusive, Color tint)
        {
            Id = id;
            Name = name;
            Price = price;
            Rarity = rarity;
            DailyExclusive = dailyExclusive;
            Tint = tint;
        }
    }

    public static class ChickenSkinCatalog
    {
        public static readonly ChickenSkinDefinition[] All =
        {
            new("classic", "Classic", 0, SkinRarity.Common, false, Color.white),
            new("lemon", "Lemon", 300, SkinRarity.Common, false, new Color(1f, .92f, .48f)),
            new("mint", "Mint", 450, SkinRarity.Common, false, new Color(.55f, 1f, .72f)),
            new("coral", "Coral", 600, SkinRarity.Common, false, new Color(1f, .58f, .5f)),
            new("sky", "Sky Rider", 900, SkinRarity.Rare, false, new Color(.45f, .75f, 1f)),
            new("violet", "Violet Star", 1400, SkinRarity.Epic, false, new Color(.78f, .48f, 1f)),
            new("emerald", "Emerald Crown", 0, SkinRarity.Epic, true, new Color(.22f, 1f, .52f)),
            new("golden", "Golden Legend", 0, SkinRarity.Legendary, true, new Color(1f, .68f, .08f))
        };

        public static ChickenSkinDefinition Get(string id)
        {
            foreach (ChickenSkinDefinition skin in All)
                if (skin.Id == id) return skin;
            return All[0];
        }

        public static Sprite GetSprite(string id)
        {
            return Resources.Load<Sprite>("ChickenSkins/" + Get(id).Id)
                ?? Resources.Load<Sprite>("RiverJump2D/Chicken");
        }
    }

    public static class ChickenSkinService
    {
        private const string SelectedKey = "ChickenSkin_Selected";
        private const string UnlockPrefix = "ChickenSkin_Unlocked_";
        public const string BalanceKey = "RiverJumpBalance";

        public static ChickenSkinDefinition Selected
        {
            get
            {
                ChickenSkinDefinition skin = ChickenSkinCatalog.Get(PlayerPrefs.GetString(SelectedKey, "classic"));
                return IsUnlocked(skin.Id) ? skin : ChickenSkinCatalog.All[0];
            }
        }

        public static bool IsUnlocked(string id) => id == "classic" || PlayerPrefs.GetInt(UnlockPrefix + id, 0) == 1;

        public static bool TryBuy(string id)
        {
            ChickenSkinDefinition skin = ChickenSkinCatalog.Get(id);
            if (skin.DailyExclusive || IsUnlocked(id)) return false;
            int balance = PlayerPrefs.GetInt(BalanceKey, 1000);
            if (balance < skin.Price) return false;
            PlayerPrefs.SetInt(BalanceKey, balance - skin.Price);
            Unlock(id);
            Select(id);
            return true;
        }

        public static void Unlock(string id)
        {
            PlayerPrefs.SetInt(UnlockPrefix + id, 1);
            PlayerPrefs.Save();
        }

        public static void Select(string id)
        {
            if (!IsUnlocked(id)) return;
            PlayerPrefs.SetString(SelectedKey, id);
            PlayerPrefs.Save();
        }

        public static ChickenSkinDefinition GetLockedDailySkin()
        {
            foreach (ChickenSkinDefinition skin in ChickenSkinCatalog.All)
                if (skin.DailyExclusive && !IsUnlocked(skin.Id)) return skin;
            return null;
        }

        public static void Apply(SpriteRenderer renderer)
        {
            if (renderer == null) return;
            renderer.sprite = ChickenSkinCatalog.GetSprite(Selected.Id);
            renderer.color = Color.white;
        }

        public static void Apply(Image image)
        {
            if (image == null) return;
            image.sprite = ChickenSkinCatalog.GetSprite(Selected.Id);
            image.color = Color.white;
        }

        public static Color RarityColor(SkinRarity rarity) => rarity switch
        {
            SkinRarity.Rare => new Color(.2f, .58f, 1f),
            SkinRarity.Epic => new Color(.72f, .3f, 1f),
            SkinRarity.Legendary => new Color(1f, .68f, .08f),
            _ => new Color(.72f, .82f, .84f)
        };
    }
}
