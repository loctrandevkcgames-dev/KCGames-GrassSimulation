# Data, Databases, ConfigKey and Settings

Verdicts: Data/Databases = Later (balancing tables). `ConfigKey<bool>` PlayerPrefs = Later (sound/haptics).
`Settings<T>` for player settings = Reject (project/editor settings only).

## Data and Databases (balancing tables) - `EncosyTower.Data`

- asmdef: `EncosyTower.Data` (references `EncosyTower.Core` only; `FW/EncosyTower.Data/EncosyTower.Data.asmdef`). Generator:
  `FW/EncosyTower.Data/SourceGenerators/EncosyTower.Data.Generators.dll` (+ `EncosyTower.Data.CodeRefactors.dll`);
  source `GEN/EncosyTower.Data.Generators/`. Namespaces: `EncosyTower.Data` (`[Data]`, `[DataProperty]`, `[DataMutable]`,
  `[DataWithoutId]`), `EncosyTower.Data.Authoring` (`[DataManualAuthoring]`), `EncosyTower.Databases` (`[Database]`, `[Table]`,
  `[DataTableAsset]`, `DatabaseAsset`, `DataTableAssetBase<,>`).
- Authoring (Editor only): `EncosyTower.Databases.Authoring` (asmdef `defineConstraints`: `UNITY_EDITOR||ENCOSY_INCLUDE_AUTHORING`,
  `BAKING_SHEET`, `UNITY_NEWTONSOFT_JSON`; references BakingSheet.* and ExcelDataReader/Google APIs precompiled DLLs) with
  generator `EncosyTower.Databases.Authoring.Generators.dll`. `manifest.json` already lists `com.laicasaane.bakingsheet`.
- Sample: `SAMPLES/EncosyTower.Samples.Data/` (`HeroTableAsset.cs`, `SampleDatabase.cs`, `BakingSheetFeatures.cs`,
  `DatabaseTester.cs`, `Common.cs`). The sample asmdef is Editor-only; copy patterns, not the assembly.

```csharp
[DataTableAsset]
public sealed partial class HeroTableAsset : DataTableAssetBase<EntityId, HeroData> { }

[Data, DataMutable(DataMutableOptions.WithReadOnlyView)]
public partial struct HeroData
{
    [DataProperty(typeof(EntityIdData))] public readonly EntityId Id => Get_Id();
    [DataProperty] public readonly EntityStatData Stat => Get_Stat();
    [DataProperty] public readonly ReadOnlyMemory<EntityStatMultiplierData> Multipliers => Get_Multipliers();
}

[Database(NameCasing.SnakeLower, AssetName = $"{nameof(SampleDatabase)}Asset")]
public readonly partial struct SampleDatabase { [Table] public readonly HeroTableAsset Heroes => Get_Heroes(); }

// runtime (DatabaseTester.cs):
var db = new SampleDatabase(databaseAsset /* DatabaseAsset */, InitializationBehaviour.Forced);
foreach (var entry in db.Heroes.Entries.Span) { ... }
```

- `[DataTableAsset]` ids may be `[WrapRecord]` ids or `[UnionId]` ids (sample `EntityId`). Field storage is generated
  (`_id`, `_name`); `Get_X()` reads it.
- Authoring side (Editor, BakingSheet): `[AuthorDatabase(typeof(SampleDatabase), converters...)] readonly partial struct
  SampleDatabaseAuthoring` with nested `SheetContainer` / `*DataSheet` partials (`BakingSheetFeatures.cs`, guarded by
  `#if UNITY_EDITOR && BAKING_SHEET`). The authoring asmdef references the BakingSheet Csv/Excel/Google/Json packages; baked output is consumed through a
  `DatabaseAsset` (sample `DatabaseTester` field `_database`). Check `FW/EncosyTower.Editor` conversion tasks before relying on the bake flow.
- `[Database]` is `[Conditional("UNITY_EDITOR"), Conditional("ENCOSY_INCLUDE_AUTHORING")]`
  (`FW/EncosyTower.Data/Databases/Annotations/DatabaseAttribute.cs`): the attribute vanishes from player builds.
- The sample uses `StringId`/`StringVault` for names (`HeroData.Name`). `StringId` is rejected for persisted ids because the
  vault assigns sequential interning indexes that are not stable across sessions (`FW/EncosyTower.Core/StringIds/StringVault.cs:184-186, 199-201`).
  For balancing tables prefer plain `string`/enum fields unless localization needs ids.

Adopt when: designers need spreadsheet-driven balancing (level params, star thresholds, prices). Costs: BakingSheet package,
Newtonsoft (already in manifest), generated partial structs, baked assets in the build, Editor-only authoring asmdefs.

## ConfigKey<T> and PlayerPrefs - `EncosyTower.ConfigKeys` (Core)

`FW/EncosyTower.Core/ConfigKeys/` (`ConfigKey<T>` generic-1 file) and `ConfigKeyPlayerPrefExtensions.cs`. `ConfigKey<T>` is a typed
string key (implicit from `string`). PlayerPrefs extensions exist for `string`, `bool`, `int`, `float`:

```csharp
private static readonly ConfigKey<bool> s_soundEnabled = "SOUND_ENABLED";      // pattern from PlayerPersistence.API in the Persistence sample
bool on = s_soundEnabled.GetPlayerPref(true);                                   // GetPlayerPref(defaultValue) or Option<T> GetPlayerPref()
s_soundEnabled.SetPlayerPref(false);                                            // SetPlayerPref(value) / SetPlayerPref(value, overrideExisting)
bool has = s_soundEnabled.HasPlayerPref();   s_soundEnabled.DeletePlayerPref();
```

Fits sound/haptics/quality toggles (small scalar player settings). Not for progress or anything that needs integrity.

## Settings<T> - `EncosyTower.Settings` (Core)

`abstract class Settings<T> : ScriptableObject` with `[Settings(SettingsUsage usage, string displayPath = null, string filename = null)]`;
`SettingsUsage` is `RuntimeProject`, `EditorProject`, `EditorUser` (`Settings/SettingsAttribute.cs`, `SettingsUsage.cs`,
`Settings.cs`). It is a ScriptableObject singleton asset for project/editor configuration, not a store for mutable
player settings. Reject for player settings; acceptable for read-only project tuning assets if a need appears.

## Gotchas

- Generated partial structs/classes only; do not hand-edit generated members.
- Data sample asmdef is Editor-only; game assemblies need `EncosyTower.Core` + `EncosyTower.Data`, and authoring code lives in
  an Editor-only assembly with the BakingSheet references.
- `ConfigKey<T>` PlayerPrefs are plain Unity PlayerPrefs: no integrity, no cloud sync.
