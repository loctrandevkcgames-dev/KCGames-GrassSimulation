using GrassSimulation.Gameplay;

namespace GrassSimulation.Progression
{
    public readonly record struct ProgressSnapshot(
          int CompletedCount
        , int LevelCount
        , int NextLevelIndex
        , int TotalStars
        , bool IsReadOnly
        , bool HasPendingSave
    )
    {
        public static ProgressSnapshot From(ProgressionService service, LevelCatalog catalog, bool hasPendingSave)
        {
            var levelCount = catalog.Count;
            var completedCount = 0;
            var totalStars = 0;

            for (var i = 0; i < levelCount; i++)
            {
                var id = catalog.Get(i).Id;

                if (service.IsCompleted(id))
                {
                    completedCount++;
                }

                totalStars += service.GetStarCount(id);
            }

            return new ProgressSnapshot(
                  completedCount
                , levelCount
                , service.FindFirstIncomplete(catalog)
                , totalStars
                , service.IsReadOnly
                , hasPendingSave
            );
        }
    }
}
