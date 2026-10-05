using System;
using EncosyTower.UnityExtensions;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class TireTrackRenderer : MonoBehaviour
    {
        private const int VERTICES_PER_SEGMENT = 4;
        private const int INDICES_PER_SEGMENT = 6;

        [SerializeField]
        private Transform[] _wheels = Array.Empty<Transform>();

        [SerializeField]
        private float _width = 0.14f;

        [SerializeField]
        private float _height = 0.015f;

        [SerializeField]
        private float _minSegmentLength = 0.12f;

        [SerializeField]
        private float _maxSegmentLength = 1.5f;

        [SerializeField]
        private float _lifetime = 8f;

        [SerializeField]
        private int _maxSegments = 2048;

        private Mesh _mesh;
        private Vector3[] _vertices;
        private Vector2[] _uvs;
        private Color32[] _colors;
        private float[] _birthTimes;
        private WheelTrail[] _trails;
        private float _clock;
        private int _next;
        private int _count;

        private static int[] CreateIndices(int segmentCount)
        {
            var indices = new int[segmentCount * INDICES_PER_SEGMENT];

            for (var i = 0; i < segmentCount; i++)
            {
                var vertex = i * VERTICES_PER_SEGMENT;
                var index = i * INDICES_PER_SEGMENT;

                indices[index] = vertex;
                indices[index + 1] = vertex + 2;
                indices[index + 2] = vertex + 1;
                indices[index + 3] = vertex + 1;
                indices[index + 4] = vertex + 2;
                indices[index + 5] = vertex + 3;
            }

            return indices;
        }

        public void Clear()
        {
            _clock = 0f;
            _next = 0;
            _count = 0;

            Array.Clear(array: _vertices, index: 0, length: _vertices.Length);
            Array.Clear(array: _colors, index: 0, length: _colors.Length);
            Array.Clear(array: _trails, index: 0, length: _trails.Length);

            UploadVertices();
            _mesh.SetColors(_colors);
        }

        public void Step(float deltaTime)
        {
            _clock += deltaTime;

            var wheelCount = _wheels.Length;
            var hasNewSegments = false;

            for (var i = 0; i < wheelCount; i++)
            {
                var wheel = _wheels[i];

                if (wheel.IsValid())
                {
                    hasNewSegments |= TryExtend(ref _trails[i], wheel.position);
                }
            }

            if (hasNewSegments)
            {
                UploadVertices();
            }

            FadeSegments();
        }

        private void Awake()
        {
            var vertexCount = _maxSegments * VERTICES_PER_SEGMENT;

            _vertices = new Vector3[vertexCount];
            _uvs = new Vector2[vertexCount];
            _colors = new Color32[vertexCount];
            _birthTimes = new float[_maxSegments];
            _trails = new WheelTrail[_wheels.Length];

            _mesh = new Mesh { name = "TireTracks" };
            _mesh.MarkDynamic();
            _mesh.SetVertices(_vertices);
            _mesh.SetUVs(channel: 0, _uvs);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(CreateIndices(_maxSegments), submesh: 0, calculateBounds: false);

            GetComponent<MeshFilter>().sharedMesh = _mesh;
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            transform.localScale = Vector3.one;
        }

        private void OnDestroy()
        {
            if (_mesh.IsValid())
            {
                Destroy(_mesh);
            }
        }

        private bool TryExtend(ref WheelTrail trail, Vector3 position)
        {
            position.y = _height;

            if (trail.HasPoint == false)
            {
                trail = new WheelTrail { Point = position, HasPoint = true };
                return false;
            }

            var delta = position - trail.Point;
            var length = delta.magnitude;

            if (length < _minSegmentLength)
            {
                return false;
            }

            if (length > _maxSegmentLength)
            {
                trail = new WheelTrail { Point = position, HasPoint = true };
                return false;
            }

            var side = Vector3.Cross(Vector3.up, delta / length) * (_width * 0.5f);
            var previousSide = trail.HasSide ? trail.Side : side;
            var distance = trail.Distance + length;

            WriteSegment(in trail, previousSide, position, side, distance);

            trail.Point = position;
            trail.Side = side;
            trail.HasSide = true;
            trail.Distance = distance;
            return true;
        }

        private void WriteSegment(in WheelTrail trail, Vector3 fromSide, Vector3 to, Vector3 toSide, float toDistance)
        {
            var from = trail.Point;
            var fromDistance = trail.Distance;
            var vertex = _next * VERTICES_PER_SEGMENT;

            _vertices[vertex] = from - fromSide;
            _vertices[vertex + 1] = from + fromSide;
            _vertices[vertex + 2] = to - toSide;
            _vertices[vertex + 3] = to + toSide;

            _uvs[vertex] = new Vector2(0f, fromDistance);
            _uvs[vertex + 1] = new Vector2(1f, fromDistance);
            _uvs[vertex + 2] = new Vector2(0f, toDistance);
            _uvs[vertex + 3] = new Vector2(1f, toDistance);

            _birthTimes[_next] = _clock;
            _next = (_next + 1) % _maxSegments;
            _count = Mathf.Min(_count + 1, _maxSegments);
        }

        private void UploadVertices()
        {
            _mesh.SetVertices(_vertices);
            _mesh.SetUVs(channel: 0, _uvs);
            _mesh.RecalculateBounds();
        }

        private void FadeSegments()
        {
            if (_count == 0)
            {
                return;
            }

            for (var i = 0; i < _count; i++)
            {
                var age = _clock - _birthTimes[i];
                var alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(1f - age / _lifetime) * byte.MaxValue);
                var color = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, alpha);
                var vertex = i * VERTICES_PER_SEGMENT;

                _colors[vertex] = color;
                _colors[vertex + 1] = color;
                _colors[vertex + 2] = color;
                _colors[vertex + 3] = color;
            }

            _mesh.SetColors(_colors);
        }

        private struct WheelTrail
        {
            public Vector3 Point;
            public Vector3 Side;
            public float Distance;
            public bool HasPoint;
            public bool HasSide;
        }
    }
}
