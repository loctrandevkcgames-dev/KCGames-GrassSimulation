using System.Collections.Generic;
using UnityEngine;

namespace GrassSimulation.Gameplay
{
    internal static class FruitHalfMesh
    {
        private const int RINGS = 6;
        private const int SLICES = 14;
        private const float RADIUS = 0.5f;

        public static Mesh Create()
        {
            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var rind = new List<int>();
            var flesh = new List<int>();
            var columns = SLICES + 1;

            for (var ring = 0; ring <= RINGS; ring++)
            {
                var latitude = (float)ring / RINGS * Mathf.PI * 0.5f;
                var x = Mathf.Sin(latitude) * RADIUS;
                var ringRadius = Mathf.Cos(latitude) * RADIUS;

                for (var slice = 0; slice < columns; slice++)
                {
                    var longitude = (float)slice / SLICES * Mathf.PI * 2f;
                    var position = new Vector3(x, Mathf.Cos(longitude) * ringRadius, Mathf.Sin(longitude) * ringRadius);
                    positions.Add(position);
                    normals.Add(position.normalized);
                    uvs.Add(new Vector2((float)slice / SLICES, (float)ring / RINGS));
                }
            }

            for (var ring = 0; ring < RINGS; ring++)
            {
                for (var slice = 0; slice < SLICES; slice++)
                {
                    var current = ring * columns + slice;
                    var above = current + columns;
                    rind.AddRange(new[] { current, above, current + 1, current + 1, above, above + 1 });
                }
            }

            var center = positions.Count;
            positions.Add(Vector3.zero);
            normals.Add(Vector3.left);
            uvs.Add(new Vector2(0.5f, 0.5f));

            for (var slice = 0; slice < columns; slice++)
            {
                var rim = positions[slice];
                positions.Add(rim);
                normals.Add(Vector3.left);
                uvs.Add(new Vector2(rim.z / RADIUS * 0.5f + 0.5f, rim.y / RADIUS * 0.5f + 0.5f));
            }

            for (var slice = 0; slice < SLICES; slice++)
            {
                flesh.AddRange(new[] { center, center + 2 + slice, center + 1 + slice });
            }

            var mesh = new Mesh { name = "FruitHalf", subMeshCount = 2 };
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(rind, submesh: 0);
            mesh.SetTriangles(flesh, submesh: 1);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
