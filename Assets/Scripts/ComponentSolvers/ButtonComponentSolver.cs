﻿using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

public class ButtonComponentSolver : ComponentSolver
{
    public ButtonComponentSolver(MonoBehaviour bomb, MonoBehaviour bombComponent) :
        base(bomb, bombComponent)
    {
        _button = (MonoBehaviour)_buttonField.GetValue(bombComponent);
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if (!_held && (inputCommand.Equals("tap", StringComparison.InvariantCultureIgnoreCase) || inputCommand.Equals("click", StringComparison.InvariantCultureIgnoreCase)))
        {
            yield return "tap";

            DoInteractionStart (_button);
            yield return new WaitForSeconds(0.1f);
            DoInteractionEnd(_button);
        }
        else if (!_held && inputCommand.Equals("hold", StringComparison.InvariantCultureIgnoreCase))
        {
            yield return "hold";

            _held = true;
            DoInteractionStart(_button);
            yield return new WaitForSeconds(2.0f);
        }
        else if (_held)
        {
            string[] commandParts = inputCommand.Split(' ');
            if (commandParts.Length == 2 && commandParts[0].Equals("release", StringComparison.InvariantCultureIgnoreCase))
            {
                int second = 0;
                if (!int.TryParse(commandParts[1], out second))
                {
                    yield break;
                }

                if (second >= 0 && second <= 9)
                {
                    IEnumerator releaseCoroutine = ReleaseCoroutine(second);
                    while (releaseCoroutine.MoveNext())
                    {
                        yield return releaseCoroutine.Current;
                    }
                }
            }
        }
    }

    private IEnumerator ReleaseCoroutine(int second)
    {
        yield return "release";

        MonoBehaviour timerComponent = (MonoBehaviour)CommonReflectedTypeInfo.GetTimerMethod.Invoke(Bomb, null);

        string secondString = second.ToString();

        float timeRemaining = float.PositiveInfinity;
        while (timeRemaining > 0.0f)
        {
            timeRemaining = (float)CommonReflectedTypeInfo.TimeRemainingField.GetValue(timerComponent);
            string formattedTime = (string)CommonReflectedTypeInfo.GetFormattedTimeMethod.Invoke(null, new object[] { timeRemaining, true });

            if (formattedTime.Contains(secondString))
            {
                DoInteractionEnd(_button);
                _held = false;
                break;
            }

            yield return null;
        }
    }

    static ButtonComponentSolver()
    {
        _buttonComponentType = ReflectionHelper.FindType("ButtonComponent");
        _buttonField = _buttonComponentType.GetField("button", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _buttonComponentType = null;
    private static FieldInfo _buttonField = null;

    private MonoBehaviour _button = null;
    private bool _held = false;
}
