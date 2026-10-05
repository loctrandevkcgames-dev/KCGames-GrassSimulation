using System;
using EncosyTower.Common;

namespace GrassSimulation.Progression.Tests;

internal sealed class FakeProgressStore : IProgressStore
{
    public bool FailSaves { get; set; }

    public bool ThrowOnSave { get; set; }

    public bool FailDeletes { get; set; }

    public LoadError? LoadFailure { get; set; }

    public int SaveCount { get; private set; }

    public int DeleteCount { get; private set; }

    public ProgressSave Saved { get; private set; }

    public Result<ProgressSave, LoadError> Load()
    {
        if (LoadFailure.HasValue)
        {
            return Result<ProgressSave, LoadError>.Err(LoadFailure.Value);
        }

        if (Saved == null)
        {
            return Result<ProgressSave, LoadError>.Err(new LoadError.NotFound());
        }

        return Result<ProgressSave, LoadError>.Succeed(Saved.Clone());
    }

    public Success<SaveError> Save(ProgressSave save)
    {
        SaveCount++;

        if (ThrowOnSave)
        {
            throw new InvalidOperationException("The fake store throws on save.");
        }

        if (FailSaves)
        {
            return Success.No<SaveError>(new SaveError.WriteFailed("fake", "The fake store fails saves."));
        }

        Saved = save.Clone();
        return Success.Yes;
    }

    public Success<SaveError> Delete()
    {
        DeleteCount++;

        if (FailDeletes)
        {
            return Success.No<SaveError>(new SaveError.WriteFailed("fake", "The fake store fails deletes."));
        }

        Saved = null;
        return Success.Yes;
    }
}
