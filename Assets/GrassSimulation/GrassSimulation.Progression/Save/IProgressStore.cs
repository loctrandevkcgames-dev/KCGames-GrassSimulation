using EncosyTower.Common;

namespace GrassSimulation.Progression
{
    public interface IProgressStore
    {
        Result<ProgressSave, LoadError> Load();

        Success<SaveError> Save(ProgressSave save);

        Success<SaveError> Delete();
    }
}
