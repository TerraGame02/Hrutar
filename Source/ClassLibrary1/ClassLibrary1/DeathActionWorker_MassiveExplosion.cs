using System;
using System.Collections.Generic;
using UnityEngine;
using RimWorld;
using Verse;

namespace Gjallarhorn
{
	public class DeathActionWorker_MassiveExplosion : DeathActionWorker
	{
		public override RulePackDef DeathRules
		{
			get
			{
				return RulePackDefOf.Transition_DiedExplosive;
			}
		}
		public override bool DangerousInMelee
		{
			get
			{
				return true;
			}
		}
		public override void PawnDied(Corpse corpse)
		{
			float radius;
			if (corpse.InnerPawn.ageTracker.CurLifeStageIndex == 0)
			{
				radius = 10f;
			}
			else if (corpse.InnerPawn.ageTracker.CurLifeStageIndex == 1)
			{
				radius = 10f;
			}
			else
			{
				radius = 10f;
			}
			GenExplosion.DoExplosion(corpse.Position, corpse.Map, radius, DamageDefOf.Bomb, corpse.InnerPawn, 50, 1f, null, null, null, null, null, 0f, 1, false, null, 0f, 1, 1f, false, null, null);
		}
	}
}
