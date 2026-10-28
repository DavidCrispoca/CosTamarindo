using System.Collections.Generic;
using UnityEngine;

namespace CosTamarindo.Core
{
    /// <summary>
    /// Colección local de ítems descubiertos (pantalla «Libro»).
    /// Solo guarda preferencias de presentación: nunca puntajes ni vidas,
    /// para no abrir una vía de manipulación desde el cliente (control A.8.9).
    /// </summary>
    public static class CollectionBook
    {
        private const string Prefix = "ct.unlocked.";

        public static bool IsUnlocked(string itemId) =>
            !string.IsNullOrEmpty(itemId) && PlayerPrefs.GetInt(Prefix + itemId, 0) == 1;

        /// <summary>Marca el ítem como descubierto. Devuelve true si era la primera vez.</summary>
        public static bool Unlock(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || IsUnlocked(itemId)) return false;

            PlayerPrefs.SetInt(Prefix + itemId, 1);
            PlayerPrefs.Save();
            return true;
        }

        public static int UnlockedCount(IEnumerable<string> ids)
        {
            int total = 0;
            foreach (var id in ids)
                if (IsUnlocked(id)) total++;
            return total;
        }

        public static void ResetAll(IEnumerable<string> ids)
        {
            foreach (var id in ids) PlayerPrefs.DeleteKey(Prefix + id);
            PlayerPrefs.Save();
        }
    }
}
