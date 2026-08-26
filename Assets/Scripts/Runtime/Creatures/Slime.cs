using UnityEngine;

namespace SlimesRevenge
{
    public sealed class Slime : Creature
    {
        public override CreatureKind Kind => CreatureKind.Slime;

        [SerializeField] private Sprite waterSprite;
        [SerializeField] private Sprite oilSprite;
        [SerializeField] private Sprite poisonSprite;
        [SerializeField] private Sprite acidSprite;
        [SerializeField] private Sprite bloodSprite;
        [SerializeField] private Sprite lavaSprite;

        private SpriteRenderer body;

        public static void FillStarting(Volume volume)
        {
            volume.Fill(new Water(), new Water(), new Oil(), new Poison(), new Acid(), new Lava());
        }

        private void Awake()
        {
            body = GetComponent<SpriteRenderer>();
            SetMaxHitPoints(0);
            if (Volume.UnitCount == 0)
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

            var sprite = SpriteFor(SlimeAppearance.FromVolume(Volume));
            if (sprite != null && body.sprite != sprite)
            {
                body.sprite = sprite;
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
