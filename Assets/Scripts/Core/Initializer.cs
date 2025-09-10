using UnityEngine;
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