using System;
using EncosyTower.UnityExtensions;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    public sealed class ObstacleView : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] _rockPrefabs = Array.Empty<GameObject>();

        [SerializeField]
        private GameObject _fencePrefab;

        public void Build(ReadOnlySpan<ObstaclePlacement> placements, Vector2 origin)
        {
            Clear();

            var count = placements.Length;

            for (var i = 0; i < count; i++)
            {
                var placement = placements[i];
                var center = placement.Position + origin;

                if (placement.Kind == ObstacleKind.Rock)
                {
                    SpawnRock(i, in placement, center);
                }
                else
                {
                    SpawnFence(in placement, center);
                }
            }
        }

        public void Clear()
        {
            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        private static float MeasureX(GameObject instance)
        {
            var meshRenderer = instance.GetComponentInChildren<Renderer>();

            return meshRenderer.IsValid() ? meshRenderer.bounds.size.x : 1f;
        }

        private static float MeasureWidth(GameObject instance)
        {
            var meshRenderer = instance.GetComponentInChildren<Renderer>();

            if (meshRenderer.IsInvalid())
            {
                return 1f;
            }

            var size = meshRenderer.bounds.size;

            return Mathf.Max(size.x, size.z);
        }

        private void SpawnRock(int index, in ObstaclePlacement placement, Vector2 center)
        {
            if (_rockPrefabs.Length == 0)
            {
                return;
            }

            var prefab = _rockPrefabs[index % _rockPrefabs.Length];
            var position = new Vector3(center.x, 0f, center.y);
            var instance = Instantiate(prefab, position, Quaternion.Euler(0f, placement.Yaw, 0f), transform);
            var scale = placement.Length / Mathf.Max(MeasureWidth(instance), 1e-3f);

            instance.transform.localScale = Vector3.one * scale;
        }

        private void SpawnFence(in ObstaclePlacement placement, Vector2 center)
        {
            if (_fencePrefab.IsInvalid())
            {
                return;
            }

            var rotation = Quaternion.Euler(0f, placement.Yaw, 0f);
            var probe = Instantiate(_fencePrefab, transform);
            var pieceLength = Mathf.Max(MeasureX(probe), 1e-3f);

            Destroy(probe);

            var pieces = Mathf.Max(1, Mathf.RoundToInt(placement.Length / pieceLength));
            var stretch = placement.Length / (pieces * pieceLength);
            var axis = rotation * Vector3.right;
            var start = new Vector3(center.x, 0f, center.y) - axis * (placement.Length * 0.5f);

            for (var i = 0; i < pieces; i++)
            {
                var offset = axis * ((i + 0.5f) * placement.Length / pieces);
                var piece = Instantiate(_fencePrefab, start + offset, rotation, transform);

                piece.transform.localScale = new Vector3(stretch, 1f, 1f);
            }
        }
    }
}
