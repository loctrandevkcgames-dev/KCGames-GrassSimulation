# GrassSimulation

Unity 6000.3.20f1 game project (working title: Grass Route), built on an embedded copy of the EncosyTower
framework.

- Game code: `Assets/GrassSimulation/` (`GrassSimulation.<Module>` assemblies). Burst and Jobs are allowed;
  ECS is not.
- Embedded framework: `Packages/com.laicasaane.encosy-tower*/` and `Plugins/`. Change it only when the game needs
  it, and commit framework changes separately from game changes.
- Design: [docs/Grass-Route-GDD.md](docs/Grass-Route-GDD.md).

## Conventions (mandatory)

- Project rules: [docs/conventions/PROJECT-CONVENTIONS.md](docs/conventions/PROJECT-CONVENTIONS.md)
- C# code: [docs/conventions/CODING-CONVENTIONS.md](docs/conventions/CODING-CONVENTIONS.md)
- Commits: [docs/conventions/COMMIT-CONVENTIONS.md](docs/conventions/COMMIT-CONVENTIONS.md)

**Every commit, whether the user asks for it or you decide to create it, follows COMMIT-CONVENTIONS.md through
the `grass-commit` skill.** The `.githooks/commit-msg` hook enforces the subject format. Never bypass it.

## Unity Editor

Drive the open Editor with the Unity CLI through the `grass-unity-editor` skill, which builds on the `unity-cli`
and `unity-pipeline` skills. Several Editors may be open at once, so always target this project. Never hand-edit
scene, prefab or asset YAML while the Editor is open. Never run `dotnet build`/`dotnet test` or MSBuild against the
Unity-generated `.csproj`/`.slnx`.
