namespace SlimesRevenge
{
    public static class TextKey
    {
        public const string MenuAttack = "menu.attack";
        public const string MenuMess = "menu.mess";
        public const string MenuCollect = "menu.collect";
        public const string MenuDevour = "menu.devour";
        public const string MenuInfo = "menu.info";
        public const string GameOver = "game.over";
        public const string GameMenu = "game.menu";
        public const string SubstanceWater = "substance.water";
        public const string SubstanceOil = "substance.oil";
        public const string SubstancePoison = "substance.poison";
        public const string SubstanceAcid = "substance.acid";
        public const string SubstanceBlood = "substance.blood";
        public const string SubstanceLava = "substance.lava";
        public const string SubstanceMercury = "substance.mercury";
        public const string StatusPoisoned = "status.poisoned";
        public const string StatusCorroding = "status.corroding";
        public const string StatusInstability = "status.instability";
        public const string StatusBurning = "status.burning";
        public const string StatusWet = "status.wet";
        public const string StatusFireproof = "status.fireproof";
        public const string StatusInvisible = "status.invisible";
        public const string StatusFlammable = "status.flammable";
        public const string StatusRetaliation = "status.retaliation";
        public const string StatusRegeneration = "status.regeneration";
        public const string StatusVampirism = "status.vampirism";
        public const string StatusPoisonous = "status.poisonous";
        public const string StatusHatesRats = "status.hates_rats";
        public const string StatusHatesCats = "status.hates_cats";
        public const string StatusFearsCats = "status.fears_cats";
        public const string StatusFearsDogs = "status.fears_dogs";
        public const string StatusFearsWater = "status.fears_water";
        public const string StatusDigesting = "status.digesting";
        public const string StatusPoisonedDesc = "status.poisoned.desc";
        public const string StatusCorrodingDesc = "status.corroding.desc";
        public const string StatusInstabilityDesc = "status.instability.desc";
        public const string StatusBurningDesc = "status.burning.desc";
        public const string StatusWetDesc = "status.wet.desc";
        public const string StatusFireproofDesc = "status.fireproof.desc";
        public const string StatusInvisibleDesc = "status.invisible.desc";
        public const string StatusFlammableDesc = "status.flammable.desc";
        public const string StatusRetaliationDesc = "status.retaliation.desc";
        public const string StatusRegenerationDesc = "status.regeneration.desc";
        public const string StatusVampirismDesc = "status.vampirism.desc";
        public const string StatusPoisonousDesc = "status.poisonous.desc";
        public const string StatusHatesRatsDesc = "status.hates_rats.desc";
        public const string StatusHatesCatsDesc = "status.hates_cats.desc";
        public const string StatusFearsCatsDesc = "status.fears_cats.desc";
        public const string StatusFearsDogsDesc = "status.fears_dogs.desc";
        public const string StatusFearsWaterDesc = "status.fears_water.desc";
        public const string StatusDigestingDesc = "status.digesting.desc";
        public const string LogHit = "log.hit";
        public const string LogHitWith = "log.hit_with";
        public const string LogGains = "log.gains";
        public const string LogStepsPuddle = "log.steps_puddle";
        public const string LogArmor = "log.armor";
        public const string LogHp = "log.hp";
        public const string LogVolume = "log.volume";
        public const string LogDamageTaken = "log.damage_taken";
        public const string LogHeals = "log.heals";
        public const string LogDies = "log.dies";
        public const string LogTurn = "log.turn";
        public const string LogCollects = "log.collects";
        public const string LogMesses = "log.messes";
        public const string LogDigests = "log.digests";
        public const string LogDevours = "log.devours";
        public const string LogDominance = "log.dominance";
        public const string LogDominanceLost = "log.dominance_lost";
        public const string LogGrows = "log.grows";
        public const string LogRunStarted = "log.run_started";
        public const string LogDuelStarted = "log.duel_started";
        public const string LogCampaignStarted = "log.campaign_started";
        public const string LogCollapse = "log.collapse";
        public const string LogExpand = "log.expand";
        public const string ModeHardcore = "mode.hardcore";
        public const string ModeSoftcore = "mode.softcore";
        public const string StatusTimer = "status.timer";
        public const string StatusForever = "status.forever";
        public const string UiBack = "ui.back";
        public const string CardVolume = "card.volume";
        public const string CardStatuses = "card.statuses";
        public const string CardApplies = "card.applies";
        public const string CardDominance = "card.dominance";
        public const string CardPower = "card.power";
        public const string CardCorrosion = "card.corrosion";
        public const string CardNone = "card.none";
        public const string CardCorpseSuffix = "card.corpse_suffix";
        public const string CardVolumeOf = "card.volume_of";
        public const string CardDominant = "card.dominant";
        public const string CardDigesting = "card.digesting";
        public const string CardHp = "card.hp";
        public const string CardArmor = "card.armor";
        public const string CardVolumeCount = "card.volume_count";
        public const string CardDecay = "card.decay";
        public const string MenuCorpseChoice = "menu.corpse_choice";
        public const string MenuPuddleChoice = "menu.puddle_choice";
        public const string MenuTitle = "menu.title";
        public const string MenuDuel = "menu.duel";
        public const string MenuCampaign = "menu.campaign";
        public const string MenuCredits = "menu.credits";
        public const string MenuQuit = "menu.quit";
        public const string MenuCampaignSoon = "menu.campaign_soon";
        public const string MenuCreditsTitle = "menu.credits_title";
        public const string MenuCreditsBody = "menu.credits_body";
        public const string MenuDuelSetup = "menu.duel_setup";
        public const string MenuOpponent = "menu.opponent";
        public const string MenuLoadoutSlot = "menu.loadout_slot";
        public const string MenuStartDuel = "menu.start_duel";
        public const string MenuNext = "menu.next";
        public const string MenuPickOpponent = "menu.pick_opponent";
        public const string MenuPickSubstance = "menu.pick_substance";
        public const string CreatureSlime = "creature.slime";
        public const string CreatureRat = "creature.rat";
        public const string CreatureCat = "creature.cat";
        public const string CreatureDog = "creature.dog";
        public const string CreatureBat = "creature.bat";
        public const string CreatureScorpion = "creature.scorpion";
        public const string CreatureMushroom = "creature.mushroom";
        public const string CreatureUndine = "creature.undine";
        public const string CreatureCactus = "creature.cactus";
        public const string CreatureMandragora = "creature.mandragora";
        public const string CreatureEye = "creature.eye";

        public static string ForCreature(CreatureKind kind)
        {
            switch (kind)
            {
                case CreatureKind.Slime:
                    return CreatureSlime;
                case CreatureKind.Rat:
                    return CreatureRat;
                case CreatureKind.Cat:
                    return CreatureCat;
                case CreatureKind.Dog:
                    return CreatureDog;
                case CreatureKind.Bat:
                    return CreatureBat;
                case CreatureKind.Scorpion:
                    return CreatureScorpion;
                case CreatureKind.Mushroom:
                    return CreatureMushroom;
                case CreatureKind.Undine:
                    return CreatureUndine;
                case CreatureKind.Cactus:
                    return CreatureCactus;
                case CreatureKind.Mandragora:
                    return CreatureMandragora;
                case CreatureKind.Eye:
                    return CreatureEye;
                default:
                    return CreatureSlime;
            }
        }

        public static readonly string[] All =
        {
            MenuAttack,
            MenuMess,
            MenuCollect,
            MenuDevour,
            MenuInfo,
            GameOver,
            GameMenu,
            SubstanceWater,
            SubstanceOil,
            SubstancePoison,
            SubstanceAcid,
            SubstanceBlood,
            SubstanceLava,
            SubstanceMercury,
            StatusPoisoned,
            StatusCorroding,
            StatusInstability,
            StatusBurning,
            StatusWet,
            StatusFireproof,
            StatusInvisible,
            StatusFlammable,
            StatusRetaliation,
            StatusRegeneration,
            StatusVampirism,
            StatusPoisonous,
            StatusHatesRats,
            StatusHatesCats,
            StatusFearsCats,
            StatusFearsDogs,
            StatusFearsWater,
            StatusDigesting,
            StatusPoisonedDesc,
            StatusCorrodingDesc,
            StatusInstabilityDesc,
            StatusBurningDesc,
            StatusWetDesc,
            StatusFireproofDesc,
            StatusInvisibleDesc,
            StatusFlammableDesc,
            StatusRetaliationDesc,
            StatusRegenerationDesc,
            StatusVampirismDesc,
            StatusPoisonousDesc,
            StatusHatesRatsDesc,
            StatusHatesCatsDesc,
            StatusFearsCatsDesc,
            StatusFearsDogsDesc,
            StatusFearsWaterDesc,
            StatusDigestingDesc,
            LogHit,
            LogHitWith,
            LogGains,
            LogStepsPuddle,
            LogArmor,
            LogHp,
            LogVolume,
            LogDamageTaken,
            LogHeals,
            LogDies,
            LogTurn,
            LogCollects,
            LogMesses,
            LogDigests,
            LogDevours,
            LogDominance,
            LogDominanceLost,
            LogGrows,
            LogRunStarted,
            LogDuelStarted,
            LogCampaignStarted,
            LogCollapse,
            LogExpand,
            ModeHardcore,
            ModeSoftcore,
            StatusTimer,
            StatusForever,
            UiBack,
            CardVolume,
            CardStatuses,
            CardApplies,
            CardDominance,
            CardPower,
            CardCorrosion,
            CardNone,
            CardCorpseSuffix,
            CardVolumeOf,
            CardDominant,
            CardDigesting,
            CardHp,
            CardArmor,
            CardVolumeCount,
            CardDecay,
            MenuCorpseChoice,
            MenuPuddleChoice,
            MenuTitle,
            MenuDuel,
            MenuCampaign,
            MenuCredits,
            MenuQuit,
            MenuCampaignSoon,
            MenuCreditsTitle,
            MenuCreditsBody,
            MenuDuelSetup,
            MenuOpponent,
            MenuLoadoutSlot,
            MenuStartDuel,
            MenuNext,
            MenuPickOpponent,
            MenuPickSubstance,
            CreatureSlime,
            CreatureRat,
            CreatureCat,
            CreatureDog,
            CreatureBat,
            CreatureScorpion,
            CreatureMushroom,
            CreatureUndine,
            CreatureCactus,
            CreatureMandragora,
            CreatureEye,
        };
    }
}
