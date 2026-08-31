using UnityEngine;
using UnityEngine.UI;

namespace SlimesRevenge
{
    /// <summary>
    /// Attachable FDR clip player. Drives a world <see cref="SpriteRenderer"/> or a UI
    /// <see cref="Image"/>. Sheet indices come from <see cref="FdrSheetLayout"/> via
    /// <see cref="CreatureSheet"/> — the same component works for every pack enemy.
    /// </summary>
    public sealed class CreatureSpriteAnimator : MonoBehaviour
    {
        public const float DefaultFrameSeconds = 0.12f;

        [SerializeField]
        private float frameSeconds = DefaultFrameSeconds;

        private SpriteRenderer body;
        private Image image;
        private Sprite[] frames;
        private int index;
        private float elapsed;
        private bool loop = true;
        private bool finished;
        private System.Action onComplete;

        public CreatureKind Kind { get; set; }

        public WalkFacing Facing { get; set; } = WalkFacing.South;

        /// <summary>
        /// Adds this component on a non-slime creature with a <see cref="SpriteRenderer"/>.
        /// Slime walk frames come from substance sheets, not <see cref="CreatureSheet"/>.
        /// </summary>
        public static CreatureSpriteAnimator Attach(Creature creature)
        {
            if (creature == null || creature is Slime || !Application.isPlaying)
            {
                return null;
            }

            var animator = creature.GetComponent<CreatureSpriteAnimator>();
            if (animator == null)
            {
                if (creature.GetComponent<SpriteRenderer>() == null)
                {
                    return null;
                }

                animator = creature.gameObject.AddComponent<CreatureSpriteAnimator>();
            }

            animator.Kind = creature.Kind;
            return animator;
        }

        public static void PlayIdle(Creature creature)
        {
            var animator = Attach(creature);
            animator?.PlayIdle();
        }

        public static void PlayWalk(Creature creature, Vector2Int toward)
        {
            var animator = Attach(creature);
            if (animator == null)
            {
                return;
            }

            animator.Facing = FdrSheetLayout.FacingToward(creature.Cell, toward);
            animator.PlayLoop(FdrClip.Walk);
        }

        public static void PlayAttack(Creature creature, Vector2Int toward)
        {
            var animator = Attach(creature);
            if (animator == null)
            {
                return;
            }

            animator.Facing = FdrSheetLayout.FacingToward(creature.Cell, toward);
            animator.PlayOnce(FdrClip.Attack, animator.PlayIdle);
        }

        public static void PlayDamage(Creature creature)
        {
            var animator = Attach(creature);
            if (animator == null)
            {
                return;
            }

            animator.PlayOnce(FdrClip.Damage, animator.PlayIdle);
        }

        public static void PlayDeath(Creature creature)
        {
            var animator = Attach(creature);
            animator?.PlayOnce(FdrClip.Dead, null);
        }

        public void PlayIdle()
        {
            // FDR idle is a single C2 pose; on the board we loop walk so standing
            // pack enemies keep acting in-place (flap / bob), same as the slime.
            PlayLoop(FdrClip.Walk);
        }

        public void PlayPortrait()
        {
            Play(CreatureSheet.Portrait(Kind));
        }

        public void Play(Sprite[] clipFrames, float secondsPerFrame = DefaultFrameSeconds)
        {
            PlayInternal(clipFrames, secondsPerFrame, looping: true, null);
        }

        public void PlayOnce(
            Sprite[] clipFrames,
            float secondsPerFrame = DefaultFrameSeconds,
            System.Action done = null
        )
        {
            PlayInternal(clipFrames, secondsPerFrame, looping: false, done);
        }

        public void PlayLoop(FdrClip clip)
        {
            var clipFrames = CreatureSheet.Clip(Kind, Facing, clip);
            if (clipFrames == null || clipFrames.Length == 0)
            {
                return;
            }

            Play(clipFrames);
        }

        public void PlayOnce(FdrClip clip, System.Action done)
        {
            var clipFrames = CreatureSheet.Clip(Kind, Facing, clip);
            if (clipFrames == null || clipFrames.Length == 0)
            {
                done?.Invoke();
                return;
            }

            PlayOnce(clipFrames, frameSeconds, done);
        }

        public void Tick(float deltaSeconds)
        {
            if (finished || !TargetReady() || frames == null || frames.Length <= 1)
            {
                return;
            }

            elapsed += deltaSeconds;
            if (elapsed < frameSeconds)
            {
                return;
            }

            elapsed = 0f;
            if (index + 1 < frames.Length)
            {
                index++;
                Apply(frames[index]);
                return;
            }

            if (loop)
            {
                index = 0;
                Apply(frames[index]);
                return;
            }

            finished = true;
            var done = onComplete;
            onComplete = null;
            done?.Invoke();
        }

        private void PlayInternal(
            Sprite[] clipFrames,
            float secondsPerFrame,
            bool looping,
            System.Action done
        )
        {
            frames = clipFrames;
            frameSeconds = Mathf.Max(0.04f, secondsPerFrame);
            index = 0;
            elapsed = 0f;
            loop = looping;
            finished = false;
            onComplete = done;
            CacheTargets();
            if (frames != null && frames.Length > 0)
            {
                Apply(frames[0]);
            }
        }

        private void CacheTargets()
        {
            if (body == null)
            {
                body = GetComponent<SpriteRenderer>();
            }

            if (image == null)
            {
                image = GetComponent<Image>();
            }
        }

        private bool TargetReady()
        {
            CacheTargets();
            if (body != null && body.enabled)
            {
                return true;
            }

            return image != null && image.enabled;
        }

        private void Apply(Sprite sprite)
        {
            if (body != null)
            {
                body.sprite = sprite;
            }

            if (image != null)
            {
                image.sprite = sprite;
                image.preserveAspect = true;
            }
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }
    }
}
