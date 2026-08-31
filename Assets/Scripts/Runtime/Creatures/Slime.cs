using UnityEngine;

namespace SlimesRevenge
{
    public sealed class Slime : Creature
    {
        public override CreatureKind Kind => CreatureKind.Slime;

        [SerializeField]
        private Sprite waterSprite;

        [SerializeField]
        private Sprite oilSprite;

        [SerializeField]
        private Sprite poisonSprite;

        [SerializeField]
        private Sprite acidSprite;

        [SerializeField]
        private Sprite bloodSprite;

        [SerializeField]
        private Sprite lavaSprite;

        private SpriteRenderer body;
        private CreatureSpriteAnimator animator;
        private SlimeLook shownLook;
        private WalkFacing facing = WalkFacing.East;
        private bool appearanceStarted;

        public static void FillStarting(Volume volume)
        {
            volume.Fill(new Water(), new Water(), new Oil(), new Poison(), new Acid(), new Lava());
        }

        private void Awake()
        {
            body = GetComponent<SpriteRenderer>();
            SetMaxHitPoints(0);
            // Duel loadout is applied by WorldView.Start; campaign uses FillStarting.
            if (
                Volume.UnitCount == 0
                && !(RunConfig.TryPeek(out var run) && run.Kind == RunKind.Duel)
            )
            {
                FillStarting(Volume);
            }

            RefreshVolumeStatuses();
            ApplyAppearance();
        }

        private void LateUpdate()
        {
            ApplyAppearance();
        }

        public void Face(Vector2Int cell)
        {
            facing = FdrSheetLayout.FacingToward(Cell, cell);
            appearanceStarted = false;
        }

        public void ApplyAppearance()
        {
            if (body == null)
            {
                body = GetComponent<SpriteRenderer>();
            }

            if (body == null)
            {
                return;
            }

            var look = SlimeAppearance.FromVolume(Volume);
            if (appearanceStarted && look == shownLook)
            {
                return;
            }

            shownLook = look;
            appearanceStarted = true;
            var sheet = SpriteFor(look);
            var frames = SlimeSprites.Walk(sheet, facing);
            if (frames.Length > 1)
            {
                if (animator == null)
                {
                    animator = GetComponent<CreatureSpriteAnimator>();
                    if (animator == null)
                    {
                        animator = gameObject.AddComponent<CreatureSpriteAnimator>();
                    }
                }

                animator.Play(frames);
                return;
            }

            if (sheet != null && body.sprite != sheet)
            {
                body.sprite = sheet;
            }
        }

        private Sprite SpriteFor(SlimeLook look)
        {
            switch (look)
            {
                case SlimeLook.Oil:
                    return oilSprite;
                case SlimeLook.Poison:
                    return poisonSprite;
                case SlimeLook.Acid:
                    return acidSprite;
                case SlimeLook.Blood:
                    return bloodSprite;
                case SlimeLook.Lava:
                    return lavaSprite;
                default:
                    return waterSprite;
            }
        }
    }
}
