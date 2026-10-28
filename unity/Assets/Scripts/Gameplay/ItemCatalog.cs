using System.Collections.Generic;
using System.Linq;
using CosTamarindo.Core;

namespace CosTamarindo.Gameplay
{
    /// <summary>
    /// Catálogo único de ítems. El <c>Id</c> coincide con el nombre del sprite
    /// (por ejemplo <c>item_cocada</c>), de modo que agregar arte nuevo no exige
    /// tocar código: basta con añadir el PNG y registrar su definición aquí.
    /// </summary>
    public static class ItemCatalog
    {
        private static readonly ItemDef[] Definitions =
        {
            // ── Buenos ────────────────────────────────────────────────
            new ItemDef("item_cocada",        "Cocada",           ItemKind.Good,  15, 1.0f, 1.00f),
            new ItemDef("item_tamarindo",     "Tamarindo",        ItemKind.Good,  15, 1.0f, 1.00f),
            new ItemDef("item_pescado",       "Pescado",          ItemKind.Good,  20, 0.9f, 1.05f),
            new ItemDef("item_frutas",        "Frutas",           ItemKind.Good,  10, 1.2f, 0.95f),
            new ItemDef("item_limonada_coco", "Limonada de coco", ItemKind.Good,  25, 0.6f, 1.10f),

            // ── Peligrosos: quitan una vida ───────────────────────────
            new ItemDef("item_erizo",         "Erizo",            ItemKind.Danger, 0, 0.5f, 1.15f),
            new ItemDef("item_medusa",        "Medusa",           ItemKind.Danger, 0, 0.5f, 1.15f),

            // ── Basura: resta puntos, no vidas ────────────────────────
            new ItemDef("item_basura",        "Basura",           ItemKind.Trash, -10, 0.7f, 1.00f),
        };

        private static readonly Dictionary<string, ItemDef> ById =
            Definitions.ToDictionary(d => d.Id);

        public static IReadOnlyList<ItemDef> All => Definitions;

        public static bool TryGet(string id, out ItemDef def) => ById.TryGetValue(id, out def);

        public static ItemDef Get(string id) => ById.TryGetValue(id, out var d) ? d : Definitions[0];

        /// <summary>Selecciona un ítem respetando los pesos relativos.</summary>
        public static ItemDef Roll(System.Random rng)
        {
            float total = 0f;
            foreach (var d in Definitions) total += d.Weight;

            float pick = (float)(rng.NextDouble() * total);
            foreach (var d in Definitions)
            {
                pick -= d.Weight;
                if (pick <= 0f) return d;
            }
            return Definitions[Definitions.Length - 1];
        }

        /// <summary>Ítems que el jugador puede coleccionar en el Libro.</summary>
        public static IEnumerable<ItemDef> Collectibles => Definitions.Where(d => d.Kind != ItemKind.Trash);
    }
}
