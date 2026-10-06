namespace GrassSimulation.Gameplay
{
    public static class StarRules
    {
        public const int MAX_STARS = 3;
        public const StarFlags ALL = StarFlags.Goal | StarFlags.Clean | StarFlags.Side;
        public const int ALL_BITS = (int)ALL;
        public const float SIDE_SWEEP_FRACTION = 0.9f;

        public static int Count(StarFlags flags)
        {
            var count = 0;

            for (var bits = (int)flags; bits != 0; bits &= bits - 1)
            {
                count++;
            }

            return count;
        }

        public static StarFlags Evaluate(
              bool isWin
            , bool isAssisted
            , bool isTimed
            , float remainingTime
            , float timeLimit
            , float star2TimeLeft
            , int protectedHits
            , bool isSideMet
        )
        {
            if (isWin == false)
            {
                return StarFlags.None;
            }

            var stars = StarFlags.Goal;

            if (isAssisted)
            {
                return stars;
            }

            var hasTimeLeft = isTimed == false || remainingTime >= timeLimit * star2TimeLeft;

            if (protectedHits == 0 && hasTimeLeft)
            {
                stars |= StarFlags.Clean;
            }

            if (isSideMet)
            {
                stars |= StarFlags.Side;
            }

            return stars;
        }
    }
}
