using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using RimWorld.Planet;
using RimWorld;
using Verse;

namespace Gjallarhorn
{
	// Token: 0x02000EEB RID: 3819
	public class FactionManager : IExposable
	{
		// Token: 0x17000FFC RID: 4092
		// (get) Token: 0x06005AC4 RID: 23236 RVA: 0x001F2493 File Offset: 0x001F0693
		public List<Faction> AllFactionsListForReading
		{
			get
			{
				return this.allFactions;
			}
		}

		// Token: 0x17000FFD RID: 4093
		// (get) Token: 0x06005AC5 RID: 23237 RVA: 0x001F2493 File Offset: 0x001F0693
		public IEnumerable<Faction> AllFactions
		{
			get
			{
				return this.allFactions;
			}
		}

		// Token: 0x17000FFE RID: 4094
		// (get) Token: 0x06005AC6 RID: 23238 RVA: 0x001F249B File Offset: 0x001F069B
		public IEnumerable<Faction> AllFactionsVisible
		{
			get
			{
				return from fa in this.allFactions
					   where !fa.Hidden
					   select fa;
			}
		}

		// Token: 0x17000FFF RID: 4095
		// (get) Token: 0x06005AC7 RID: 23239 RVA: 0x001F24C7 File Offset: 0x001F06C7
		public IEnumerable<Faction> AllFactionsVisibleInViewOrder
		{
			get
			{
				return FactionManager.GetInViewOrder(this.AllFactionsVisible);
			}
		}

		// Token: 0x17001000 RID: 4096
		// (get) Token: 0x06005AC8 RID: 23240 RVA: 0x001F24D4 File Offset: 0x001F06D4
		public IEnumerable<Faction> AllFactionsInViewOrder
		{
			get
			{
				return FactionManager.GetInViewOrder(this.AllFactions);
			}
		}

		// Token: 0x17001001 RID: 4097
		// (get) Token: 0x06005AC9 RID: 23241 RVA: 0x001F24E1 File Offset: 0x001F06E1
		public Faction OfPlayer
		{
			get
			{
				return this.ofPlayer;
			}
		}

		// Token: 0x17001002 RID: 4098
		// (get) Token: 0x06005ACA RID: 23242 RVA: 0x001F24E9 File Offset: 0x001F06E9
		public Faction OfMechanoids
		{
			get
			{
				return this.ofMechanoids;
			}
		}

		// Token: 0x17001003 RID: 4099
		// (get) Token: 0x06005ACB RID: 23243 RVA: 0x001F24F1 File Offset: 0x001F06F1
		public Faction OfInsects
		{
			get
			{
				return this.ofInsects;
			}
		}

		// Token: 0x17001004 RID: 4100
		// (get) Token: 0x06005ACC RID: 23244 RVA: 0x001F24F9 File Offset: 0x001F06F9
		public Faction OfAncients
		{
			get
			{
				return this.ofAncients;
			}
		}

		// Token: 0x17001005 RID: 4101
		// (get) Token: 0x06005ACD RID: 23245 RVA: 0x001F2501 File Offset: 0x001F0701
		public Faction OfAncientsHostile
		{
			get
			{
				return this.ofAncientsHostile;
			}
		}

		// Token: 0x17001006 RID: 4102
		// (get) Token: 0x06005ACE RID: 23246 RVA: 0x001F2509 File Offset: 0x001F0709
		public Faction OfEmpire
		{
			get
			{
				return this.empire;
			}
		}

		// Token: 0x17001007 RID: 4103
		// (get) Token: 0x06005ACF RID: 23247 RVA: 0x001F2511 File Offset: 0x001F0711
		public Faction OfPirates
		{
			get
			{
				return this.ofPirates;
			}
		}

		public Faction OfVoidElemental
		{
			get
			{
				return this.OfVoidElemental;
			}
		}

		// Token: 0x06005AD0 RID: 23248 RVA: 0x001F251C File Offset: 0x001F071C
		public void ExposeData()
		{
			Scribe_Collections.Look<Faction>(ref this.allFactions, "allFactions", LookMode.Deep, Array.Empty<object>());
			Scribe_Collections.Look<Faction>(ref this.toRemove, "toRemove", LookMode.Reference, Array.Empty<object>());
			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				BackCompatibility.FactionManagerPostLoadInit();
				if (this.toRemove == null)
				{
					this.toRemove = new List<Faction>();
				}
			}
			if (Scribe.mode == LoadSaveMode.LoadingVars || Scribe.mode == LoadSaveMode.ResolvingCrossRefs || Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				if (this.allFactions.RemoveAll((Faction x) => x == null || x.def == null) != 0)
				{
					Log.Error("Some factions were null after loading.");
				}
				this.RecacheFactions();
			}
		}

		// Token: 0x06005AD1 RID: 23249 RVA: 0x001F25C9 File Offset: 0x001F07C9
		public void Add(Faction faction)
		{
			if (this.allFactions.Contains(faction))
			{
				return;
			}
			this.allFactions.Add(faction);
			this.RecacheFactions();
		}

		// Token: 0x06005AD2 RID: 23250 RVA: 0x001F25EC File Offset: 0x001F07EC
		private void Remove(Faction faction)
		{
			if (!faction.temporary)
			{
				Log.Error("Attempting to remove " + faction.Name + " which is not a temporary faction, only temporary factions can be removed");
				return;
			}
			if (!this.allFactions.Contains(faction))
			{
				return;
			}
			faction.RemoveAllRelations();
			this.allFactions.Remove(faction);
			List<Pawn> allMapsWorldAndTemporary_AliveOrDead = PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead;
			for (int i = 0; i < allMapsWorldAndTemporary_AliveOrDead.Count; i++)
			{
				if (allMapsWorldAndTemporary_AliveOrDead[i].Faction == faction)
				{
					allMapsWorldAndTemporary_AliveOrDead[i].SetFaction(null, null);
				}
			}
			for (int j = 0; j < Find.Maps.Count; j++)
			{
				Find.Maps[j].pawnDestinationReservationManager.Notify_FactionRemoved(faction);
			}
			Find.LetterStack.Notify_FactionRemoved(faction);
			Find.PlayLog.Notify_FactionRemoved(faction);
			this.RecacheFactions();
			Find.QuestManager.Notify_FactionRemoved(faction);
			Find.IdeoManager.Notify_FactionRemoved(faction);
			Find.TaleManager.Notify_FactionRemoved(faction);
		}

		// Token: 0x06005AD3 RID: 23251 RVA: 0x001F26DC File Offset: 0x001F08DC
		public void FactionManagerTick()
		{
			this.goodwillSituationManager.GoodwillManagerTick();
			SettlementProximityGoodwillUtility.CheckSettlementProximityGoodwillChange();
			for (int i = 0; i < this.allFactions.Count; i++)
			{
				this.allFactions[i].FactionTick();
			}
			for (int j = this.toRemove.Count - 1; j >= 0; j--)
			{
				Faction faction = this.toRemove[j];
				this.toRemove.Remove(faction);
				this.Remove(faction);
			}
		}

		// Token: 0x06005AD4 RID: 23252 RVA: 0x001F275C File Offset: 0x001F095C
		public Faction FirstFactionOfDef(FactionDef facDef)
		{
			for (int i = 0; i < this.allFactions.Count; i++)
			{
				if (this.allFactions[i].def == facDef)
				{
					return this.allFactions[i];
				}
			}
			return null;
		}

		// Token: 0x06005AD5 RID: 23253 RVA: 0x001F27A4 File Offset: 0x001F09A4
		public bool TryGetRandomNonColonyHumanlikeFaction(out Faction faction, bool tryMedievalOrBetter, bool allowDefeated = false, TechLevel minTechLevel = TechLevel.Undefined, bool allowTemporary = false)
		{
			return (from x in this.AllFactions
					where !x.IsPlayer && !x.Hidden && x.def.humanlikeFaction && (allowDefeated || !x.defeated) && (allowTemporary || !x.temporary) && (minTechLevel == TechLevel.Undefined || x.def.techLevel >= minTechLevel)
					select x).TryRandomElementByWeight(delegate (Faction x)
					{
						if (tryMedievalOrBetter && x.def.techLevel < TechLevel.Medieval)
						{
							return 0.1f;
						}
						return 1f;
					}, out faction);
		}

		// Token: 0x06005AD6 RID: 23254 RVA: 0x001F27FE File Offset: 0x001F09FE
		public IEnumerable<Faction> GetFactions(bool allowHidden = false, bool allowDefeated = false, bool allowNonHumanlike = true, TechLevel minTechLevel = TechLevel.Undefined, bool allowTemporary = false)
		{
			int num;
			for (int i = 0; i < this.allFactions.Count; i = num + 1)
			{
				Faction faction = this.allFactions[i];
				if (!faction.IsPlayer && (allowHidden || !faction.Hidden) && (allowTemporary || !faction.temporary) && (allowDefeated || !faction.defeated) && (allowNonHumanlike || faction.def.humanlikeFaction) && (minTechLevel == TechLevel.Undefined || faction.def.techLevel >= minTechLevel))
				{
					yield return faction;
				}
				num = i;
			}
			yield break;
		}

		// Token: 0x06005AD7 RID: 23255 RVA: 0x001F2834 File Offset: 0x001F0A34
		public Faction RandomEnemyFaction(bool allowHidden = false, bool allowDefeated = false, bool allowNonHumanlike = true, TechLevel minTechLevel = TechLevel.Undefined)
		{
			Faction result;
			if ((from x in this.GetFactions(allowHidden, allowDefeated, allowNonHumanlike, minTechLevel, false)
				 where x.HostileTo(Faction.OfPlayer)
				 select x).TryRandomElement(out result))
			{
				return result;
			}
			return null;
		}

		// Token: 0x06005AD8 RID: 23256 RVA: 0x001F2880 File Offset: 0x001F0A80
		public Faction RandomNonHostileFaction(bool allowHidden = false, bool allowDefeated = false, bool allowNonHumanlike = true, TechLevel minTechLevel = TechLevel.Undefined)
		{
			Faction result;
			if ((from x in this.GetFactions(allowHidden, allowDefeated, allowNonHumanlike, minTechLevel, false)
				 where !x.HostileTo(Faction.OfPlayer)
				 select x).TryRandomElement(out result))
			{
				return result;
			}
			return null;
		}

		// Token: 0x06005AD9 RID: 23257 RVA: 0x001F28CC File Offset: 0x001F0ACC
		public Faction RandomAlliedFaction(bool allowHidden = false, bool allowDefeated = false, bool allowNonHumanlike = true, TechLevel minTechLevel = TechLevel.Undefined)
		{
			Faction result;
			if ((from x in this.GetFactions(allowHidden, allowDefeated, allowNonHumanlike, minTechLevel, false)
				 where x.PlayerRelationKind == FactionRelationKind.Ally
				 select x).TryRandomElement(out result))
			{
				return result;
			}
			return null;
		}

		// Token: 0x06005ADA RID: 23258 RVA: 0x001F2918 File Offset: 0x001F0B18
		public Faction RandomRoyalFaction(bool allowHidden = false, bool allowDefeated = false, bool allowNonHumanlike = true, TechLevel minTechLevel = TechLevel.Undefined)
		{
			Faction result;
			if ((from x in this.GetFactions(allowHidden, allowDefeated, allowNonHumanlike, minTechLevel, false)
				 where x.def.HasRoyalTitles
				 select x).TryRandomElement(out result))
			{
				return result;
			}
			return null;
		}

		// Token: 0x06005ADB RID: 23259 RVA: 0x001F2964 File Offset: 0x001F0B64
		public void LogKidnappedPawns()
		{
			Log.Message("Kidnapped pawns:");
			for (int i = 0; i < this.allFactions.Count; i++)
			{
				this.allFactions[i].kidnapped.LogKidnappedPawns();
			}
		}

		// Token: 0x06005ADC RID: 23260 RVA: 0x001F29A8 File Offset: 0x001F0BA8
		public void LogAllFactions()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (Faction faction in this.allFactions)
			{
				stringBuilder.AppendLine(string.Format("name: {0}, temporary: {1}, can be deleted?: {2}", faction.Name, faction.temporary, this.FactionCanBeRemoved(faction)));
			}
			stringBuilder.AppendLine(string.Format("{0} factions found.", this.allFactions.Count));
			Log.Message(stringBuilder.ToString());
		}

		// Token: 0x06005ADD RID: 23261 RVA: 0x001F2A54 File Offset: 0x001F0C54
		public void LogFactionsToRemove()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (Faction faction in this.toRemove)
			{
				stringBuilder.AppendLine(string.Format("name: {0}, temporary: {1}, can be deleted?: {2}", faction.Name, faction.temporary, this.FactionCanBeRemoved(faction)));
			}
			stringBuilder.AppendLine(string.Format("{0} factions found.", this.toRemove.Count));
			Log.Message(stringBuilder.ToString());
		}

		// Token: 0x06005ADE RID: 23262 RVA: 0x001F2B00 File Offset: 0x001F0D00
		public void LogFactionsOnPawns()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (IGrouping<Faction, Pawn> grouping in from p in Find.WorldPawns.AllPawnsAliveOrDead
														  group p by p.Faction)
			{
				if (grouping.Key == null)
				{
					stringBuilder.AppendLine(string.Format("no faction: {0} pawns found.", grouping.Count<Pawn>()));
				}
				else
				{
					stringBuilder.AppendLine(string.Format("{0}: {1} pawns found.", grouping.Key, grouping.Count<Pawn>()));
				}
			}
			Log.Message(stringBuilder.ToString());
		}

		// Token: 0x06005ADF RID: 23263 RVA: 0x001F2BC8 File Offset: 0x001F0DC8
		public static IEnumerable<Faction> GetInViewOrder(IEnumerable<Faction> factions)
		{
			return from x in factions
				   orderby x.defeated, x.def.listOrderPriority descending
				   select x;
		}

		// Token: 0x06005AE0 RID: 23264 RVA: 0x001F2C20 File Offset: 0x001F0E20
		private void RecacheFactions()
		{
			this.ofPlayer = null;
			for (int i = 0; i < this.allFactions.Count; i++)
			{
				if (this.allFactions[i].IsPlayer)
				{
					this.ofPlayer = this.allFactions[i];
					break;
				}
			}
			this.ofMechanoids = this.FirstFactionOfDef(FactionDefOf.Mechanoid);
			this.ofInsects = this.FirstFactionOfDef(FactionDefOf.Insect);
			this.ofAncients = this.FirstFactionOfDef(FactionDefOf.Ancients);
			this.ofAncientsHostile = this.FirstFactionOfDef(FactionDefOf.AncientsHostile);
			this.empire = this.FirstFactionOfDef(FactionDefOf.Empire);
			this.ofPirates = this.FirstFactionOfDef(FactionDefOf.Pirate);
			this.ofVoidElemental = this.FirstFactionOfDef(FactionDefOf.VoidElemental);
		}

		// Token: 0x06005AE1 RID: 23265 RVA: 0x001F2CD8 File Offset: 0x001F0ED8
		public void Notify_QuestCleanedUp(Quest quest)
		{
			for (int i = this.allFactions.Count - 1; i >= 0; i--)
			{
				Faction faction = this.allFactions[i];
				if (this.FactionCanBeRemoved(faction))
				{
					this.QueueForRemoval(faction);
				}
			}
		}

		// Token: 0x06005AE2 RID: 23266 RVA: 0x001F2D1A File Offset: 0x001F0F1A
		public void Notify_PawnKilled(Pawn pawn)
		{
			this.TryQueuePawnFactionForRemoval(pawn);
		}

		// Token: 0x06005AE3 RID: 23267 RVA: 0x001F2D1A File Offset: 0x001F0F1A
		public void Notify_PawnLeftMap(Pawn pawn)
		{
			this.TryQueuePawnFactionForRemoval(pawn);
		}

		// Token: 0x06005AE4 RID: 23268 RVA: 0x001F2D23 File Offset: 0x001F0F23
		public void Notify_PawnRecruited(Faction oldFaction)
		{
			if (this.FactionCanBeRemoved(oldFaction))
			{
				this.QueueForRemoval(oldFaction);
			}
		}

		// Token: 0x06005AE5 RID: 23269 RVA: 0x001F2D35 File Offset: 0x001F0F35
		public void Notify_WorldObjectDestroyed(WorldObject worldObject)
		{
			if (worldObject.Faction == null)
			{
				return;
			}
			if (this.FactionCanBeRemoved(worldObject.Faction))
			{
				this.QueueForRemoval(worldObject.Faction);
			}
		}

		// Token: 0x06005AE6 RID: 23270 RVA: 0x001F2D5C File Offset: 0x001F0F5C
		private void TryQueuePawnFactionForRemoval(Pawn pawn)
		{
			if (pawn.Faction != null && this.FactionCanBeRemoved(pawn.Faction))
			{
				this.QueueForRemoval(pawn.Faction);
			}
			Faction extraHomeFaction = pawn.GetExtraHomeFaction(null);
			if (extraHomeFaction != null && this.FactionCanBeRemoved(extraHomeFaction))
			{
				this.QueueForRemoval(extraHomeFaction);
			}
			Faction extraMiniFaction = pawn.GetExtraMiniFaction(null);
			if (extraMiniFaction != null && this.FactionCanBeRemoved(extraMiniFaction))
			{
				this.QueueForRemoval(extraMiniFaction);
			}
			if (pawn.SlaveFaction != null && this.FactionCanBeRemoved(pawn.SlaveFaction))
			{
				this.QueueForRemoval(pawn.SlaveFaction);
			}
		}

		// Token: 0x06005AE7 RID: 23271 RVA: 0x001F2DE3 File Offset: 0x001F0FE3
		private void QueueForRemoval(Faction faction)
		{
			if (!faction.temporary)
			{
				Log.Error("Cannot queue faction " + faction.Name + " for removal, only temporary factions can be removed");
				return;
			}
			if (!this.toRemove.Contains(faction))
			{
				this.toRemove.Add(faction);
			}
		}

		// Token: 0x06005AE8 RID: 23272 RVA: 0x001F2E24 File Offset: 0x001F1024
		private bool FactionCanBeRemoved(Faction faction)
		{
			FactionManager.<> c__DisplayClass58_0 CS$<> 8__locals1;
			CS$<> 8__locals1.faction = faction;
			if (!CS$<> 8__locals1.faction.temporary || this.toRemove.Contains(CS$<> 8__locals1.faction) || Find.QuestManager.IsReservedByAnyQuest(CS$<> 8__locals1.faction))
			{
				return false;
			}
			if (!FactionManager.< FactionCanBeRemoved > g__CheckPawns | 58_0(PawnsFinder.AllMaps_Spawned, ref CS$<> 8__locals1))
			{
				return false;
			}
			if (!FactionManager.< FactionCanBeRemoved > g__CheckPawns | 58_0(PawnsFinder.AllCaravansAndTravelingTransportPods_Alive, ref CS$<> 8__locals1))
			{
				return false;
			}
			using (List<WorldObject>.Enumerator enumerator = Find.WorldObjects.AllWorldObjects.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.Faction == CS$<> 8__locals1.faction)
					{
						return false;
					}
				}
			}
			return true;
		}

		// Token: 0x06005AEA RID: 23274 RVA: 0x001F2F10 File Offset: 0x001F1110
		[CompilerGenerated]
		internal static bool <FactionCanBeRemoved>g__CheckPawns|58_0(List<Pawn> pawns, ref FactionManager.<>c__DisplayClass58_0 A_1)
		{
			for (int i = 0; i<pawns.Count; i++)
			{
				Pawn pawn = pawns[i];
				if (!pawn.Dead && ((pawn.Faction != null && pawn.Faction == A_1.faction) || A_1.faction == pawn.GetExtraHomeFaction(null) || A_1.faction == pawn.GetExtraMiniFaction(null) || A_1.faction == pawn.SlaveFaction))
				{
					return false;
				}
			}
			return true;
		}

// Token: 0x04003554 RID: 13652
private List<Faction> allFactions = new List<Faction>();

// Token: 0x04003555 RID: 13653
private List<Faction> toRemove = new List<Faction>();

// Token: 0x04003556 RID: 13654
public GoodwillSituationManager goodwillSituationManager = new GoodwillSituationManager();

// Token: 0x04003557 RID: 13655
private Faction ofPlayer;

// Token: 0x04003558 RID: 13656
private Faction ofMechanoids;

// Token: 0x04003559 RID: 13657
private Faction ofInsects;

// Token: 0x0400355A RID: 13658
private Faction ofAncients;

// Token: 0x0400355B RID: 13659
private Faction ofAncientsHostile;

// Token: 0x0400355C RID: 13660
private Faction empire;

// Token: 0x0400355D RID: 13661
private Faction ofPirates;

private Faction ofVoidElemental;
	}
}