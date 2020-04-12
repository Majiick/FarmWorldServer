using System;
using System.Collections.Generic;
using System.Text;

namespace Server {
    class PlantManager {

        public static long CalculateGrowthTime(string plantableType) {
            long growthTime = 0;

            if (plantableType == ObjectSchema.ObjectTypes.IPlantableType.WHEAT.Value) {
                growthTime = ObjectSchema.ObjectLifeTimes.IPlantLifeTime.WHEAT.Value;
            } else if (plantableType == ObjectSchema.ObjectTypes.IPlantableType.TREE.Value) {
                growthTime = ObjectSchema.ObjectLifeTimes.IPlantLifeTime.TREE.Value;
            }

            return growthTime;
        }
    }

    public static bool CheckHarvestable(ObjectSchema.Plantable plantable) {
        if (plantable.timePlanted + plantable.growthTime >= GameTime.Instance().TickStartTime()) {
            return true;
        }

        return false;
    }
}
