using System;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;

namespace GrassSimulation.Gameplay.Tests;

public sealed class FieldFeedbackTests
{
    private const float CELL_SIZE = 0.25f;
    private const byte FULL = 255;
    private const byte HALF = 128;

    [Test]
    public void NeverPlantedCell_LeavesNoClippings()
    {
        var grid = new FieldGrid(cellsX: 4, cellsZ: 4, CELL_SIZE);
        var feedback = new FieldFeedback(grid.Count);

        using var buffers = new Buffers(grid.Count);
        feedback.Write(grid, buffers.States, buffers.Litter, buffers.Accent, buffers.Cut);

        Assert.That(buffers.States[0].r, Is.EqualTo(FULL));
        Assert.That(buffers.Litter[0].a, Is.Zero);
        Assert.That(buffers.Cut[0].a, Is.Zero);
    }

    [Test]
    public void FreshCut_PopsThenClippingsFadeIn()
    {
        var grid = new FieldGrid(cellsX: 4, cellsZ: 4, CELL_SIZE);
        var feedback = new FieldFeedback(grid.Count);
        const int INDEX = 5;

        grid.Fill(new RectInt(0, 0, 4, 4), PlantKind.Grass);
        grid.SetProgress(INDEX, 1f);
        feedback.MarkCut(INDEX, Vector2.right);

        using var buffers = new Buffers(grid.Count);
        feedback.Write(grid, buffers.States, buffers.Litter, buffers.Accent, buffers.Cut);

        Assert.That(buffers.Cut[INDEX].a, Is.EqualTo(FULL));
        Assert.That(buffers.Litter[INDEX].a, Is.Zero);

        feedback.Decay(deltaTime: 1f);
        feedback.Write(grid, buffers.States, buffers.Litter, buffers.Accent, buffers.Cut);

        Assert.That(buffers.Cut[INDEX].a, Is.Zero);
        Assert.That(buffers.Litter[INDEX].a, Is.EqualTo(FULL));
        Assert.That(buffers.Cut[INDEX].r, Is.EqualTo(FULL));
        Assert.That(buffers.Cut[INDEX].g, Is.EqualTo(HALF));
    }

    [Test]
    public void Clear_SettlesCutsAndForgetsHeadings()
    {
        var grid = new FieldGrid(cellsX: 4, cellsZ: 4, CELL_SIZE);
        var feedback = new FieldFeedback(grid.Count);
        const int INDEX = 2;

        grid.Fill(new RectInt(0, 0, 4, 4), PlantKind.Grass);
        grid.SetProgress(INDEX, 1f);
        feedback.MarkCut(INDEX, Vector2.left);
        feedback.Clear();

        using var buffers = new Buffers(grid.Count);
        feedback.Write(grid, buffers.States, buffers.Litter, buffers.Accent, buffers.Cut);

        Assert.That(buffers.Litter[INDEX].a, Is.EqualTo(FULL));
        Assert.That(buffers.Cut[INDEX].r, Is.EqualTo(HALF));
        Assert.That(buffers.Cut[INDEX].a, Is.Zero);
    }

    [Test]
    public void AccentShare_IsScaledByTheClippingsAmount()
    {
        var grid = new FieldGrid(cellsX: 4, cellsZ: 4, CELL_SIZE);
        var feedback = new FieldFeedback(grid.Count);
        const int INDEX = 0;

        grid.Fill(new RectInt(0, 0, 4, 4), PlantKind.HarvestFlower);
        grid.SetProgress(INDEX, 1f);
        feedback.SetLitterColors(PlantKind.HarvestFlower, Color.white, Color.white, accentShare: 0.5f);

        using var buffers = new Buffers(grid.Count);
        feedback.Write(grid, buffers.States, buffers.Litter, buffers.Accent, buffers.Cut);

        Assert.That(buffers.Litter[INDEX].r, Is.EqualTo(FULL));
        Assert.That(buffers.Accent[INDEX].a, Is.EqualTo(HALF));
        Assert.That(buffers.Accent[INDEX + 1].a, Is.Zero);
    }

    private sealed class Buffers : IDisposable
    {
        public Buffers(int count)
        {
            States = new NativeArray<Color32>(count, Allocator.Temp);
            Litter = new NativeArray<Color32>(count, Allocator.Temp);
            Accent = new NativeArray<Color32>(count, Allocator.Temp);
            Cut = new NativeArray<Color32>(count, Allocator.Temp);
        }

        public NativeArray<Color32> States { get; }

        public NativeArray<Color32> Litter { get; }

        public NativeArray<Color32> Accent { get; }

        public NativeArray<Color32> Cut { get; }

        public void Dispose()
        {
            States.Dispose();
            Litter.Dispose();
            Accent.Dispose();
            Cut.Dispose();
        }
    }
}
