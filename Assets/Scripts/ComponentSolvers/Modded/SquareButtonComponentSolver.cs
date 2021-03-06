﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class SquareButtonComponentSolver : ComponentSolver
{
    public SquareButtonComponentSolver(BombCommander bombCommander, MonoBehaviour bombComponent, IRCConnection ircConnection, CoroutineCanceller canceller) :
        base(bombCommander, bombComponent, ircConnection, canceller)
    {
        _button = (MonoBehaviour)_buttonField.GetValue(bombComponent.GetComponent(_componentType));
    }

    protected override IEnumerator RespondToCommandInternal(string inputCommand)
    {
        if (!_held && (inputCommand.Equals("tap", StringComparison.InvariantCultureIgnoreCase) ||
                       inputCommand.Equals("click", StringComparison.InvariantCultureIgnoreCase)))
        {
            yield return "tap";

            DoInteractionStart (_button);
            yield return new WaitForSeconds(0.1f);
            DoInteractionEnd(_button);
        }
        if (!_held && (inputCommand.StartsWith("tap ", StringComparison.InvariantCultureIgnoreCase) ||
                       inputCommand.StartsWith("click ", StringComparison.InvariantCultureIgnoreCase)))
        {
            yield return "tap2";

            IEnumerator releaseCoroutine = ReleaseCoroutine(inputCommand.Substring(inputCommand.IndexOf(' ')));
            while (releaseCoroutine.MoveNext())
            {
                yield return releaseCoroutine.Current;
            }
        }
        else if (!_held && (inputCommand.Equals("hold", StringComparison.InvariantCultureIgnoreCase) ||
                            inputCommand.Equals("press", StringComparison.InvariantCultureIgnoreCase)))
        {
            yield return "hold";

            _held = true;
            DoInteractionStart(_button);
            yield return new WaitForSeconds(2.0f);
        }
        else if (_held && inputCommand.StartsWith("release ", StringComparison.InvariantCultureIgnoreCase))
        {
            IEnumerator releaseCoroutine = ReleaseCoroutine(inputCommand.Substring(inputCommand.IndexOf(' ')));
            while (releaseCoroutine.MoveNext())
            {
                yield return releaseCoroutine.Current;
            }
        }
    }

    private IEnumerator ReleaseCoroutine(string second)
    {
        string[] list = second.Split(' ');
        List<int> sortedTimes = new List<int>();
        foreach(string value in list)
        {
            int time = -1;
            if(!int.TryParse(value, out time))
            {
                int pos = value.IndexOf(':');
                if(pos == -1) continue;
                int min, sec;
                if(!int.TryParse(value.Substring(0, pos), out min)) continue;
                if(!int.TryParse(value.Substring(pos+1), out sec)) continue;
                time = min * 60 + sec;
            }
            sortedTimes.Add(time);
        }
        sortedTimes.Sort();
        sortedTimes.Reverse();
        if(sortedTimes.Count == 0) yield break;

        yield return "release";

        MonoBehaviour timerComponent = (MonoBehaviour)CommonReflectedTypeInfo.GetTimerMethod.Invoke(BombCommander.Bomb, null);

        string secondString = second.ToString();

        int timeTarget = sortedTimes[0];
        sortedTimes.RemoveAt(0);
        float timeRemaining = float.PositiveInfinity;
        while (timeRemaining > 0.0f)
        {
            if (Canceller.ShouldCancel)
            {
                Canceller.ResetCancel();
                yield break;
            }

            timeRemaining = (int)((float)CommonReflectedTypeInfo.TimeRemainingField.GetValue(timerComponent) + 0.25f);

            if (timeRemaining < timeTarget)
            {
                if(sortedTimes.Count == 0) yield break;
                timeTarget = sortedTimes[0];
                sortedTimes.RemoveAt(0);
                continue;
            }
            if (timeRemaining == timeTarget)
            {
                if (!_held)
                {
                    DoInteractionStart(_button);
                    yield return new WaitForSeconds(0.1f);
                }
                DoInteractionEnd(_button);
                _held = false;
                break;
            }

            yield return null;
        }
    }

    static SquareButtonComponentSolver()
    {
        _componentType = ReflectionHelper.FindType("AdvancedButton");
        _buttonField = _componentType.GetField("Button", BindingFlags.Public | BindingFlags.Instance);
    }

    private static Type _componentType = null;
    private static FieldInfo _buttonField = null;

    private MonoBehaviour _button = null;
    private bool _held = false;
}
