using UnityEngine;

namespace Slime
{
    public sealed class StaticScreen : MonoBehaviour
    {
        [SerializeField] private int targetFrameRate = 30;

        private void Awake()
        {
            Application.targetFrameRate = targetFrameRate;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;
        }
    }
}
