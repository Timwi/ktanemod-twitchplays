﻿using System;
using UnityEngine;

public static class InputInterceptor
{
    static InputInterceptor()
    {
        Type abstractControlsType = ReflectionHelper.FindType("AbstractControls");
        _inputSystems = Resources.FindObjectsOfTypeAll(abstractControlsType);
    }

    public static void EnableInput()
    {
        foreach (UnityEngine.Object inputSystem in _inputSystems)
        {
            try
            {
                ((MonoBehaviour)inputSystem).gameObject.SetActive(true);
            }
            catch (Exception ex)
            {
            }
        }
    }

    public static void DisableInput()
    {
        foreach (UnityEngine.Object inputSystem in _inputSystems)
        {
            try
            {
                ((MonoBehaviour)inputSystem).gameObject.SetActive(false);
            }
            catch (Exception ex)
            {
            }
        }
    }

    private static UnityEngine.Object[] _inputSystems = null;
}

