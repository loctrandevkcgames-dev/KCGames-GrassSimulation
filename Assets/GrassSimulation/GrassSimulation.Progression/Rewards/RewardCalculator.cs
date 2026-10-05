using System;
using System.Collections.Generic;
using GrassSimulation.Gameplay;

namespace GrassSimulation.Progression
{
    public static class RewardCalculator
    {
        public static void Collect(
              LevelId level
            , bool isWin
            , int stars
            , ICollection<string> granted
            , List<RewardGrant> output
        )
        {
            if (isWin == false)
            {
                return;
            }

            RewardId firstWin = new RewardId.FirstWin(level);

            if (granted.Contains(firstWin.ToKey()) == false)
            {
                output.Add(new RewardGrant(firstWin, RewardRules.FIRST_WIN_COINS));
            }

            var starCount = Math.Min(Math.Max(stars, 0), RewardRules.MAX_STARS);

            for (var star = 1; star <= starCount; star++)
            {
                RewardId id = new RewardId.Star(level, star);

                if (granted.Contains(id.ToKey()) == false)
                {
                    output.Add(new RewardGrant(id, RewardRules.STAR_COINS));
                }
            }
        }
    }
}
