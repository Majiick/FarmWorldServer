// GameStatistics holds the values for things like the starting quantity or a small rock, or how much wood you get per an accurate hit.

using System;
using System.Collections.Generic;
using System.Text;
using static ObjectSchema.ObjectTypes;

namespace GameStatistics
{
    public static class GameStatistics
    {
        public static int StartingQuantity(string subType, string size)
        {
            int quantity;

            if (subType == IMineableSubMineableType.IRON.Value)
            {
                quantity = 5;
            }
            else if (subType == IMineableSubMineableType.STONE.Value)
            {
                quantity = 20;
            }
            else if (subType == IMineableSubMineableType.OAK.Value)
            {
                quantity = 20;
            }
            else
            {
                throw new ArgumentException(String.Format("The subType {0} is not known.", subType));
            }


            if (size == IMineableSize.SMALL.Value)
            {
                quantity *= 1;
            }
            else if (size == IMineableSize.MEDIUM.Value)
            {
                quantity *= 2;
            }
            else if (size == IMineableSize.LARGE.Value)
            {
                quantity *= 4;
            }
            else
            {
                throw new ArgumentException(String.Format("The size {0} is not known.", size));
            }

            return quantity;
        }

        public static int XpForMining(string subType, string size) {
            int xp;

            if (subType == IMineableSubMineableType.IRON.Value) {
                xp = 1;
            } else if (subType == IMineableSubMineableType.STONE.Value) {
                xp = 1;
            } else if (subType == IMineableSubMineableType.OAK.Value) {
                xp = 1;
            } else {
                throw new ArgumentException(String.Format("The subType {0} is not known.", subType));
            }


            if (size == IMineableSize.SMALL.Value) {
                xp *= 1;
            } else if (size == IMineableSize.MEDIUM.Value) {
                xp *= 2;
            } else if (size == IMineableSize.LARGE.Value) {
                xp *= 4;
            } else {
                throw new ArgumentException(String.Format("The size {0} is not known.", size));
            }

            return xp;
        }

        public static ItemSchema.ItemDBSchema ItemPerHit(IMineableSubMineableType subType, string userName)
        {
            var item = new ItemSchema.ItemDBSchema();
            if (subType.Value == ObjectSchema.ObjectTypes.IMineableSubMineableType.OAK.Value)
            {
                item.uniqueName = ItemSchema.ItemNames.Wood.Value;
            }
            else if (subType.Value == ObjectSchema.ObjectTypes.IMineableSubMineableType.IRON.Value)
            {
                item.uniqueName = ItemSchema.ItemNames.Ore.Value;
            }
            else if (subType.Value == ObjectSchema.ObjectTypes.IMineableSubMineableType.STONE.Value)
            {
                item.uniqueName = ItemSchema.ItemNames.Ore.Value;
            }
            else
            {
                throw new ArgumentException(String.Format("subType not recignized {0}", subType.Value));
            }

            item.userName = userName;
            item.quantity = 1;

            return item;
        }
    }
}
