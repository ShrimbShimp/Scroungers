using System;
using BepInEx;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RWCustom;
using Menu;
using MoreSlugcats;
using Fisobs;
using Fisobs.Core;
using Fisobs.Creatures;
using Fisobs.Sandbox;
using On;
using IL;
using DevInterface;
using System.Drawing;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using ArenaBehaviors;
using System.Runtime.Remoting.Messaging;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Color = UnityEngine.Color;
using Unity.Mathematics;
using Unity.Collections;

namespace scroungerproject
{

    #region extra math stuff
    public static class ExtraExtentions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 ToF2(this Vector2 v)
        {
            return new float2(v.x, v.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 ToV2(this float2 v)
        {
            return new Vector2(v.x, v.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float magnitude(this float2 f)
        {
            return math.sqrt(f.x * f.x + f.y * f.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float sqrMagnitude(this float2 f)
        {
            return f.x * f.x + f.y * f.y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 normalized(this float2 f)
        {
            float num = f.magnitude();
            if ((double)num > 9.999999747378752E-06)
            {
                return f / num;
            }
            return float2.zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ind(int row, int col, int width)
        {
            return width * row + col;
        }

        public static void CopyToAlt(this NativeArray<Vector2> na, Vector2[,] dim)
        {
            int length = dim.GetLength(0);
            int length2 = dim.GetLength(1);
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < length2; j++)
                {
                    dim[i, j] = na[ExtraExtentions.ind(i, j, length2)];
                }
            }
        }

        public static void CopyToAlt(this NativeArray<Vector3> na, Vector3[,] dim)
        {
            int length = dim.GetLength(0);
            int length2 = dim.GetLength(1);
            for (int i = 0; i < length; i++)
            {
                for (int j = 0; j < length2; j++)
                {
                    dim[i, j] = na[ExtraExtentions.ind(i, j, length2)];
                }
            }
        }

        public struct Indexer
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int ind(int row, int col)
            {
                return this.width * row + col;
            }
            public int width;
        }
    }
    #endregion

    [BepInPlugin(MOD_ID, "shrimb.scroungers", "0.1.0")]

    class Plugin : BaseUnityPlugin
    {
        private const string MOD_ID = "shrimb.scroungers";

        FAtlas ScrIcon;

        public void OnEnable()
        {
            On.RainWorld.OnModsInit += Extras.WrapInit(LoadResources);
            On.RainWorld.OnModsInit += Init;

            Content.Register(new ScrCritob());

            On.ScavengerGraphics.DrawSprites += ScavengerGraphics_DrawSprites1;
            On.ScavengerGraphics.IndividualVariations.ctor += IndividualVariations_ctor;
            On.ScavengerGraphics.ctor += ScavengerGraphics_ctor;
            On.ScavengerGraphics.GenerateColors += ScavengerGraphics_GenerateColors;
            On.ScavengerGraphics.Update += ScavengerGraphics_Update;
            On.ScavengerGraphics.Reset += ScavengerGraphics_Reset;
            On.ScavengerGraphics.Eartlers.GenerateSegments += Eartlers_GenerateSegments;
            On.Scavenger.ctor += Scavenger_ctor;
            On.Scavenger.Update += Scavenger_Update;
            On.ScavengerCosmetic.Tail.ctor += Tail_ctor;
            On.ScavengerCosmetic.Tail.ApplyPalette += Tail_ApplyPalette;
            On.ScavengerCosmetic.Tail.InitiateSprites += Tail_InitiateSprites;
            On.ScavengerCosmetic.Tail.DrawSprites += Tail_DrawSprites;
            IL.ScavengerGraphics.Update += ScavengerGraphics_Update1;
        }

        private void LoadResources(RainWorld rainWorld)
        {
        }

        private void Init(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);

            ScrIcon ??= Futile.atlasManager.LoadAtlas("atlases/Kill_Scr");

            if (ScrIcon == null)
                Logger.LogError("Scrounger icon not found!");
        }

        #region CWT
        public static ConditionalWeakTable<Scavenger, ScroungerInfo> ScroungerCWT = new();
        public class ScroungerInfo
        {
            public WeakReference<Scavenger> scrRef;

            public ScroungerInfo(Scavenger sc)
            {
                scrRef = new WeakReference<Scavenger>(sc);
            }

            public void JumpLogicUpdate()
            {
                if (!scrRef.TryGetTarget(out Scavenger self))
                    return;

                if (self.dead)
                {
                    return;
                }
                if (!self.Consious && self.animation != null && (self.animation.id == MoreSlugcatsEnums.ScavengerAnimationID.PrepareToJump || self.animation.id == MoreSlugcatsEnums.ScavengerAnimationID.Jumping))
                {
                    self.animation = null;
                }
                for (int i = self.jumpFinders.Count - 1; i >= 0; i--)
                {
                    if (self.jumpFinders[i] != null)
                    {
                        if (self.jumpFinders[i].slatedForDeletion)
                        {
                            self.jumpFinders.RemoveAt(i);
                        }
                        else if (self.safariControlled)
                        {
                            self.jumpFinders[i].Destroy();
                        }
                        else
                        {
                            self.jumpFinders[i].Update();
                        }
                    }
                }
                if (self.safariControlled)
                {
                    if (self.controlledJumpFinder != null && self.controlledJumpFinder.startPos != self.abstractCreature.pos.Tile)
                    {
                        self.controlledJumpFinder.Destroy();
                        self.controlledJumpFinder = null;
                    }
                    if (self.controlledJumpFinder == null)
                    {
                        self.controlledJumpFinder = new Scavenger.JumpFinder(self.room, self, self.abstractCreature.pos.Tile);
                    }
                    self.controlledJumpFinder.Update();
                    self.controlledJumpFinder.fade = 0;
                    if (self.inputWithDiagonals != null && self.inputWithDiagonals.Value.jmp && !self.lastInputWithDiagonals.Value.jmp && !self.inputWithDiagonals.Value.pckp && self.controlledJumpFinder.bestJump != null)
                    {
                        self.InitiateJump(self.controlledJumpFinder);
                    }
                }
                else if (self.controlledJumpFinder != null)
                {
                    self.controlledJumpFinder.Destroy();
                    self.controlledJumpFinder = null;
                }
                if (self.animation != null && self.animation.id == MoreSlugcatsEnums.ScavengerAnimationID.Jumping)
                {
                    self.JumpingUpdate();
                }
                else if (self.InStandardRunMode)
                {
                    self.RunningUpdate();
                }
                if (self.actOnJump != null && (self.animation == null || (self.animation.id != MoreSlugcatsEnums.ScavengerAnimationID.PrepareToJump && self.animation.id != MoreSlugcatsEnums.ScavengerAnimationID.Jumping)))
                {
                    self.actOnJump.fade++;
                    if (self.actOnJump.fade > 40)
                    {
                        self.actOnJump = null;
                    }
                }
            }

            public float tailThickness;
            public int tailPattern;
        }
        #endregion

        #region Critob
        sealed class ScrCritob : Critob
        {
            public static readonly CreatureTemplate.Type Scrounger = new("Scrounger", true);
            public static readonly MultiplayerUnlocks.SandboxUnlockID ScrUnlock = new("Scrounger", true);

            public ScrCritob() : base(Scrounger)
            {
                LoadedPerformanceCost = 300f;
                SandboxPerformanceCost = new(linear: 0.5f, exponential: 0.925f);
                ShelterDanger = ShelterDanger.Safe;
                CreatureName = "Scrounger";
                Icon = new SimpleIcon("Kill_Scr", new Color(1f, 1f, 1f, 1f));

                RegisterUnlock(killScore: KillScore.Configurable(2), ScrUnlock, parent: MultiplayerUnlocks.SandboxUnlockID.Scavenger, data: 0);
            }

            public override int ExpeditionScore() => 30;
            public override Color DevtoolsMapColor(AbstractCreature acrit) => Color.white;
            public override string DevtoolsMapName(AbstractCreature acrit) => "Scr";
            public override IEnumerable<string> WorldFileAliases() => new[] { "scrounger" };

            public override IEnumerable<RoomAttractivenessPanel.Category> DevtoolsRoomAttraction() => new[]
            {
               RoomAttractivenessPanel.Category.All,
            };

            public override CreatureTemplate CreateTemplate()
            {
                CreatureTemplate temp = new CreatureFormula(CreatureTemplate.Type.Scavenger, Type, "Scrounger")
                {
                    DefaultRelationship = new(CreatureTemplate.Relationship.Type.Ignores, 0.1f),
                    DamageResistances = new() { Base = 0.9f, Blunt = 1.1f },
                    StunResistances = new() { Base = 0.9f, Blunt = 1.1f },
                    HasAI = true,
                    InstantDeathDamage = 1,
                    Pathing = PreBakedPathing.Ancestral(MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite),
                }.IntoTemplate();
                temp.meatPoints = 3;
                temp.bodySize = 1f;
                return temp;
            }

            public override void EstablishRelationships()
            {
                Relationships s = new(Scrounger);

                foreach (var template in StaticWorld.creatureTemplates)
                {
                    if (template.quantified)
                    {
                        s.Ignores(template.type);
                        s.IgnoredBy(template.type);
                    }
                }

                #region relationshipslist
                s.HasDynamicRelationship(CreatureTemplate.Type.Slugcat, 1f);
                s.HasDynamicRelationship(MoreSlugcatsEnums.CreatureTemplateType.SlugNPC, 1f);

                s.IsInPack(CreatureTemplate.Type.Scavenger, 1f);
                s.IsInPack(Scrounger, 1f);
                s.IsInPack(MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite, 1f);

                s.Fears(CreatureTemplate.Type.Vulture, 0.85f);
                s.Fears(CreatureTemplate.Type.KingVulture, 0.99f);
                s.Fears(MoreSlugcatsEnums.CreatureTemplateType.MirosVulture, 1f);
                s.Fears(CreatureTemplate.Type.LizardTemplate, 0.6f);
                s.Fears(MoreSlugcatsEnums.CreatureTemplateType.TerrorLongLegs, 1f);
                s.Fears(CreatureTemplate.Type.DaddyLongLegs, 1f);
                s.Fears(CreatureTemplate.Type.BrotherLongLegs, 0.9f);
                s.Fears(CreatureTemplate.Type.Centipede, 0.7f);
                s.Fears(CreatureTemplate.Type.RedCentipede, 0.99f);
                s.Fears(MoreSlugcatsEnums.CreatureTemplateType.AquaCenti, 0.9f);
                s.Fears(CreatureTemplate.Type.TentaclePlant, 0.7f);
                s.Fears(CreatureTemplate.Type.PoleMimic, 0.4f);
                s.Fears(CreatureTemplate.Type.MirosBird, 0.8f);
                s.Fears(CreatureTemplate.Type.BigSpider, 0.6f);
                s.Fears(CreatureTemplate.Type.SpitterSpider, 0.8f);
                s.Fears(CreatureTemplate.Type.BigEel, 1f);
                s.Fears(CreatureTemplate.Type.DropBug, 0.65f);

                s.UncomfortableAround(CreatureTemplate.Type.LanternMouse, 0.2f);
                s.UncomfortableAround(CreatureTemplate.Type.Snail, 0.7f);
                s.UncomfortableAround(CreatureTemplate.Type.GarbageWorm, 0.9f);
                s.UncomfortableAround(CreatureTemplate.Type.BigNeedleWorm, 0.6f);
                s.UncomfortableAround(CreatureTemplate.Type.SmallCentipede, 0.1f);

                s.Ignores(CreatureTemplate.Type.SmallNeedleWorm);
                s.Ignores(MoreSlugcatsEnums.CreatureTemplateType.Yeek);

                s.EatenBy(CreatureTemplate.Type.BigSpider, 0.65f);
                s.EatenBy(CreatureTemplate.Type.Vulture, 0.85f);
                s.EatenBy(CreatureTemplate.Type.KingVulture, 0.99f);
                s.EatenBy(MoreSlugcatsEnums.CreatureTemplateType.MirosVulture, 1f);
                s.EatenBy(CreatureTemplate.Type.LizardTemplate, 0.6f);
                s.EatenBy(CreatureTemplate.Type.SpitterSpider, 0.8f);
                #endregion
            }

            public override ArtificialIntelligence CreateRealizedAI(AbstractCreature absCt) => new ScavengerAI(absCt, absCt.world);
            public override AbstractCreatureAI CreateAbstractAI(AbstractCreature absCt) => new ScavengerAbstractAI(absCt.world, absCt);

            public override Creature CreateRealizedCreature(AbstractCreature absCt) => new Scavenger(absCt, absCt.world);

            public override void LoadResources(RainWorld rainWorld) { }

            public override CreatureTemplate.Type ArenaFallback() => CreatureTemplate.Type.Scavenger;
        }
        #endregion

        public class CustomCreatures
        {
            public static bool isScrounger(AbstractCreature cr)
            {
                if (cr.creatureTemplate.type == ScrCritob.Scrounger)
                {
                    return true;
                }
                else return false;
            }
        }

        #region scrounger code
        private void ScavengerGraphics_Update1(ILContext il)
        {
            try
            {
                ILCursor cursor = new ILCursor(il);
                cursor.GotoNext(
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<ScavengerGraphics>("tail"),
                    i => i.MatchLdlen(),
                    i => i.Match(OpCodes.Brfalse)
                    );
                cursor.Index += 4;
                ILLabel label = (ILLabel)cursor.Prev.Operand;
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<ScavengerGraphics, bool>>((scacgraphics) =>
                {
                    return CustomCreatures.isScrounger(scacgraphics.scavenger.abstractCreature) == false;
                });
                cursor.Emit(OpCodes.Brfalse, label);
            }
            catch (Exception e)
            {
                base.Logger.LogError("scavenger graphics update IL hook encountered an error: " + e);
                throw;
            }
        }

        private void Scavenger_ctor(On.Scavenger.orig_ctor orig, Scavenger self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (CustomCreatures.isScrounger(self.abstractCreature))
            {
                if (!ScroungerCWT.TryGetValue(self, out _))
                    ScroungerCWT.Add(self, new ScroungerInfo(self));

                self.bodyChunkConnections[0] = new PhysicalObject.BodyChunkConnection(self.bodyChunks[0], self.bodyChunks[1], 8f, PhysicalObject.BodyChunkConnection.Type.Normal, 1f, -1f);
                self.bodyChunkConnections[1] = new PhysicalObject.BodyChunkConnection(self.bodyChunks[0], self.bodyChunks[2], 11f, PhysicalObject.BodyChunkConnection.Type.Pull, 0.8f, -1f);

                self.abstractCreature.personality.nervous *= 2f;
                self.abstractCreature.personality.bravery *= 0.5f;
                self.abstractCreature.personality.dominance *= 0.5f;
                self.abstractCreature.personality.aggression *= 0.5f;

                if (self.abstractCreature.personality.nervous > 1f)
                    self.abstractCreature.personality.nervous = 1f;
            }
        }

        private void Scavenger_Update(On.Scavenger.orig_Update orig, Scavenger self, bool eu)
        {
            orig(self, eu);
            if (ScroungerCWT.TryGetValue(self, out ScroungerInfo scrounger))
                scrounger.JumpLogicUpdate();
        }

        private void ScavengerGraphics_DrawSprites1(On.ScavengerGraphics.orig_DrawSprites orig, ScavengerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPosV2)
        {
            orig(self, sLeaser, rCam, timeStacker, camPosV2);
            float2 floamt = camPosV2.ToF2();
            if (self.scavenger != null && CustomCreatures.isScrounger(self.scavenger.abstractCreature))
            {
                float darkness = rCam.PaletteDarkness();
                if (darkness > 0.5f)
                {
                    darkness -= rCam.room.LightSourceExposure(self.scavenger.mainBodyChunk.pos) * Custom.LerpMap(darkness, 0.5f, 1f, 0f, 0.5f);
                }
                if (darkness != self.darkness)
                {
                    self.ApplyPalette(sLeaser, rCam, rCam.currentPalette);
                }

                float flipfloat = Mathf.Lerp(self.lastFlip, self.flip, timeStacker);
                Vector2 headpos = Vector2.Lerp(self.drawPositions[self.headDrawPos, 1], self.drawPositions[self.headDrawPos, 0], timeStacker);
                Vector2 chestpos = Vector2.Lerp(self.drawPositions[self.chestDrawPos, 1], self.drawPositions[self.chestDrawPos, 0], timeStacker);
                Vector2 hipspos = Vector2.Lerp(self.drawPositions[self.hipsDrawPos, 1], self.drawPositions[self.hipsDrawPos, 0], timeStacker);
                Vector2 bodypos = (chestpos + hipspos) / 2f;
                bodypos -= Custom.PerpendicularVector((hipspos - chestpos).normalized) * Mathf.Lerp(5f, 15f, self.iVars.narrowWaist) * flipfloat;

                //chest positions
                sLeaser.sprites[self.ChestSprite].x = chestpos.x - camPosV2.x;
                sLeaser.sprites[self.ChestSprite].y = chestpos.y - camPosV2.y;
                sLeaser.sprites[self.ChestSprite].scaleX = 0.5f;
                sLeaser.sprites[self.ChestSprite].scaleY = 0.5f;
                sLeaser.sprites[self.ChestSprite].rotation = Custom.AimFromOneVectorToAnother(chestpos, bodypos);

                //hips positions
                sLeaser.sprites[self.HipSprite].x = hipspos.x - camPosV2.x;
                sLeaser.sprites[self.HipSprite].y = hipspos.y - camPosV2.y;
                Vector2 a = Custom.DirVec(hipspos, chestpos);

                #region waist
                //all divided by 2
                (sLeaser.sprites[self.WaistSprite] as TriangleMesh).MoveVertice(0, hipspos + Custom.PerpendicularVector((hipspos - bodypos).normalized) * self.scavenger.bodyChunks[1].rad * Mathf.Lerp(0.65f, 0.9f, (self.iVars.WaistWidth / 2)) - camPosV2);
                (sLeaser.sprites[self.WaistSprite] as TriangleMesh).MoveVertice(1, hipspos - Custom.PerpendicularVector((hipspos - bodypos).normalized) * self.scavenger.bodyChunks[1].rad * Mathf.Lerp(0.65f, 0.9f, (self.iVars.WaistWidth / 2)) - camPosV2);
                (sLeaser.sprites[self.WaistSprite] as TriangleMesh).MoveVertice(2, Vector2.Lerp(bodypos, (chestpos + hipspos) / 2f, 0.4f) - a * 4f + Custom.PerpendicularVector(-a) * self.scavenger.mainBodyChunk.rad * Mathf.Lerp(0.35f, 0.9f, Mathf.Pow((self.iVars.WaistWidth / 2), 1.3f)) - camPosV2);
                (sLeaser.sprites[self.WaistSprite] as TriangleMesh).MoveVertice(3, Vector2.Lerp(bodypos, (chestpos + hipspos) / 2f, 0.4f) - a * 4f - Custom.PerpendicularVector(-a) * self.scavenger.mainBodyChunk.rad * Mathf.Lerp(0.35f, 0.9f, Mathf.Pow((self.iVars.WaistWidth / 2), 1.3f)) - camPosV2);
                (sLeaser.sprites[self.WaistSprite] as TriangleMesh).MoveVertice(4, Vector2.Lerp(bodypos, (chestpos + hipspos) / 2f, 0.4f) + a * 4f + Custom.PerpendicularVector(-a) * self.scavenger.mainBodyChunk.rad * Mathf.Lerp(0.25f, 0.8f, Mathf.Pow((self.iVars.WaistWidth / 2), 1.3f)) - camPosV2);
                (sLeaser.sprites[self.WaistSprite] as TriangleMesh).MoveVertice(5, Vector2.Lerp(bodypos, (chestpos + hipspos) / 2f, 0.4f) + a * 4f - Custom.PerpendicularVector(-a) * self.scavenger.mainBodyChunk.rad * Mathf.Lerp(0.25f, 0.8f, Mathf.Pow((self.iVars.WaistWidth / 2), 1.3f)) - camPosV2);
                (sLeaser.sprites[self.WaistSprite] as TriangleMesh).MoveVertice(6, chestpos + Custom.PerpendicularVector((bodypos - chestpos).normalized) * self.scavenger.mainBodyChunk.rad * Mathf.Lerp(0.7f, 1.3f, (self.iVars.fatness / 2)) - camPosV2);
                (sLeaser.sprites[self.WaistSprite] as TriangleMesh).MoveVertice(7, chestpos - Custom.PerpendicularVector((bodypos - chestpos).normalized) * self.scavenger.mainBodyChunk.rad * Mathf.Lerp(0.7f, 1.3f, (self.iVars.fatness / 2)) - camPosV2);
                #endregion

                Vector2 vector5 = chestpos + Custom.DirVec(bodypos, chestpos) * 5f;
                Vector2 vector6 = vector5;
                float rad = self.scavenger.mainBodyChunk.rad;
                float dist = Mathf.InverseLerp(0f, 10f, Custom.DistanceToLine(headpos, hipspos, chestpos) * -flipfloat);

                #region neck
                for (int i = 0; i < 4; i++)
                {
                    float num4 = (float)i / 3f;
                    Vector2 vector7 = Vector2.Lerp(vector5, headpos, num4);
                    vector7 += Custom.PerpendicularVector((vector5 - headpos).normalized) * Mathf.Sin(num4 * 3.1415927f) * 3f * dist * flipfloat;
                    Vector2 normalized = (vector6 - vector7).normalized;
                    Vector2 a2 = Custom.PerpendicularVector(normalized);
                    float dist2 = Vector2.Distance(vector7, vector6) / 8f;
                    float num5 = Mathf.Lerp(7f, 3f, num4) - 2f * Mathf.Sin(num4 * 3.1415927f) * Mathf.Lerp(0.5f, 1.5f, (self.iVars.neckThickness / 2));

                    (sLeaser.sprites[self.NeckSprite] as TriangleMesh).MoveVertice(i * 4, vector6 - a2 * (rad + num5) * 0.5f - normalized * dist2 - camPosV2);
                    (sLeaser.sprites[self.NeckSprite] as TriangleMesh).MoveVertice(i * 4 + 1, vector6 + a2 * (rad + num5) * 0.5f - normalized * dist2 - camPosV2);
                    (sLeaser.sprites[self.NeckSprite] as TriangleMesh).MoveVertice(i * 4 + 2, vector7 - a2 * num5 + normalized * dist2 - camPosV2);
                    (sLeaser.sprites[self.NeckSprite] as TriangleMesh).MoveVertice(i * 4 + 3, vector7 + a2 * num5 + normalized * dist2 - camPosV2);
                    vector6 = vector7;
                    rad = num5;
                }
                #endregion

                //head x and y position
                sLeaser.sprites[self.HeadSprite].x = headpos.x - camPosV2.x;
                sLeaser.sprites[self.HeadSprite].y = headpos.y - camPosV2.y;

                #region king mark code, possibly unneeded
                if (self.scavenger.King)
                {
                    sLeaser.sprites[self.TotalSprites - 1].x = headpos.x - camPosV2.x;
                    sLeaser.sprites[self.TotalSprites - 1].y = headpos.y - camPosV2.y + 32f;
                    sLeaser.sprites[self.TotalSprites - 1].alpha = Mathf.Lerp(self.lastMarkAlpha, self.markAlpha, timeStacker);
                    sLeaser.sprites[self.TotalSprites - 2].x = headpos.x - camPosV2.x;
                    sLeaser.sprites[self.TotalSprites - 2].y = headpos.y - camPosV2.y + 32f;
                    sLeaser.sprites[self.TotalSprites - 2].alpha = 0.2f * Mathf.Lerp(self.lastMarkAlpha, self.markAlpha, timeStacker);
                    sLeaser.sprites[self.TotalSprites - 2].scale = 1f + Mathf.Lerp(self.lastMarkAlpha, self.markAlpha, timeStacker);
                }
                #endregion

                Vector2 headdir = self.HeadDir(timeStacker);
                float bodyaxis = self.BodyAxis(timeStacker);
                float lookup = Mathf.Lerp(self.lastLookUp, self.lookUp, timeStacker);
                float neutralface = Mathf.Lerp(self.lastNeutralFace, self.neutralFace, timeStacker);
                lookup *= 1f - neutralface;
                Vector2 normalized2 = Vector2.Lerp(headdir.normalized, -Custom.DegToVec(-bodyaxis), Mathf.Lerp(0.5f, 1f, Mathf.Max(Mathf.Pow(lookup, 1.1f), neutralface))).normalized;
                Vector2 headrot = Custom.RotateAroundOrigo(normalized2.normalized, bodyaxis);
                headdir.Normalize();
                normalized2.Normalize();
                float headsize = Mathf.Lerp(0.6f, 1.1f, self.iVars.headSize);
                sLeaser.sprites[self.HeadSprite].rotation = Custom.VecToDeg(normalized2.normalized);

                #region msc
                if (ModManager.MSC)
                {
                    if (self.maskGfx != null && !self.scavenger.readyToReleaseMask)
                    {
                        float num10 = Custom.VecToDeg(headdir);
                        if (num10 < 30f && num10 > -30f)
                        {
                            self.maskGfx.overrideAnchorVector = new Vector2?(-headdir.normalized);
                            self.maskGfx.overrideDrawVector = new Vector2?(new Vector2(headpos.x, headpos.y + 4f));
                        }
                        else if (num10 <= 90f && num10 >= -90f)
                        {
                            self.maskGfx.overrideAnchorVector = new Vector2?(-normalized2.normalized);
                            self.maskGfx.overrideDrawVector = new Vector2?(new Vector2(headpos.x, headpos.y + 1f));
                        }
                        else
                        {
                            self.maskGfx.overrideAnchorVector = new Vector2?(-normalized2.normalized);
                            self.maskGfx.overrideDrawVector = new Vector2?(headpos);
                        }
                        self.maskGfx.DrawSprites(sLeaser, rCam, timeStacker, camPosV2);
                    }
                    if (self.scavenger.readyToReleaseMask && self.maskGfx != null)
                    {
                        self.maskGfx.SetVisible(sLeaser, false);
                    }
                }
                #endregion

                #region eyes
                Vector2 eyespos = Vector3.Slerp(normalized2.normalized, -headdir.normalized, lookup);
                Vector2 a3 = headpos + eyespos * (4f - Mathf.Max(10f * Mathf.Pow(lookup, 0.5f), 3f * neutralface)) * headsize; //unsure what this is, but it's important it seems
                Vector2 eyesmove = a3 + eyespos * Mathf.Lerp(-5f, 60f, self.iVars.eyesAngle) * Mathf.Lerp(self.lastEyesOpen, self.eyesOpen, timeStacker);
                float boggle = Mathf.Pow(Mathf.Max(0f, Mathf.Lerp(self.lastEyesPop, self.eyesPop, timeStacker)), 0.25f);
                float eyesizerange = Mathf.Clamp(self.iVars.eyeSize + 1 + Mathf.Lerp(0.5f, 0.25f, lookup) * boggle, 0f, 2f); //2f originall 1f

                for (int eyecount = 0; eyecount < 2; eyecount++) //two eyes, a lot of this is changed
                {
                    float eyerot = ((eyecount == 0) ? -1f : 1f) * 0.5f + headrot.x * (1f - neutralface);
                    eyerot = Mathf.Clamp(eyerot, -1f, 1f);
                    Vector2 vector11 = a3 + Custom.PerpendicularVector(eyespos) * 8f * headsize * eyerot;
                    sLeaser.sprites[self.EyeSprite(eyecount, 0)].x = vector11.x - camPosV2.x;
                    sLeaser.sprites[self.EyeSprite(eyecount, 0)].y = vector11.y - camPosV2.y;
                    float num14 = Custom.AimFromOneVectorToAnother(vector11, eyesmove);
                    float num15 = Mathf.Lerp(1.5f, 2f, Mathf.Pow(lookup, 1.5f)) * Mathf.Lerp(0.3f, 1.5f, Mathf.Pow(eyesizerange, 0.7f)) * (1f + 0.2f * boggle * (1f - lookup)) * Mathf.InverseLerp(1f, 0.7f, Mathf.Abs(eyerot)) * Mathf.Lerp(1f, Mathf.Lerp(0.5f, 0.25f, eyesizerange), self.iVars.narrowEyes * Mathf.Lerp(1f, 0.5f, boggle)) * Mathf.Lerp(self.lastEyesOpen, self.eyesOpen, timeStacker);
                    float num16 = Mathf.Lerp(2.5f, 1.5f, Mathf.Pow(lookup, 0.5f)) * Mathf.Lerp(0.3f, 1.5f, Mathf.Pow(eyesizerange, 0.7f)) * (1f + 0.2f * boggle) * Mathf.InverseLerp(0f, 0.75f, Mathf.Lerp(self.lastEyesOpen, self.eyesOpen, timeStacker));

                    //pupils
                    if (self.iVars.pupilSize > 0f)
                    {
                        Vector2 vector12;
                        if (self.iVars.deepPupils)
                        {
                            vector12 = -headdir;
                        }
                        else
                        {
                            vector12 = Custom.DirVec(vector11, self.lookPoint) * Mathf.InverseLerp(0f, 30f, Vector2.Distance(vector11, self.lookPoint)) * Mathf.InverseLerp(0.3f, 0.7f, self.scavenger.abstractCreature.personality.sympathy); //eye tilt?
                        }
                        vector12 = Custom.RotateAroundOrigo(vector12, num14);
                        vector12.x *= num15 * (1f - self.iVars.pupilSize);
                        vector12.y *= num16 * (1f - self.iVars.pupilSize);
                        vector12 = Custom.RotateAroundOrigo(vector12, -num14);
                        sLeaser.sprites[self.EyeSprite(eyecount, 1)].x = vector11.x + vector12.x - camPosV2.x;
                        sLeaser.sprites[self.EyeSprite(eyecount, 1)].y = vector11.y + vector12.y - camPosV2.y;
                        sLeaser.sprites[self.EyeSprite(eyecount, 1)].scaleX = num15 * 0.1f * self.iVars.pupilSize * (1f - 0.5f * boggle);
                        sLeaser.sprites[self.EyeSprite(eyecount, 1)].scaleY = num16 * 0.1f * self.iVars.pupilSize * (1f - 0.5f * boggle);
                        sLeaser.sprites[self.EyeSprite(eyecount, 1)].rotation = num14;
                    }
                    sLeaser.sprites[self.EyeSprite(eyecount, 0)].scaleX = num15 * 0.12f;
                    sLeaser.sprites[self.EyeSprite(eyecount, 0)].scaleY = num16 * 0.07f;
                    sLeaser.sprites[self.EyeSprite(eyecount, 0)].rotation = num14;
                }
                if (self.scavenger.blind >= 10)
                {
                    if (self.scavenger.blind == 10)
                    {
                        self.ApplyPalette(sLeaser, rCam, rCam.currentPalette);
                    }
                    else
                    {
                        for (int k = 0; k < 2; k++)
                        {
                            for (int l = 0; l < ((self.iVars.pupilSize > 0f) ? 2 : 1); l++)
                            {
                                sLeaser.sprites[self.EyeSprite(k, l)].color = new Color(0.3f, 0.3f, 0.3f);
                            }
                        }
                    }
                }
                #endregion

                //head x and y SIZE
                sLeaser.sprites[self.HeadSprite].scaleX = Mathf.Lerp(8f, 9f, Mathf.Pow(lookup, 0.5f)) * headsize / 10f;
                sLeaser.sprites[self.HeadSprite].scaleY = Mathf.Lerp(11f, 8f, Mathf.Pow(lookup, 0.5f)) * headsize / 12f;

                #region teeth
                for (int teethamount = 0; teethamount < self.teeth.GetLength(0); teethamount++)
                {
                    float num17 = (float)teethamount / (float)(self.teeth.GetLength(0) - 1);
                    Vector2 vector13 = a3 + eyespos * 4f + Custom.PerpendicularVector(eyespos) * Mathf.Lerp(-3f, 3f, num17) * headrot.x;
                    Vector2 vector14 = a3 + eyespos * Mathf.Lerp(8f, 10f, Mathf.Sin(num17 * 3.1415927f)) * self.teeth[teethamount, 0] + Custom.PerpendicularVector(eyespos) * Mathf.Lerp(Mathf.Lerp(-9f, 9f, num17) * Mathf.Lerp(0.5f, 1.2f, self.iVars.wideTeeth), -2f * Mathf.Sign(headrot.x), 1f - Mathf.Abs(headrot.y)) * self.teeth[teethamount, 0];
                    Vector2 vector15 = a3 + eyespos * Mathf.Lerp(12f, 15f, Mathf.Sin(num17 * 3.1415927f)) * self.teeth[teethamount, 0] + Custom.PerpendicularVector(eyespos) * Mathf.Lerp(Mathf.Lerp(-9f, 9f, num17) * Mathf.Lerp(0.5f, 1.2f, self.iVars.wideTeeth), -15f * Mathf.Sign(headrot.x), 1f - Mathf.Abs(headrot.y)) * self.teeth[teethamount, 0];
                    (sLeaser.sprites[self.TeethSprite] as TriangleMesh).MoveVertice(teethamount * 5, vector13 - Custom.PerpendicularVector(vector13, vector14) * 1f - camPosV2);
                    (sLeaser.sprites[self.TeethSprite] as TriangleMesh).MoveVertice(teethamount * 5 + 1, vector13 + Custom.PerpendicularVector(vector13, vector14) * 1f - camPosV2);
                    (sLeaser.sprites[self.TeethSprite] as TriangleMesh).MoveVertice(teethamount * 5 + 2, vector14 - Custom.PerpendicularVector(vector14, vector15) * self.teeth[teethamount, 1] - camPosV2);
                    (sLeaser.sprites[self.TeethSprite] as TriangleMesh).MoveVertice(teethamount * 5 + 3, vector14 + Custom.PerpendicularVector(vector14, vector15) * self.teeth[teethamount, 1] - camPosV2);
                    (sLeaser.sprites[self.TeethSprite] as TriangleMesh).MoveVertice(teethamount * 5 + 4, vector15 - camPosV2);
                }
                #endregion

                for (int n = 0; n < self.chestPatchShape.Length; n++)
                {
                    (sLeaser.sprites[self.ChestPatchSprite] as TriangleMesh).MoveVertice(n, self.OnBellySurfacePos(self.chestPatchShape[n], timeStacker) - floamt);
                }
                for (int num18 = 0; num18 < 2; num18++)
                {
                    self.hands[num18].DrawSprites(sLeaser, rCam, timeStacker, floamt);
                }
                for (int num19 = 0; num19 < 2; num19++)
                {
                    self.legs[num19].DrawSprites(sLeaser, rCam, timeStacker, floamt);
                }

                //possibly unneeded?
                #region king armor code
                if (ModManager.MSC)
                {
                    for (int armor = 0; armor < Mathf.Min(self.scavenger.armorPieces, self.shells.Length); armor++)
                    {
                        if (armor == 2)
                        {
                            self.shells[armor].pos = chestpos;
                            self.shells[armor].scaleX = 1.25f;
                            self.shells[armor].scaleY = 1f;
                            self.shells[armor].rotation = 0f;
                            self.shells[armor].zRotation = 90f;
                        }
                        else if (armor == 1)
                        {
                            self.shells[armor].pos = bodypos;
                            self.shells[armor].scaleX = 0.75f;
                            self.shells[armor].scaleY = 0.75f;
                            self.shells[armor].rotation = 0f;
                            self.shells[armor].zRotation = 90f;
                        }
                        else if (armor == 0)
                        {
                            self.shells[armor].pos = hipspos;
                            self.shells[armor].scaleX = 1f;
                            self.shells[armor].scaleY = 0.75f;
                            self.shells[armor].rotation = 0f;
                            self.shells[armor].zRotation = 90f;
                        }
                        self.shells[armor].DrawSprites(sLeaser, rCam, timeStacker, camPosV2);
                    }
                    for (int num21 = Mathf.Min(self.scavenger.armorPieces, self.shells.Length); num21 < self.shells.Length; num21++)
                    {
                        self.shells[num21].visible = false;
                        self.shells[num21].DrawSprites(sLeaser, rCam, timeStacker, camPosV2);
                    }
                }
                #endregion
            }
        }

        private void Tail_ctor(On.ScavengerCosmetic.Tail.orig_ctor orig, ScavengerCosmetic.Tail self, ScavengerGraphics owner, int firstSprite)
        {
            orig(self, owner, firstSprite);
            if (self.scavGrphs.scavenger != null && CustomCreatures.isScrounger(self.scavGrphs.scavenger.abstractCreature))
                self.totalSprites = 2;
        }

        private void Tail_InitiateSprites(On.ScavengerCosmetic.Tail.orig_InitiateSprites orig, ScavengerCosmetic.Tail self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (self.scavGrphs.scavenger != null && ScroungerCWT.TryGetValue(self.scavGrphs.scavenger, out ScroungerInfo scrounger))
            {
                sLeaser.sprites[self.firstSprite + 1] = TriangleMesh.MakeLongMesh(self.scavGrphs.tail.Length, true, true);

                if (scrounger.tailPattern == 1)
                    sLeaser.sprites[self.firstSprite + 1].shader = rCam.game.rainWorld.Shaders["KingTusk"]; //"KingTusk" results in the striped tail, 'lemurtail' scroungers
                else if (scrounger.tailPattern == 2)
                    sLeaser.sprites[self.firstSprite + 1].shader = rCam.game.rainWorld.Shaders["TentaclePlant"]; //"TentaclePlant" results in a dark stripe with a light border in the middle of the tail, 'skunktail' scroungers
                else if (scrounger.tailPattern == 3)
                    sLeaser.sprites[self.firstSprite + 1].shader = rCam.game.rainWorld.Shaders["TubeWorm"]; //"TubeWorm" results in thick fluffy stripes, 'raccoontail' scroungers
                else if (scrounger.tailPattern == 4)
                    sLeaser.sprites[self.firstSprite + 1].shader = rCam.game.rainWorld.Shaders["AquapedeBody"]; //"AquapedeBody" results in thin vertical stripe going up tail, 'linetail' scroungers
            }
        }

        private void Tail_DrawSprites(On.ScavengerCosmetic.Tail.orig_DrawSprites orig, ScavengerCosmetic.Tail self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (self.scavGrphs.scavenger != null && CustomCreatures.isScrounger(self.scavGrphs.scavenger.abstractCreature))
            {
                if (sLeaser.sprites[self.firstSprite + 1] != null && sLeaser.sprites[self.firstSprite + 1] is TriangleMesh)
                {
                    sLeaser.sprites[self.firstSprite + 1].MoveInFrontOfOtherNode(sLeaser.sprites[self.firstSprite]);

                    Vector2 vector = Vector2.Lerp(self.scavGrphs.drawPositions[3, 1], self.scavGrphs.drawPositions[3, 0], timeStacker);
                    float d = 3f;
                    for (int i = 0; i < self.scavGrphs.tail.Length; i++)
                    {
                        Vector2 vector2 = Vector2.Lerp(self.scavGrphs.tail[i].lastPos, self.scavGrphs.tail[i].pos, timeStacker);
                        Vector2 normalized = (vector2 - vector).normalized;
                        Vector2 a = Custom.PerpendicularVector(normalized);
                        float d2 = Vector2.Distance(vector2, vector) / 5f;
                        if (i == 0)
                        {
                            d2 = 0f;
                        }
                        (sLeaser.sprites[self.firstSprite + 1] as TriangleMesh).MoveVertice(i * 4, vector - a * d + normalized * d2 - camPos);
                        (sLeaser.sprites[self.firstSprite + 1] as TriangleMesh).MoveVertice(i * 4 + 1, vector + a * d + normalized * d2 - camPos);
                        if (i < self.scavGrphs.tail.Length - 1)
                        {
                            (sLeaser.sprites[self.firstSprite + 1] as TriangleMesh).MoveVertice(i * 4 + 2, vector2 - a * self.scavGrphs.tail[i].StretchedRad - normalized * d2 - camPos);
                            (sLeaser.sprites[self.firstSprite + 1] as TriangleMesh).MoveVertice(i * 4 + 3, vector2 + a * self.scavGrphs.tail[i].StretchedRad - normalized * d2 - camPos);
                        }
                        else
                        {
                            (sLeaser.sprites[self.firstSprite + 1] as TriangleMesh).MoveVertice(i * 4 + 2, vector2 - camPos);
                        }
                        d = self.scavGrphs.tail[i].StretchedRad;
                        vector = vector2;
                    }
                }

            }
        }

        private void Tail_ApplyPalette(On.ScavengerCosmetic.Tail.orig_ApplyPalette orig, ScavengerCosmetic.Tail self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);
            if (self.scavGrphs.scavenger != null && CustomCreatures.isScrounger(self.scavGrphs.scavenger.abstractCreature))
            {
                ScavengerGraphics scavGraphics = self.scavGrphs.scavenger.graphicsModule as ScavengerGraphics;
                if (sLeaser.sprites[self.firstSprite + 1] != null && sLeaser.sprites[self.firstSprite + 1] is TriangleMesh)
                {
                    for (int i = 0; i < (sLeaser.sprites[self.firstSprite + 1] as TriangleMesh).verticeColors.Length; i++)
                    {
                        float num = Mathf.InverseLerp(0f, (float)((sLeaser.sprites[self.firstSprite + 1] as TriangleMesh).verticeColors.Length - 1), (float)i);
                        (sLeaser.sprites[self.firstSprite + 1] as TriangleMesh).verticeColors[i] = Color.Lerp(Color.Lerp(scavGraphics.BlendedBodyColor, Color.white, Mathf.Pow(num, 2f)), scavGraphics.blackColor, scavGraphics.darkness);
                        (sLeaser.sprites[self.firstSprite + 1] as TriangleMesh).verticeColors[i] = Color.Lerp(Color.Lerp(Color.Lerp(HSLColor.Lerp(scavGraphics.headColor, scavGraphics.bodyColor, num).rgb, scavGraphics.blackColor, 0.65f - 0.4f * num), scavGraphics.BlendedBodyColor, Mathf.Pow(num, 2f)), scavGraphics.blackColor, scavGraphics.darkness);
                    }
                }
            }
        }

        private void IndividualVariations_ctor(On.ScavengerGraphics.IndividualVariations.orig_ctor orig, ref ScavengerGraphics.IndividualVariations self, Scavenger scavenger)
        {
            orig(ref self, scavenger);
            if (scavenger != null && CustomCreatures.isScrounger(scavenger.abstractCreature))
            {
                self.tailSegs = Random.Range(2, 3);
                self.coloredEartlerTips = true;
                self.scruffy = 1f;
                self.armThickness *= 0.7f;
                self.wideTeeth *= 0.1f;
            }
        }

        private void ScavengerGraphics_ctor(On.ScavengerGraphics.orig_ctor orig, ScavengerGraphics self, PhysicalObject ow)
        {
            orig(self, ow);
            if (self.scavenger != null && CustomCreatures.isScrounger(self.scavenger.abstractCreature) && ScroungerCWT.TryGetValue(self.scavenger, out ScroungerInfo scrounger))
            {
                Random.State state = Random.state;
                Random.InitState(self.scavenger.abstractCreature.ID.RandomSeed);

                //stuff that i originally had in individualvariations

                if (Random.value < 0.5f)
                    scrounger.tailThickness = Random.Range(1.8f, 3f);
                else
                    scrounger.tailThickness = 1f;

                if (Random.value < 0.5f)
                    scrounger.tailPattern = Random.Range(2, 5);
                else
                    scrounger.tailPattern = 1;
                //end of that stuff

                self.tail = new TailSegment[self.iVars.tailSegs + 4];
                if (self.iVars.tailSegs > 0)
                {
                    for (int i = 0; i < self.tail.Length; i++)
                    {
                        if (scrounger.tailThickness == 1)
                        {
                            self.tail[i] = new TailSegment(self, Mathf.Lerp(5, 4, i / (self.iVars.tailSegs - 1)), 10f, (i > 0) ? self.tail[i - 1] : null, 0.5f, 0.9f, 0.5f, true);
                        }
                        if (scrounger.tailThickness != 1)
                        {
                            self.tail[i] = new TailSegment(self, Mathf.Lerp(scrounger.tailThickness * 2.2f, scrounger.tailThickness * 1.8f, i / (self.iVars.tailSegs - 1)), 10f, (i > 0) ? self.tail[i - 1] : null, 0.5f, 0.9f, 0.5f, true);
                        }
                    }

                    var bp = self.bodyParts.ToList();
                    bp.RemoveAll(x => x is TailSegment);
                    bp.AddRange(self.tail);
                    self.bodyParts = bp.ToArray();
                }
                Random.state = state;
            }
        }

        private void Eartlers_GenerateSegments(On.ScavengerGraphics.Eartlers.orig_GenerateSegments orig, ScavengerGraphics.Eartlers self)
        {
            orig(self);
            if (self.owner.scavenger != null && CustomCreatures.isScrounger(self.owner.scavenger.abstractCreature))
            {
                //all 1 for normal scav

                float angle = 1.75f; //0 = point inwards, 1 = normal eartlers(straight up). this one is weird
                float2 vector = new float2(35f, 80f); //eartler angles, also weird

                float length = 1.3f; //high = length between 'bends'? doesnt increase fatness. very spindly at higher numbers
                float num2 = 1f; //bottom/first segment fatness
                float num3 = 1f; //middle segment fatness
                float num4 = 1.5f; //tip(colored part) fatness (?)

                //bottom eartlers (on cheeks)
                float botsize = 0.5f; //fatness and length of first segment
                float bottipsize = 0.6f; //tip(colored part) size (length and fatness)



                self.points = new List<ScavengerGraphics.Eartlers.Vertex[]>();
                List<ScavengerGraphics.Eartlers.Vertex> list = new List<ScavengerGraphics.Eartlers.Vertex>();
                list.Clear();
                list.Add(new ScavengerGraphics.Eartlers.Vertex(new float2(0f, 0f), 1f));
                list.Add(new ScavengerGraphics.Eartlers.Vertex(Custom.DegToFloat2(Mathf.Lerp(40f, 90f, UnityEngine.Random.value)) * 0.4f * length, 1f * num2));
                float2 float2 = Custom.DegToFloat2(Mathf.Lerp(vector.x, vector.y, UnityEngine.Random.value) * angle);
                float2 float3 = float2 - Custom.DegToFloat2(Mathf.Lerp(40f, 90f, UnityEngine.Random.value)) * 0.4f * length;
                if (float3.x < 0.2f)
                {
                    float3 = new float2(Mathf.Lerp(float3.x, float2.x, 0.4f), float3.y);
                }
                list.Add(new ScavengerGraphics.Eartlers.Vertex(float3, 1.5f * num3));
                list.Add(new ScavengerGraphics.Eartlers.Vertex(float2, 2f * num4));
                self.DefineBranch(list);
                list.Clear();
                list.Add(new ScavengerGraphics.Eartlers.Vertex(self.points[0][1].pos, 1f));
                int num7 = ((double)Vector2.Distance(self.points[0][1].pos, self.points[0][2].pos) > 0.6 && Random.value < 0.5f) ? 2 : 1;
                float2 float4 = math.lerp(self.points[0][1].pos, self.points[0][2].pos, Mathf.Lerp(0f, (num7 == 1) ? 0.7f : 0.25f, UnityEngine.Random.value));
                list.Add(new ScavengerGraphics.Eartlers.Vertex(float4, 1.2f));
                list.Add(new ScavengerGraphics.Eartlers.Vertex(float4 + self.points[0][3].pos - self.points[0][2].pos + Custom.DegToFloat2(UnityEngine.Random.value * 360f) * 0.1f, 1.75f));
                self.DefineBranch(list);
                if (num7 == 2)
                {
                    list.Clear();
                    float4 = Vector2.Lerp(self.points[0][1].pos, self.points[0][2].pos, Mathf.Lerp(0.45f, 0.7f, Random.value));
                    list.Add(new ScavengerGraphics.Eartlers.Vertex(float4, 1.2f));
                    list.Add(new ScavengerGraphics.Eartlers.Vertex(float4 + self.points[0][3].pos - self.points[0][2].pos + Custom.DegToFloat2(UnityEngine.Random.value * 360f) * 0.1f, 1.75f));
                    self.DefineBranch(list);
                }

                //bottom eartlers
                list.Clear();
                float num9 = 1f + UnityEngine.Random.value * 1.5f;
                bool flag2 = UnityEngine.Random.value < 0.5f;
                list.Add(new ScavengerGraphics.Eartlers.Vertex(new float2(0f, 0f), 1f));
                float num10 = Mathf.Lerp(95f, 135f, UnityEngine.Random.value);
                float num11 = Mathf.Lerp(0.25f, 0.4f, UnityEngine.Random.value) * botsize;
                list.Add(new ScavengerGraphics.Eartlers.Vertex(Custom.DegToFloat2(num10) * num11, (flag2 ? 0.8f : Mathf.Lerp(1f, num9, 0.3f)) * botsize));
                list.Add(new ScavengerGraphics.Eartlers.Vertex(Custom.DegToFloat2(num10 + Mathf.Lerp(5f, 35f, UnityEngine.Random.value)) * Mathf.Max(num11 + 0.1f, Mathf.Lerp(0.3f, 0.6f, UnityEngine.Random.value)), flag2 ? 0.8f : Mathf.Lerp(1f, num9, 0.6f)));

                list.Add(new ScavengerGraphics.Eartlers.Vertex(list[list.Count - 1].pos.normalized() * (list[list.Count - 1].pos.magnitude() + Mathf.Lerp(0.15f, 0.25f, UnityEngine.Random.value) * botsize), num9 * bottipsize));
                self.DefineBranch(list);
            }
        }

        private void ScavengerGraphics_Update(On.ScavengerGraphics.orig_Update orig, ScavengerGraphics self)
        {
            orig(self);
            if (self.scavenger != null && CustomCreatures.isScrounger(self.scavenger.abstractCreature))
            {
                if (self.tail.Length > 0)
                {
                    self.tail[0].connectedPoint = new Vector2?(self.drawPositions[self.hipsDrawPos, 0]);
                    float2 vector = self.drawPositions[self.hipsDrawPos, 0];
                    float2 vector2 = self.drawPositions[self.hipsDrawPos - 1, 0];
                    float num2 = 28f;
                    for (int i = 0; i < self.tail.Length; i++)
                    {
                        self.tail[i].Update();
                        self.tail[i].vel *= Mathf.Lerp(1f, 0.5f, self.owner.bodyChunks[1].submersion);
                        TailSegment tailSegment = self.tail[i];
                        tailSegment.vel.y = tailSegment.vel.y - 0.9f * (1f - self.owner.bodyChunks[1].submersion) * self.owner.room.gravity;
                        if (!Custom.DistLess(self.tail[i].pos, self.owner.bodyChunks[1].pos, 9f * (float)(i + 1)))
                        {
                            self.tail[i].pos = self.owner.bodyChunks[1].pos + Custom.DirVec(self.owner.bodyChunks[1].pos, self.tail[i].pos) * 9f * (float)(i + 1);
                        }
                        self.tail[i].vel += Custom.DirVec(vector2, self.tail[i].pos) * num2 / (Vector2.Distance(vector2, self.tail[i].pos) + 0.1f); //the 0.1f is to make this stop dividing by 0!!
                        num2 *= 0.25f;
                        vector2 = vector;
                        vector = self.tail[i].pos;
                    }
                    if (self.scavenger.dead == false)
                    {
                        for (int i = 3; i > 0; i--)
                        {
                            if (RWCustom.Custom.DistLess(self.tail[self.tail.Length - i].pos, self.tail[self.tail.Length - i - 1].pos, 15f))
                            {
                                Vector2 tailDirection = (self.tail[self.tail.Length - i].pos - self.owner.bodyChunks[0].pos).normalized;
                                float directionMultiplier = Mathf.Sign(tailDirection.x) * Mathf.Sign(self.owner.bodyChunks[0].vel.x);

                                self.tail[self.tail.Length - i].vel.x += directionMultiplier;
                            }
                        }

                        self.tail[self.tail.Length - 4].vel.y += 3f;
                        self.tail[self.tail.Length - 3].vel.y += 2f;
                        self.tail[self.tail.Length - 2].vel.y += 1f;
                        self.tail[self.tail.Length - 1].vel.y += 0.5f;

                        if (self.scavenger.animation != null && self.scavenger.animation.id != null && (self.scavenger.animation.id == Scavenger.ScavengerAnimation.ID.ThrowCharge || self.scavenger.animation.id == MoreSlugcatsEnums.ScavengerAnimationID.PrepareToJump))
                        {
                            float shake = 3f;

                            for (int i = 0; i < self.tail.Length; i++)
                            {
                                Vector2 tailShake = new Vector2(Random.Range(-shake, shake), Random.Range(-shake, shake));
                                self.tail[i].pos += tailShake;
                            }
                        }
                    }
                }
            }
        }

        private void ScavengerGraphics_Reset(On.ScavengerGraphics.orig_Reset orig, ScavengerGraphics self)
        {
            orig(self);
            if (self.scavenger != null && CustomCreatures.isScrounger(self.scavenger.abstractCreature))
            {
                if (self.tail.Length > 0)
                {
                    for (int i = 0; i < self.tail.Length; i++)
                    {
                        self.tail[i].Reset(self.owner.bodyChunks[1].pos);
                    }
                }
            }
        }

        private void ScavengerGraphics_GenerateColors(On.ScavengerGraphics.orig_GenerateColors orig, ScavengerGraphics self)
        {
            orig(self);
            if (self.scavenger != null && CustomCreatures.isScrounger(self.scavenger.abstractCreature))
            {
                float num = Random.value * 0.1f;
                if (Random.value < 0.025f)
                {
                    num = Mathf.Pow(Random.value, 0.4f);
                }
                if (self.scavenger.Elite)
                {
                    num = Mathf.Pow(Random.value, 5f);
                }
                float num2 = num + Mathf.Lerp(-1f, 1f, Random.value) * 0.3f * Mathf.Pow(Random.value, 2f);
                if (num2 > 1f)
                {
                    num2 -= 1f;
                }
                else if (num2 < 0f)
                {
                    num2 += 1f;
                }

                if (Random.value > 0.9f)
                {
                    self.bodyColor.lightness = Mathf.Lerp(self.bodyColor.lightness, 0.5f + 0.5f * Mathf.Pow(Random.value, 0.8f), 1f - self.iVars.generalMelanin);
                    self.bodyColorBlack = Custom.LerpMap((self.bodyColor.rgb.r + self.bodyColor.rgb.g + self.bodyColor.rgb.b) / 3f, 0.04f, 0.8f, 0.3f, 0.95f, 0.5f);
                    self.bodyColorBlack = Mathf.Lerp(self.bodyColorBlack, Mathf.Lerp(0.5f, 1f, Random.value), Random.value * Random.value * Random.value);
                    self.bodyColorBlack *= self.iVars.generalMelanin;
                }
                else
                {
                    self.bodyColor = new HSLColor(num, Mathf.Lerp(0.05f, 1f, Mathf.Pow(Random.value, 0.85f)), Mathf.Lerp(0.05f, 0.8f, Random.value));
                    self.bodyColor.saturation = self.bodyColor.saturation * (1f - self.iVars.generalMelanin);
                    self.bodyColor.lightness = Mathf.Lerp(self.bodyColor.lightness, 0.5f + 0.5f * Mathf.Pow(Random.value, 0.6f), 1f - self.iVars.generalMelanin);
                    self.bodyColorBlack = Custom.LerpMap((self.bodyColor.rgb.r + self.bodyColor.rgb.g + self.bodyColor.rgb.b) + self.bodyColor.lightness * 3f, 0.84f, 0.2f, 0.7f, 0.15f, 2f);
                    self.bodyColorBlack = Mathf.Lerp(self.bodyColorBlack, Mathf.Lerp(1f, 0.5f, Random.value), Random.value * Random.value * Random.value);
                    self.bodyColorBlack /= self.iVars.generalMelanin;
                }

                //normal scav colors
                // this.bodyColor.lightness = Mathf.Lerp(this.bodyColor.lightness, 0.5f + 0.5f * Mathf.Pow(Random.value, 0.8f), 1f - this.iVars.generalMelanin);
                // this.bodyColorBlack = Custom.LerpMap((this.bodyColor.rgb.r + this.bodyColor.rgb.g + this.bodyColor.rgb.b) / 3f, 0.04f, 0.8f, 0.3f, 0.95f, 0.5f);
                // this.bodyColorBlack = Mathf.Lerp(this.bodyColorBlack, Mathf.Lerp(0.5f, 1f, Random.value), Random.value * Random.value * Random.value);
                // this.bodyColorBlack *= this.iVars.generalMelanin;

                Vector2 vector = new Vector2(self.bodyColor.saturation, Mathf.Lerp(-1f, 1f, self.bodyColor.lightness * (1f + self.bodyColorBlack)));
                if (vector.magnitude < 0.5f)
                {
                    vector = Vector2.Lerp(vector, vector.normalized, Mathf.InverseLerp(0.5f, 0.3f, vector.magnitude));
                    self.bodyColor = new HSLColor(self.bodyColor.hue, Mathf.InverseLerp(-1f, 1f, vector.x), Mathf.InverseLerp(-1f, 1f, vector.y));
                    self.bodyColorBlack = Custom.LerpMap((self.bodyColor.rgb.r + self.bodyColor.rgb.g + self.bodyColor.rgb.b) / 3f, 0.04f, 0.8f, 0.3f, 0.95f, 0.5f);
                    self.bodyColorBlack = Mathf.Lerp(self.bodyColorBlack, Mathf.Lerp(0.5f, 1f, Random.value), Random.value * Random.value * Random.value);
                    self.bodyColorBlack *= self.iVars.generalMelanin;
                }
                if (self.bodyColorBlack < self.bodyColor.lightness)
                {
                    self.bodyColorBlack += self.bodyColor.lightness / 2f;
                }
                if (self.headColor.saturation < self.bodyColor.saturation)
                {
                    self.headColor.saturation += self.bodyColor.saturation / 2f;
                }
                float num3;
                if (Random.value < Custom.LerpMap(self.bodyColorBlack, 0.5f, 0.8f, 0.9f, 0.3f))
                {
                    num3 = num2 + Mathf.Lerp(-1f, 1f, Random.value) * 0.1f * Mathf.Pow(Random.value, 1.5f);
                    num3 = Mathf.Lerp(num3, 0.15f, Random.value);
                    if (num3 > 1f)
                    {
                        num3 -= 1f;
                    }
                    else if (num3 < 0f)
                    {
                        num3 += 1f;
                    }
                }
                else
                {
                    num3 = ((Random.value < 0.5f) ? Custom.Decimal(num + 0.5f) : Custom.Decimal(num2 + 0.5f)) + Mathf.Lerp(-1f, 1f, Random.value) * 0.25f * Mathf.Pow(Random.value, 2f);
                    if (Random.value < Mathf.Lerp(0.8f, 0.2f, self.scavenger.abstractCreature.personality.energy))
                    {
                        num3 = Mathf.Lerp(num3, 0.85f, Random.value);
                    }
                    if (num3 > 1f)
                    {
                        num3 -= 1f;
                    }
                    else if (num3 < 0f)
                    {
                        num3 += 1f;
                    }
                }
                self.headColor = new HSLColor((Random.value < 0.75f) ? num2 : num3, 1f, 0.05f + 0.15f * Random.value);
                self.headColor.saturation = self.headColor.saturation * Mathf.Pow(1f - self.iVars.generalMelanin, 2f);
                self.headColor.lightness = Mathf.Lerp(self.headColor.lightness, 0.5f + 0.5f * Mathf.Pow(Random.value, 0.8f), 1f - self.iVars.generalMelanin);
                self.headColor.saturation = self.headColor.saturation * (0.1f + 0.9f * Mathf.InverseLerp(0.1f, 0f, Custom.DistanceBetweenZeroToOneFloats(self.bodyColor.hue, self.headColor.hue) * Custom.LerpMap(Mathf.Abs(0.5f - self.headColor.lightness), 0f, 0.5f, 1f, 0.3f)));
                if (self.headColor.lightness < 0.5f)
                {
                    self.headColor.lightness = self.headColor.lightness * (0.5f + 0.5f * Mathf.InverseLerp(0.9f, 0.55f, Custom.DistanceBetweenZeroToOneFloats(self.bodyColor.hue, self.headColor.hue)));
                }
                self.headColorBlack = Custom.LerpMap((self.headColor.rgb.r + self.headColor.rgb.g + self.headColor.rgb.b) + self.headColor.lightness / 3f, 0.035f, 0.26f, 0.7f, 0.95f, 0.25f);
                self.headColorBlack = Mathf.Lerp(self.headColorBlack, Mathf.Lerp(0.8f, 1f, Random.value), Random.value * Random.value * Random.value);
                self.headColorBlack *= 0.2f + 0.7f * self.iVars.generalMelanin;
                self.headColorBlack = Mathf.Max(self.headColorBlack, self.bodyColorBlack);
                self.headColor.saturation = Custom.LerpMap(self.headColor.lightness * (1f - self.headColorBlack), 0f, 0.15f, 1f, self.headColor.saturation);
                if (self.headColor.lightness < self.bodyColor.lightness)
                {
                    self.headColor = self.bodyColor;
                }
                if (self.headColorBlack > self.bodyColorBlack)
                {
                    self.headColorBlack *= 0.8f;
                    self.bodyColorBlack *= 1.1f;
                }
                if (self.headColor.saturation < self.bodyColor.saturation * 0.75f)
                {
                    if (Random.value < 0.5f)
                    {
                        self.headColor.hue = self.bodyColor.hue;
                    }
                    else
                    {
                        self.headColor.lightness = self.headColor.lightness * 0.25f;
                    }
                    self.headColor.saturation = self.bodyColor.saturation * 0.75f;
                }
                self.decorationColor = new HSLColor((Random.value < 0.65f) ? num : ((Random.value < 0.5f) ? num2 : num3), Random.value, 0.5f + 0.5f * Mathf.Pow(Random.value, 0.5f));
                self.decorationColor.lightness = self.decorationColor.lightness * Mathf.Lerp(self.iVars.generalMelanin, Random.value, 0.5f);
                self.eyeColor = new HSLColor(self.scavenger.Elite ? 0f : num3, 1f, (Random.value < 0.2f) ? (0.5f + Random.value * 0.5f) : 0.5f);
                if (self.iVars.coloredPupils > 0)
                {
                    self.eyeColor.lightness = Mathf.Lerp(self.eyeColor.lightness, 1f, 1f);
                }
                if (self.headColor.lightness * (1f - self.headColorBlack) > self.eyeColor.lightness / 2f && (self.iVars.pupilSize == 0f || self.iVars.deepPupils))
                {
                    self.eyeColor.lightness = self.eyeColor.lightness * 0.2f;
                }
                float value = Random.value;
                float value2 = Random.value;
                self.bellyColor = new HSLColor(Mathf.Lerp(self.bodyColor.hue, self.decorationColor.hue, value * 0.7f), self.bodyColor.saturation * Mathf.Lerp(1f, 0.5f, value), self.bodyColor.lightness + 0.05f + 0.3f * value2);
                self.bellyColorBlack = Mathf.Lerp(self.bodyColorBlack, 1f, 0.3f * Mathf.Pow(value2, 1.4f));
                if (Random.value < 0.033333335f)
                {
                    self.headColor.lightness = Mathf.Lerp(0.2f, 1f, Random.value);
                    self.headColorBlack *= Mathf.Lerp(1f, 0.8f, Random.value);
                    self.bellyColor.hue = Mathf.Lerp(self.bellyColor.hue, self.headColor.hue, Mathf.Pow(Random.value, 0.5f));
                }
            }
        }
        #endregion
    }
}