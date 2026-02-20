using DataTable;
using Definition.Enum;
using GameFramework.DataTable;
using UnityEngine;

namespace CustomUtility
{
    public static class RarityUtility
    {
        public static ItemRarity SelectRarityForLevel(IDataTable<DRLevelRarity> table, int level)
        {
            DRLevelRarity row = GetLevelRarityRow(table, level);
            if (row == null)
            {
                return ItemRarity.White;
            }

            int white = Mathf.Max(0, row.WhiteWeight);
            int green = Mathf.Max(0, row.GreenWeight);
            int blue = Mathf.Max(0, row.BlueWeight);
            int purple = Mathf.Max(0, row.PurpleWeight);
            int red = Mathf.Max(0, row.RedWeight);

            int total = white + green + blue + purple + red;
            if (total <= 0)
            {
                return ItemRarity.White;
            }

            int roll = Random.Range(1, total + 1);
            if (roll <= white) return ItemRarity.White;
            roll -= white;
            if (roll <= green) return ItemRarity.Green;
            roll -= green;
            if (roll <= blue) return ItemRarity.Blue;
            roll -= blue;
            if (roll <= purple) return ItemRarity.Purple;
            return ItemRarity.Red;
        }

        private static DRLevelRarity GetLevelRarityRow(IDataTable<DRLevelRarity> table, int level)
        {
            if (table == null)
            {
                return null;
            }

            DRLevelRarity[] rows = table.GetAllDataRows();
            if (rows == null || rows.Length == 0)
            {
                return null;
            }

            int safeLevel = Mathf.Max(1, level);
            DRLevelRarity below = null;
            DRLevelRarity above = null;

            foreach (DRLevelRarity row in rows)
            {
                if (row == null)
                {
                    continue;
                }

                if (safeLevel >= row.LevelMin && safeLevel <= row.LevelMax)
                {
                    return row;
                }

                if (safeLevel < row.LevelMin)
                {
                    if (above == null || row.LevelMin < above.LevelMin)
                    {
                        above = row;
                    }
                }
                else if (safeLevel > row.LevelMax)
                {
                    if (below == null || row.LevelMax > below.LevelMax)
                    {
                        below = row;
                    }
                }
            }

            return above ?? below;
        }
    }
}
