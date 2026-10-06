using NUnit.Framework;
using UnityEngine;

namespace GrassSimulation.Gameplay.Tests;

public sealed class ObstacleFieldTests
{
    private const float BODY_RADIUS = 0.3f;
    private const float TOLERANCE = 0.001f;

    [Test]
    public void Resolve_PushesTheBodyOutOfACircle()
    {
        var field = CreateField(ObstacleShape.Circle(ObstacleKind.Rock, Vector2.zero, radius: 1f));
        var velocity = new Vector2(-2f, 0f);

        var position = field.Resolve(new Vector2(1.1f, 0f), BODY_RADIUS, ref velocity);

        Assert.That(position.x, Is.EqualTo(1.3f).Within(TOLERANCE));
        Assert.That(position.y, Is.EqualTo(0f).Within(TOLERANCE));
    }

    [Test]
    public void Resolve_SlidesAlongTheEdgeWithoutBouncing()
    {
        var field = CreateField(ObstacleShape.Circle(ObstacleKind.Rock, Vector2.zero, radius: 1f));
        var velocity = new Vector2(-2f, 1f);

        field.Resolve(new Vector2(1.2f, 0f), BODY_RADIUS, ref velocity);

        Assert.That(velocity.x, Is.EqualTo(0f).Within(TOLERANCE));
        Assert.That(velocity.y, Is.EqualTo(1f).Within(TOLERANCE));
    }

    [Test]
    public void Resolve_KeepsVelocityMovingAwayFromTheObstacle()
    {
        var field = CreateField(ObstacleShape.Circle(ObstacleKind.Rock, Vector2.zero, radius: 1f));
        var velocity = new Vector2(3f, 0f);

        field.Resolve(new Vector2(1.2f, 0f), BODY_RADIUS, ref velocity);

        Assert.That(velocity, Is.EqualTo(new Vector2(3f, 0f)));
    }

    [Test]
    public void Resolve_LeavesADistantBodyAlone()
    {
        var field = CreateField(ObstacleShape.Circle(ObstacleKind.Rock, Vector2.zero, radius: 1f));
        var velocity = new Vector2(-2f, 0f);

        var position = field.Resolve(new Vector2(5f, 0f), BODY_RADIUS, ref velocity);

        Assert.That(position, Is.EqualTo(new Vector2(5f, 0f)));
        Assert.That(velocity, Is.EqualTo(new Vector2(-2f, 0f)));
    }

    [Test]
    public void Resolve_SlidesAlongAFence()
    {
        var fence = ObstacleShape.Capsule(ObstacleKind.Fence, new Vector2(-3f, 0f), new Vector2(3f, 0f), 0.12f);
        var field = CreateField(fence);
        var velocity = new Vector2(2f, -2f);

        var position = field.Resolve(new Vector2(1f, 0.3f), BODY_RADIUS, ref velocity);

        Assert.That(position.y, Is.EqualTo(0.42f).Within(TOLERANCE));
        Assert.That(velocity.x, Is.EqualTo(2f).Within(TOLERANCE));
        Assert.That(velocity.y, Is.EqualTo(0f).Within(TOLERANCE));
    }

    [Test]
    public void Resolve_HandlesACornerBetweenTwoObstacles()
    {
        var field = CreateField(
              ObstacleShape.Capsule(ObstacleKind.Fence, new Vector2(-3f, 0f), new Vector2(3f, 0f), 0.12f)
            , ObstacleShape.Capsule(ObstacleKind.Fence, new Vector2(0f, 0f), new Vector2(0f, 3f), 0.12f)
        );

        var velocity = new Vector2(-1f, -1f);
        var position = field.Resolve(new Vector2(0.2f, 0.2f), BODY_RADIUS, ref velocity);

        Assert.That(position.x, Is.GreaterThanOrEqualTo(0.42f - TOLERANCE));
        Assert.That(position.y, Is.GreaterThanOrEqualTo(0.42f - TOLERANCE));
    }

    [Test]
    public void IsOccluded_IsTrueWhenTheSegmentCrossesARock()
    {
        var field = CreateField(ObstacleShape.Circle(ObstacleKind.Rock, new Vector2(2f, 0f), radius: 0.5f));

        Assert.That(field.IsOccluded(Vector2.zero, new Vector2(4f, 0f)), Is.True);
        Assert.That(field.IsOccluded(Vector2.zero, new Vector2(4f, 3f)), Is.False);
    }

    [Test]
    public void IsOccluded_IsTrueWhenTheSegmentCrossesAFence()
    {
        var fence = ObstacleShape.Capsule(ObstacleKind.Fence, new Vector2(-1f, 1f), new Vector2(1f, 1f), 0.12f);
        var field = CreateField(fence);

        Assert.That(field.IsOccluded(Vector2.zero, new Vector2(0f, 2f)), Is.True);
        Assert.That(field.IsOccluded(Vector2.zero, new Vector2(3f, 2f)), Is.False);
        Assert.That(field.IsOccluded(Vector2.zero, new Vector2(0f, 0.5f)), Is.False);
    }

    [Test]
    public void HasObstacleNear_ComparesTheShapeBoundsWithTheBox()
    {
        var field = CreateField(ObstacleShape.Circle(ObstacleKind.Rock, new Vector2(5f, 5f), radius: 1f));

        Assert.That(field.HasObstacleNear(new Vector2(3.5f, 3.5f), new Vector2(4.5f, 4.5f)), Is.True);
        Assert.That(field.HasObstacleNear(new Vector2(0f, 0f), new Vector2(3f, 3f)), Is.False);
    }

    [Test]
    public void Placement_BuildsARockCircleAndAFenceCapsule()
    {
        var rock = new ObstaclePlacement { Kind = ObstacleKind.Rock, Position = new Vector2(1f, 2f), Length = 2f };
        var fence = new ObstaclePlacement {
            Kind = ObstacleKind.Fence,
            Position = new Vector2(0f, 0f),
            Yaw = 0f,
            Length = 4f,
        };

        var rockShape = ObstacleShape.From(in rock, new Vector2(1f, 1f));
        var fenceShape = ObstacleShape.From(in fence, Vector2.zero);

        Assert.That(rockShape.A, Is.EqualTo(new Vector2(2f, 3f)));
        Assert.That(rockShape.Radius, Is.EqualTo(1f));
        Assert.That(fenceShape.A.x, Is.EqualTo(-2f).Within(TOLERANCE));
        Assert.That(fenceShape.B.x, Is.EqualTo(2f).Within(TOLERANCE));
        Assert.That(fenceShape.Radius, Is.EqualTo(ObstacleShape.FENCE_RADIUS));
    }

    private static ObstacleField CreateField(params ObstacleShape[] shapes)
    {
        var field = new ObstacleField();

        for (var i = 0; i < shapes.Length; i++)
        {
            field.Add(in shapes[i]);
        }

        return field;
    }
}
