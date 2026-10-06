using System.Collections.Generic;
using System.IO;
using GrassSimulation.Gameplay;
using NUnit.Framework;
using UnityEditor;

namespace GrassSimulation.Editor.Tests;

public sealed class CatalogValidationTests
{
    private LevelCatalog _catalog;
    private PlantDefinition[] _plants;
    private List<LevelSpec> _specs;

    [SetUp]
    public void SetUp()
    {
        _catalog = AssetDatabase.LoadAssetAtPath<LevelCatalog>(LevelPipeline.CATALOG_PATH);

        var plantCatalog = AssetDatabase.LoadAssetAtPath<PlantCatalog>(LevelPipeline.PLANT_CATALOG_PATH);

        _plants = plantCatalog.Plants.ToArray();
        LevelSpecTable.Parse(File.ReadAllText(LevelPipeline.SPEC_PATH)).TryGetValue(out _specs);
    }

    [Test]
    public void Catalog_ListsEveryLevelThatHasALayoutInSheetOrder()
    {
        var withLayouts = 0;

        for (var i = 0; i < _specs.Count; i++)
        {
            withLayouts += File.Exists(LevelPipeline.LayoutPath(_specs[i])) ? 1 : 0;
        }

        Assert.That(_catalog.Count, Is.EqualTo(withLayouts));

        for (var i = 0; i < _catalog.Count; i++)
        {
            Assert.That(_catalog.Get(i).Id.Value, Is.EqualTo($"level-{_specs[i].Order:00}"));
        }
    }

    [Test]
    public void Catalog_DoesNotListTheSandboxLevel()
    {
        for (var i = 0; i < _catalog.Count; i++)
        {
            Assert.That(_catalog.Get(i).name, Does.Not.Contain("Sandbox"));
        }
    }

    [Test]
    public void Catalog_CarriesTheSpecTextAndUnlocks()
    {
        for (var i = 0; i < _catalog.Count; i++)
        {
            var level = _catalog.Get(i);

            Assert.That(level.DisplayName, Is.EqualTo(_specs[i].Name), _specs[i].Id);
            Assert.That(level.Decision, Is.EqualTo(_specs[i].Decision), _specs[i].Id);
            Assert.That(level.Unlock, Is.EqualTo(_specs[i].Unlock), _specs[i].Id);
        }
    }

    [Test]
    public void Catalog_PassesTheValidatorForEveryLevel()
    {
        for (var i = 0; i < _catalog.Count; i++)
        {
            var layout = LevelLayout.From(_catalog.Get(i));
            var issues = LevelValidator.Validate(layout, _specs[i], _plants, in LevelValidationRules.Default);

            Assert.That(issues, Is.Empty, $"{_specs[i].Id}: {string.Join("; ", issues)}");
        }
    }

    [Test]
    public void Catalog_MatchesAFreshBakeOfTheLayouts()
    {
        for (var i = 0; i < _catalog.Count; i++)
        {
            var spec = _specs[i];
            var layoutText = File.ReadAllText(LevelPipeline.LayoutPath(spec));

            LayoutText.Parse(spec.Id, layoutText, spec.Width, spec.Length).TryGetValue(out var layout);
            LevelBaker.Bake(spec, layout, _plants).TryGetValue(out var baked);

            var stored = LevelLayout.From(_catalog.Get(i));

            Assert.That(stored.Plants, Is.EqualTo(baked.Plants), spec.Id);
            Assert.That(stored.Obstacles, Is.EqualTo(baked.Obstacles), spec.Id);
            Assert.That(stored.Beds, Is.EqualTo(baked.Beds), spec.Id);
            Assert.That(stored.Spawn, Is.EqualTo(baked.Spawn), spec.Id);

            for (var cell = 0; cell < baked.Kinds.Length; cell++)
            {
                Assert.That((byte)stored.Kinds[cell], Is.EqualTo(baked.Kinds[cell]), $"{spec.Id} cell {cell}");
            }
        }
    }

    [Test]
    public void Catalog_UsesNoLegacyFieldKinds()
    {
        for (var i = 0; i < _catalog.Count; i++)
        {
            var layout = LevelLayout.From(_catalog.Get(i));

            Assert.That(layout.Kinds, Has.None.EqualTo(PlantKind.LowBush));
            Assert.That(layout.Kinds, Has.None.EqualTo(PlantKind.HardBush));
        }
    }
}
