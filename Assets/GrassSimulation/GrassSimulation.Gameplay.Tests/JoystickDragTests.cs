using NUnit.Framework;
using UnityEngine;

namespace GrassSimulation.Gameplay.Tests;

public sealed class JoystickDragTests
{
    private const int MOUSE = 1;
    private const int TOUCH = 2;
    private const float RADIUS = 100f;

    private static readonly Vector2 s_screen = new(x: 1080f, y: 1920f);
    private static readonly Rect s_lowerHalf = new(x: 0f, y: 0f, width: 1f, height: 0.5f);

    private static Vector2 Press(
          ref JoystickDrag drag
        , Vector2 position
        , int pointerId = MOUSE
        , System.Func<Vector2, bool> isBlocked = null
    )
    {
        return drag.Update(
              isPressed: true
            , pointerId: pointerId
            , position: position
            , screenSize: s_screen
            , region: s_lowerHalf
            , isBlocked: isBlocked
            , radius: RADIUS
        );
    }

    private static void Release(ref JoystickDrag drag)
    {
        drag.Update(
              isPressed: false
            , pointerId: MOUSE
            , position: Vector2.zero
            , screenSize: s_screen
            , region: s_lowerHalf
            , isBlocked: null
            , radius: RADIUS
        );
    }

    [Test]
    public void PressInsideRegion_StartsDragAtThePressPoint()
    {
        var drag = new JoystickDrag();

        Press(ref drag, new Vector2(300f, 400f));

        Assert.That(drag.IsDragging, Is.True);
        Assert.That(drag.Origin, Is.EqualTo(new Vector2(300f, 400f)));
        Assert.That(drag.Radius, Is.EqualTo(RADIUS));
    }

    [Test]
    public void MovingWhileHeld_ReturnsClampedInput()
    {
        var drag = new JoystickDrag();

        Press(ref drag, new Vector2(300f, 400f));
        var input = Press(ref drag, new Vector2(350f, 400f));

        Assert.That(input, Is.EqualTo(new Vector2(0.5f, 0f)));
        Assert.That(drag.Input, Is.EqualTo(input));
    }

    [Test]
    public void PressOutsideRegion_IsIgnoredUntilRelease()
    {
        var drag = new JoystickDrag();

        Press(ref drag, new Vector2(300f, 1500f));
        var input = Press(ref drag, new Vector2(300f, 400f));

        Assert.That(drag.IsDragging, Is.False);
        Assert.That(input, Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void BlockedPress_IsIgnoredEvenWhenTheFingerSlidesOut()
    {
        var drag = new JoystickDrag();

        Press(ref drag, new Vector2(300f, 400f), isBlocked: _ => true);
        var input = Press(ref drag, new Vector2(400f, 400f), isBlocked: _ => false);

        Assert.That(drag.IsDragging, Is.False);
        Assert.That(input, Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void Release_ResetsAndAllowsANewDrag()
    {
        var drag = new JoystickDrag();

        Press(ref drag, new Vector2(300f, 1500f));
        Release(ref drag);
        Press(ref drag, new Vector2(500f, 300f));

        Assert.That(drag.IsDragging, Is.True);
        Assert.That(drag.Origin, Is.EqualTo(new Vector2(500f, 300f)));
    }

    [Test]
    public void Release_EndsTheDragAndClearsInput()
    {
        var drag = new JoystickDrag();

        Press(ref drag, new Vector2(300f, 400f));
        Press(ref drag, new Vector2(350f, 400f));
        Release(ref drag);

        Assert.That(drag.IsDragging, Is.False);
        Assert.That(drag.Input, Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void PointerSwitchMidPress_EndsTheDragAndIgnoresUntilRelease()
    {
        var drag = new JoystickDrag();

        Press(ref drag, new Vector2(300f, 400f));
        var input = Press(ref drag, new Vector2(500f, 400f), pointerId: TOUCH);

        Assert.That(drag.IsDragging, Is.False);
        Assert.That(input, Is.EqualTo(Vector2.zero));

        Press(ref drag, new Vector2(520f, 400f), pointerId: TOUCH);

        Assert.That(drag.IsDragging, Is.False);
    }

    [Test]
    public void PointerSwitch_DoesNotCarryTheOldOrigin()
    {
        var drag = new JoystickDrag();

        Press(ref drag, new Vector2(300f, 400f));
        Press(ref drag, new Vector2(500f, 400f), pointerId: TOUCH);
        Release(ref drag);
        Press(ref drag, new Vector2(700f, 200f), pointerId: TOUCH);

        Assert.That(drag.IsDragging, Is.True);
        Assert.That(drag.Origin, Is.EqualTo(new Vector2(700f, 200f)));
    }

    [Test]
    public void Reset_ClearsAnActiveDrag()
    {
        var drag = new JoystickDrag();

        Press(ref drag, new Vector2(300f, 400f));
        drag.Reset();

        Assert.That(drag.IsDragging, Is.False);
    }
}
