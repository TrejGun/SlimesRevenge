using UnityEngine;
using UnityEngine.UI;

namespace SlimesRevenge
{
    /// <summary>Cycles sprites on a UI <see cref="Image"/> (menu creature walk loops).</summary>
    public sealed class UiSpriteAnimator : MonoBehaviour
    {
        [SerializeField]
        private float frameSeconds = 0.12f;

        private Image image;
        private Sprite[] frames;
        private int index;
        private float elapsed;

        public void Play(Sprite[] walkFrames, float secondsPerFrame = 0.12f)
        {
            frames = walkFrames;
            frameSeconds = Mathf.Max(0.04f, secondsPerFrame);
            index = 0;
            elapsed = 0f;
            image = GetComponent<Image>();
            if (image != null && frames != null && frames.Length > 0)
            {
                image.sprite = frames[0];
                image.preserveAspect = true;
            }
        }

        private void Update()
        {
            if (image == null || frames == null || frames.Length <= 1)
            {
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            if (elapsed < frameSeconds)
            {
                return;
            }

            elapsed = 0f;
            index = (index + 1) % frames.Length;
            image.sprite = frames[index];
        }
    }
}
