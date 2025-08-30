using UnityEngine;
using System;
using System.Collections;
namespace Assets.Scripts.Core
{
    public class Initializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void InitializeBeforeSceneLoad()
        {

        }
    }
}