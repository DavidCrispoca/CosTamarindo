using System;

namespace CosTamarindo.Core
{
    /// <summary>Estados posibles de una partida.</summary>
    public enum GameState
    {
        Boot,
        Playing,
        Paused,
        GameOver
    }

    /// <summary>Categoría de un ítem que cae.</summary>
    public enum ItemKind
    {
        Good,
        Danger,
        Trash
    }

    /// <summary>
    /// Definición de un ítem coleccionable. Los valores viven en
    /// <see cref="CosTamarindo.Gameplay.ItemCatalog"/>; aquí solo se describe la forma.
    /// </summary>
    [Serializable]
    public sealed class ItemDef
    {
        public string Id;
        public string DisplayName;
        public ItemKind Kind;
        public int Points;
        public float Weight;
        public float FallSpeedMultiplier;

        public ItemDef(string id, string displayName, ItemKind kind, int points, float weight, float fallSpeedMultiplier = 1f)
        {
            Id = id;
            DisplayName = displayName;
            Kind = kind;
            Points = points;
            Weight = weight;
            FallSpeedMultiplier = fallSpeedMultiplier;
        }

        public bool CostsLife => Kind == ItemKind.Danger;
    }
}
