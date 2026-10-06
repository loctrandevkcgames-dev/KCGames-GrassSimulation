using System.Collections.Generic;
using EncosyTower.PubSub;
using EncosyTower.UnityExtensions;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace GrassSimulation.Gameplay
{
    public sealed class GrassFieldRenderer : MonoBehaviour
    {
        private const int CHUNK_CELLS = 16;

        private readonly List<GameObject> _chunks = new();
        private readonly List<Mesh> _meshes = new();
        private readonly List<ISubscription> _subscriptions = new();

        private Texture2D _stateTexture;
        private Texture2D _litterTexture;
        private Texture2D _litterAccentTexture;
        private Texture2D _cutStateTexture;
        private Vector4 _fieldParams;
        private Vector4 _bladeParams;
        private Vector4 _bladeState;
        private Vector4 _sweep;
        private bool _isStateDirty;

        public NativeArray<Color32> CellStates => _stateTexture.GetPixelData<Color32>(mipLevel: 0);

        public NativeArray<Color32> LitterStates => _litterTexture.GetPixelData<Color32>(mipLevel: 0);

        public NativeArray<Color32> LitterAccentStates => _litterAccentTexture.GetPixelData<Color32>(mipLevel: 0);

        public NativeArray<Color32> CutStates => _cutStateTexture.GetPixelData<Color32>(mipLevel: 0);

        public void Bind(MessageSubscriber.Subscriber<GameplayScope> subscriber)
        {
            _subscriptions.Unsubscribe();
            _subscriptions.Add(QuotaCompletedMsg.Subscribe(in subscriber, OnQuotaCompleted));
            _subscriptions.Add(TierUpMsg.Subscribe(in subscriber, OnTierUp));
        }

        public void Build(FieldGrid grid, PlantSettings[] plants, int seed)
        {
            var cellsX = grid.CellsX;
            var cellsZ = grid.CellsZ;
            var origin = grid.Origin;
            var size = grid.Size;

            ClearChunks();
            CreateStateTextures(cellsX, cellsZ);

            _fieldParams = new Vector4(origin.x, origin.y, 1f / size.x, 1f / size.y);
            _sweep = Vector4.zero;

            var builder = new GrassFieldMeshBuilder();
            var random = new System.Random(seed);
            var plantCount = plants.Length;

            for (var z = 0; z < cellsZ; z += CHUNK_CELLS)
            {
                for (var x = 0; x < cellsX; x += CHUNK_CELLS)
                {
                    var width = Mathf.Min(CHUNK_CELLS, cellsX - x);
                    var depth = Mathf.Min(CHUNK_CELLS, cellsZ - z);
                    var chunk = new RectInt(x, z, width, depth);

                    for (var i = 0; i < plantCount; i++)
                    {
                        var plant = plants[i];

                        if (builder.TryBuildChunk(grid, chunk, in plant, random, out var mesh))
                        {
                            CreateChunk(mesh, plant.Material, origin);
                        }
                    }
                }
            }

            _isStateDirty = true;
        }

        public void MarkCellStatesDirty()
        {
            _isStateDirty = true;
        }

        public void SetBlade(Vector3 position, float radius, float warning)
        {
            _bladeParams = new Vector4(position.x, position.y, position.z, radius);
            _bladeState = new Vector4(warning, 0f, 0f, 0f);
        }

        public void PlaySweep(Vector3 origin)
        {
            _sweep = new Vector4(origin.x, origin.z, Time.timeSinceLevelLoad, 1f);
        }

        private void LateUpdate()
        {
            if (_stateTexture.IsInvalid())
            {
                return;
            }

            if (_isStateDirty)
            {
                _stateTexture.Apply(updateMipmaps: false);
                _litterTexture.Apply(updateMipmaps: false);
                _litterAccentTexture.Apply(updateMipmaps: false);
                _cutStateTexture.Apply(updateMipmaps: false);
                _isStateDirty = false;
            }

            Shader.SetGlobalTexture(GrassFieldShaderIds.CellState, _stateTexture);
            Shader.SetGlobalTexture(GrassFieldShaderIds.Litter, _litterTexture);
            Shader.SetGlobalTexture(GrassFieldShaderIds.LitterAccent, _litterAccentTexture);
            Shader.SetGlobalTexture(GrassFieldShaderIds.CutState, _cutStateTexture);
            Shader.SetGlobalVector(GrassFieldShaderIds.FieldParams, _fieldParams);
            Shader.SetGlobalVector(GrassFieldShaderIds.BladeParams, _bladeParams);
            Shader.SetGlobalVector(GrassFieldShaderIds.BladeState, _bladeState);
            Shader.SetGlobalVector(GrassFieldShaderIds.Sweep, _sweep);
        }

        private void OnDisable()
        {
            Shader.SetGlobalVector(GrassFieldShaderIds.FieldParams, Vector4.zero);
            Shader.SetGlobalVector(GrassFieldShaderIds.BladeParams, Vector4.zero);
            Shader.SetGlobalVector(GrassFieldShaderIds.BladeState, Vector4.zero);
            Shader.SetGlobalVector(GrassFieldShaderIds.Sweep, Vector4.zero);
        }

        private void OnDestroy()
        {
            _subscriptions.Unsubscribe();
            ClearChunks();
            DestroyStateTextures();
        }

        private void CreateStateTextures(int cellsX, int cellsZ)
        {
            DestroyStateTextures();

            _stateTexture = new Texture2D(cellsX, cellsZ, TextureFormat.RGBA32, mipChain: false, linear: true) {
                name = "GrassCellState",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };

            _litterTexture = CreateSmoothTexture(cellsX, cellsZ, "GrassLitter");
            _litterAccentTexture = CreateSmoothTexture(cellsX, cellsZ, "GrassLitterAccent");
            _cutStateTexture = CreateSmoothTexture(cellsX, cellsZ, "GrassCutState");
        }

        private static Texture2D CreateSmoothTexture(int cellsX, int cellsZ, string name)
        {
            return new Texture2D(cellsX, cellsZ, TextureFormat.RGBA32, mipChain: false, linear: true) {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
        }

        private void DestroyStateTextures()
        {
            if (_stateTexture.IsValid())
            {
                Destroy(_stateTexture);
            }

            if (_litterTexture.IsValid())
            {
                Destroy(_litterTexture);
            }

            if (_litterAccentTexture.IsValid())
            {
                Destroy(_litterAccentTexture);
            }

            if (_cutStateTexture.IsValid())
            {
                Destroy(_cutStateTexture);
            }
        }

        private void CreateChunk(Mesh mesh, Material material, Vector2 origin)
        {
            var chunk = new GameObject(mesh.name) { hideFlags = HideFlags.DontSave };
            chunk.transform.SetParent(transform, worldPositionStays: false);
            chunk.transform.SetPositionAndRotation(new Vector3(origin.x, 0f, origin.y), Quaternion.identity);
            chunk.AddComponent<MeshFilter>().sharedMesh = mesh;

            var meshRenderer = chunk.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.On;

            _chunks.Add(chunk);
            _meshes.Add(mesh);
        }

        private void OnQuotaCompleted(QuotaCompletedMsg message)
        {
            PlayCelebrationSweep();
        }

        private void OnTierUp(TierUpMsg message)
        {
            PlayCelebrationSweep();
        }

        private void PlayCelebrationSweep()
        {
            if (PlayerOptions.GetReduceEffects() == false)
            {
                PlaySweep(_bladeParams);
            }
        }

        private void ClearChunks()
        {
            var chunkCount = _chunks.Count;

            for (var i = 0; i < chunkCount; i++)
            {
                Destroy(_chunks[i]);
                Destroy(_meshes[i]);
            }

            _chunks.Clear();
            _meshes.Clear();
        }
    }
}
