using System.Collections.Generic;
using System.Globalization;
using EncosyTower.Common;
using GrassSimulation.Gameplay;
using UnityEngine;

namespace GrassSimulation.Editor
{
    public sealed record class LayoutText(
          int Width
        , int Height
        , char[] Chars
        , int Seed
        , Dictionary<char, int> FruitCounts
        , RectInt[] Beds
        , Vector2Int Spawn
    )
    {
        public const char LAWN = '.';
        public const char SPAWN = 'S';
        public const char GRASS = 'g';
        public const char FLOWER = 'f';
        public const char THICK = 't';
        public const char BUSH_LOW = 'b';
        public const char VEGETABLE = 'v';
        public const char BUSH_BIG = 'B';
        public const char MELON = 'm';
        public const char TREE = 'T';
        public const char GIANT = 'G';
        public const char BED = 'P';
        public const char ROCK = 'R';
        public const char FENCE = '#';

        private const string LEGEND = ".SgftbvBmTGPR#";

        public char At(int x, int y)
            => x < 0 || y < 0 || x >= Width || y >= Height ? LAWN : Chars[y * Width + x];

        public static Result<LayoutText, LevelBuildError> Parse(string id, string text, int width, int height)
        {
            var rows = new List<string>();
            var seed = 0;
            var fruits = new Dictionary<char, int>();
            var lines = text.Split('\n');

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].TrimEnd('\r', ' ', '\t');

                if (line.StartsWith(';'))
                {
                    ReadHeader(line, ref seed, fruits);
                }
                else if (line.Length > 0)
                {
                    rows.Add(line);
                }
            }

            if (rows.Count != height)
            {
                return Fail(id, $"expected {height} rows, found {rows.Count}.");
            }

            var chars = new char[width * height];

            for (var row = 0; row < height; row++)
            {
                if (rows[row].Length != width)
                {
                    return Fail(id, $"row {row + 1} has {rows[row].Length} characters, expected {width}.");
                }

                for (var x = 0; x < width; x++)
                {
                    var c = rows[row][x];

                    if (LEGEND.IndexOf(c) < 0)
                    {
                        return Fail(id, $"unknown character '{c}' at row {row + 1}, column {x + 1}.");
                    }

                    chars[(height - 1 - row) * width + x] = c;
                }
            }

            return Finish(id, new LayoutText(width, height, chars, seed, fruits, null, default));
        }

        private static Result<LayoutText, LevelBuildError> Finish(string id, LayoutText layout)
        {
            var spawn = Vector2Int.zero;
            var spawnCount = 0;

            for (var i = 0; i < layout.Chars.Length; i++)
            {
                if (layout.Chars[i] == SPAWN)
                {
                    spawn = new Vector2Int(i % layout.Width, i / layout.Width);
                    spawnCount++;
                }
            }

            if (spawnCount != 1)
            {
                return Fail(id, $"expected exactly one spawn 'S', found {spawnCount}.");
            }

            var beds = FindBeds(layout, out var badBed);

            if (badBed.HasValue)
            {
                return Fail(id, $"the bed at ({badBed.Value.x}, {badBed.Value.y}) is not a rectangle.");
            }

            return Result<LayoutText, LevelBuildError>.Succeed(layout with { Beds = beds, Spawn = spawn });
        }

        private static void ReadHeader(string line, ref int seed, Dictionary<char, int> fruits)
        {
            var body = line.TrimStart(';').Trim();
            var split = body.IndexOf('=');

            if (split < 0)
            {
                return;
            }

            var key = body.Substring(0, split).Trim();
            var value = body.Substring(split + 1).Trim();

            if (key == "seed")
            {
                int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out seed);
            }
            else if (key == "fruit" && value.Length > 2 && value[1] == ':')
            {
                int.TryParse(value.Substring(2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count);
                fruits[value[0]] = count;
            }
        }

        private static RectInt[] FindBeds(LayoutText layout, out Vector2Int? badBed)
        {
            var beds = new List<RectInt>();
            var visited = new bool[layout.Chars.Length];

            badBed = null;

            for (var y = 0; y < layout.Height; y++)
            {
                for (var x = 0; x < layout.Width; x++)
                {
                    if (layout.At(x, y) != BED || visited[y * layout.Width + x])
                    {
                        continue;
                    }

                    var bounds = Flood(layout, visited, x, y, out var area);
                    var isRectangle = area == bounds.width * bounds.height;

                    if (isRectangle == false)
                    {
                        badBed = new Vector2Int(x, y);
                    }

                    beds.Add(bounds);
                }
            }

            return beds.ToArray();
        }

        private static RectInt Flood(LayoutText layout, bool[] visited, int startX, int startY, out int area)
        {
            var stack = new Stack<Vector2Int>();
            var min = new Vector2Int(startX, startY);
            var max = min;

            area = 0;
            visited[startY * layout.Width + startX] = true;
            stack.Push(min);

            while (stack.Count > 0)
            {
                var cell = stack.Pop();

                area++;
                min = Vector2Int.Min(min, cell);
                max = Vector2Int.Max(max, cell);

                Push(layout, visited, stack, cell.x - 1, cell.y);
                Push(layout, visited, stack, cell.x + 1, cell.y);
                Push(layout, visited, stack, cell.x, cell.y - 1);
                Push(layout, visited, stack, cell.x, cell.y + 1);
            }

            return new RectInt(min.x, min.y, max.x - min.x + 1, max.y - min.y + 1);
        }

        private static void Push(LayoutText layout, bool[] visited, Stack<Vector2Int> stack, int x, int y)
        {
            if (layout.At(x, y) != BED || x < 0 || y < 0 || x >= layout.Width || y >= layout.Height)
            {
                return;
            }

            if (visited[y * layout.Width + x])
            {
                return;
            }

            visited[y * layout.Width + x] = true;
            stack.Push(new Vector2Int(x, y));
        }

        private static Result<LayoutText, LevelBuildError> Fail(string id, string reason)
            => Result<LayoutText, LevelBuildError>.Err(new LevelBuildError.LayoutInvalid(id, reason));
    }
}
