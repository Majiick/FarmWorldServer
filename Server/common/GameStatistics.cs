// GameStatistics holds the values for things like the starting quantity or a small rock, or how much wood you get per an accurate hit.

using System;
using System.Collections.Generic;
using System.Text;
using static ObjectSchema.ObjectTypes;

namespace Server.common
{
    public static class GameStatistics
    {
        public static int StartingQuantity(IMineableSubMineableType subType, IMineableSize size)
        {
            int quantity;

            if (subType.Value == IMineableSubMineableType.IRON.Value)
            {
                quantity = 5;
            } else if (subType.Value == IMineableSubMineableType.STONE.Value)
            {
                quantity = 20;
            } else
            {
                throw new ArgumentException(String.Format("The subType {0} is not known.", subType.Value));
            }

            
            if (size.Value ==  IMineableSize.SMALL.Value)
            {
                quantity *= 1;
            } else if (size.Value == IMineableSize.MEDIUM.Value)
            {
                quantity *= 2;
            } else if (size.Value == IMineableSize.LARGE.Value)
            {
                quantity *= 4;
            } else
            {
                throw new ArgumentException(String.Format("The size {0} is not known.", size.Value));
            }

            return quantity;
        }

        public static int QuantityPerHit(IMineableSubMineableType subType)
        {
            int quantity;

            if (subType.Value == IMineableSubMineableType.IRON.Value)
            {
                quantity = 1;
            }
            else if (subType.Value == IMineableSubMineableType.STONE.Value)
            {
                quantity = 1;
            }
            else
            {
                throw new ArgumentException(String.Format("The subType {0} is not known.", subType.Value));
            }

            return quantity;
        }
    }
}
