using System.Collections.Generic;
using System.Linq;
using CosTamarindo.Core;

namespace CosTamarindo.Gameplay
{
    /// <summary>
    /// Catálogo único de ítems (FE-03). El <c>Id</c> coincide exactamente con el
    /// nombre del sprite en <c>Assets/Art/Items/curated/</c> (por ejemplo
    /// <c>item_cocada</c> → <c>item_cocada.png</c>), de modo que
    /// <see cref="ItemSpawner"/> resuelve el arte por nombre sin cablear cada
    /// referencia a mano. Agregar un ítem nuevo exige dos pasos: copiar el PNG
    /// con el nombre del Id y registrar su definición aquí.
    ///
    /// La selección es deliberadamente corta: 12 tipos (8 buenos, 3 peligrosos y
    /// 1 de basura), no los 60 PNG exportados de Figma. El resto queda como
    /// archivo fuente para futuras temporadas o variantes visuales.
    /// </summary>
    public static class ItemCatalog
    {
        private static readonly ItemDef[] Definitions =
        {
            // ── Buenos ────────────────────────────────────────────────
            new ItemDef("item_cocada",        "Cocada",           ItemKind.Good,  15, 1.00f, 1.00f),
            new ItemDef("item_tamarindo",     "Tamarindo",        ItemKind.Good,  15, 1.00f, 1.00f),
            new ItemDef("item_pescado",       "Pescado",          ItemKind.Good,  20, 0.80f, 1.05f),
            new ItemDef("item_frutas",        "Frutas",           ItemKind.Good,  10, 1.10f, 0.95f),
            new ItemDef("item_limonada_coco", "Limonada de coco", ItemKind.Good,  25, 0.55f, 1.10f),
            new ItemDef("item_coco_verde",    "Coco verde",       ItemKind.Good,  12, 0.90f, 1.00f),
            new ItemDef("item_arepa",         "Arepa de huevo",   ItemKind.Good,  18, 0.70f, 1.00f),
            new ItemDef("item_raspao",        "Raspao",           ItemKind.Good,  22, 0.55f, 1.05f),

            // ── Peligrosos: quitan una vida ───────────────────────────
            new ItemDef("item_erizo",         "Erizo",            ItemKind.Danger, 0, 0.50f, 1.15f),
            new ItemDef("item_medusa",        "Medusa",           ItemKind.Danger, 0, 0.50f, 1.15f),
            new ItemDef("item_cangrejo",      "Cangrejo",         ItemKind.Danger, 0, 0.45f, 1.10f),

            // ── Basura: resta puntos, no vidas ────────────────────────
            new ItemDef("item_basura",        "Basura",           ItemKind.Trash, -10, 0.70f, 1.00f),
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
