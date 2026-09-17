using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ElementalHexTactics3D.Grid;
using ElementalHexTactics3D.Units;
using ElementalHexTactics3D.Combat;
using ElementalHexTactics3D.CameraControl;

namespace ElementalHexTactics3D.Turn
{
    public enum TurnPhase
    {
        PlayerTurn,
        EnemyTurn
    }

    public enum BattleResult
    {
        InProgress,
        Victory,
        Defeat
    }

    /// <summary>
    /// Manages the turn cycle between Player and Enemy, evaluates Win/Loss battle conditions,
    /// and resets player unit action economy every round.
    /// </summary>
    public class TurnManager3D : MonoBehaviour
    {
        public static TurnManager3D Instance { get; private set; }

        [Header("Turn State")]
        [SerializeField] private TurnPhase currentPhase = TurnPhase.PlayerTurn;
        [SerializeField] private int currentRound = 1;
        [SerializeField] private BattleResult result = BattleResult.InProgress;

        public TurnPhase CurrentPhase => currentPhase;
        public int CurrentRound => currentRound;
        public bool IsPlayerTurn => currentPhase == TurnPhase.PlayerTurn;
        public BattleResult Result => result;

        public System.Action<TurnPhase> OnTurnChanged;
        public System.Action<BattleResult> OnBattleEnded;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);
        }

        private void Start()
        {
            StartPlayerTurn();
        }

        public void EndPlayerTurn()
        {
            if (result != BattleResult.InProgress) return;
            if (currentPhase != TurnPhase.PlayerTurn) return;

            currentPhase = TurnPhase.EnemyTurn;
            Debug.Log($"<color=#EF5350><b>[Enemy Turn]</b></color> Round {currentRound} - Dracomancer is preparing to act...");
            OnTurnChanged?.Invoke(currentPhase);

            StartCoroutine(ExecuteEnemyTurnRoutine());
        }

        private void StartPlayerTurn()
        {
            CheckBattleConditions();
            if (result != BattleResult.InProgress) return;

            currentPhase = TurnPhase.PlayerTurn;
            Debug.Log($"<color=#64B5F6><b>[Player Turn]</b></color> Round {currentRound} - Your turn, Commander!");

            // Reset action economy for all player units
            TacticalUnit3D[] units = FindObjectsByType<TacticalUnit3D>(FindObjectsSortMode.None);
            foreach (var u in units)
            {
                if (u != null && u.Faction == UnitFaction.Player)
                {
                    u.ResetTurnActions();
                }
            }

            OnTurnChanged?.Invoke(currentPhase);

            // Resolve Environmental Hazards for Player units standing in Magma / Deep Water
            StartCoroutine(ResolveFactionHazards(UnitFaction.Player));
        }

        private IEnumerator ResolveFactionHazards(UnitFaction faction)
        {
            TacticalUnit3D[] units = FindObjectsByType<TacticalUnit3D>(FindObjectsSortMode.None);
            foreach (var u in units)
            {
                if (u == null || u.CurrentHealth <= 0 || u.Faction != faction) continue;

                HexTile3D tile = u.CurrentTile;
                if (tile == null)
                {
                    u.ReacquireCurrentTile();
                    tile = u.CurrentTile;
                }

                if (tile != null && tile.State == TileState.Magma && !u.IsMagmaImmune)
                {
                    Debug.Log($"<color=#FF3D00><b>[Turn Hazard]</b></color> {u.UnitName} is standing in molten Magma! Took 3 burn damage.");
                    u.TakeDamage(3, "🔥 MAGMA BURN! -3");
                    TacticalCameraController.Instance?.Shake(0.32f, 0.35f);
                    SoundManager3D.Instance?.PlaySpellCast(isFire: true);
                    yield return new WaitForSeconds(0.35f);
                }
                else if (tile != null && tile.State == TileState.Water && tile.TierLevel >= 2)
                {
                    Debug.Log($"<color=#0288D1><b>[Turn Hazard]</b></color> {u.UnitName} is submerged in Deep Water! Took 2 hazard damage.");
                    u.TakeDamage(2, "🌊 DEEP WATER! -2");
                    TacticalCameraController.Instance?.Shake(0.2f, 0.25f);
                    SoundManager3D.Instance?.PlaySpellCast(isFire: false);
                    yield return new WaitForSeconds(0.35f);
                }
            }
            CheckBattleConditions();
        }

        /// <summary>
        /// Evaluates whether the battle has been won or lost.
        /// </summary>
        public void CheckBattleConditions()
        {
            if (result != BattleResult.InProgress) return;

            TacticalUnit3D[] units = FindObjectsByType<TacticalUnit3D>(FindObjectsSortMode.None);
            int alivePlayers = 0;
            int aliveEnemies = 0;

            foreach (var u in units)
            {
                if (u == null || u.CurrentHealth <= 0) continue;
                if (u.Faction == UnitFaction.Player) alivePlayers++;
                else if (u.Faction == UnitFaction.Enemy) aliveEnemies++;
            }

            if (alivePlayers == 0 && aliveEnemies > 0)
            {
                result = BattleResult.Defeat;
                Debug.Log("<color=#EF5350><b>[Battle Ended] DEFEAT!</b></color> Commander has fallen in battle.");
                OnBattleEnded?.Invoke(result);
            }
            else if (aliveEnemies == 0)
            {
                result = BattleResult.Victory;
                Debug.Log("<color=#FFD700><b>[Battle Ended] VICTORY!</b></color> All enemy forces vanquished!");
                OnBattleEnded?.Invoke(result);
            }
        }

        /// <summary>
        /// Restarts the battle cleanly by reloading the scene.
        /// </summary>
        public void RestartBattle()
        {
            Debug.Log("<color=#00E5FF><b>[Restart]</b></color> Reloading battlefield...");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private IEnumerator ExecuteEnemyTurnRoutine()
        {
            // Ensure CombatFeedbackManager exists
            if (CombatFeedbackManager.Instance == null)
            {
                var feedbackObj = new GameObject("CombatFeedbackManager");
                feedbackObj.AddComponent<CombatFeedbackManager>();
            }

            CombatFeedbackManager.Instance.ShowBanner("⚔️ ENEMY PHASE", "Enemy squad is coordinating attacks...", 1.2f, new Color(0.95f, 0.25f, 0.2f));
            yield return new WaitForSeconds(0.8f);

            // Resolve Environmental Hazards for Enemy units standing in Magma / Deep Water
            yield return ResolveFactionHazards(UnitFaction.Enemy);
            if (result != BattleResult.InProgress) yield break;

            TacticalUnit3D[] allUnits = Object.FindObjectsByType<TacticalUnit3D>(FindObjectsSortMode.None);
            List<TacticalUnit3D> enemies = new List<TacticalUnit3D>();
            List<TacticalUnit3D> players = new List<TacticalUnit3D>();

            foreach (var u in allUnits)
            {
                if (u == null || u.CurrentHealth <= 0) continue;
                if (u.Faction == UnitFaction.Enemy) enemies.Add(u);
                else if (u.Faction == UnitFaction.Player) players.Add(u);
            }

            // Sequentially activate each enemy
            foreach (var enemy in enemies)
            {
                if (enemy == null || enemy.CurrentHealth <= 0) continue;

                // Re-evaluate living players
                players.RemoveAll(p => p == null || p.CurrentHealth <= 0);
                if (players.Count == 0) break;

                TacticalUnit3D targetPlayer = GetClosestUnit(enemy, players);
                if (targetPlayer == null) continue;

                yield return ExecuteSingleEnemyAI(enemy, targetPlayer);
                yield return new WaitForSeconds(0.45f);

                CheckBattleConditions();
                if (result != BattleResult.InProgress) yield break;
            }

            CheckBattleConditions();
            if (result != BattleResult.InProgress) yield break;

            yield return new WaitForSeconds(0.4f);

            // Advance Round & Pass to Player
            currentRound++;
            StartPlayerTurn();
            CombatFeedbackManager.Instance?.ShowBanner($"ROUND {currentRound} - PLAYER TURN", "Your turn, Commander!", 1.2f, new Color(0.25f, 0.7f, 1.0f));
        }

        private IEnumerator ExecuteSingleEnemyAI(TacticalUnit3D enemy, TacticalUnit3D targetPlayer)
        {
            if (enemy == null || targetPlayer == null || HexGrid3D.Instance == null) yield break;

            if (enemy.CurrentTile == null) enemy.ReacquireCurrentTile();
            if (targetPlayer.CurrentTile == null) targetPlayer.ReacquireCurrentTile();

            int currentDist = enemy.Coordinates.DistanceTo(targetPlayer.Coordinates);

            // 1. ADVANCE TOWARDS TARGET IF OUT OF MELEE RANGE
            if (currentDist > 1)
            {
                var path = HexPathfinder3D.FindPath(HexGrid3D.Instance, enemy.CurrentTile, targetPlayer.CurrentTile);
                if (path != null && path.Count > 1)
                {
                    int steps = Mathf.Min(enemy.EffectiveMoveRange, path.Count - 1);
                    if (steps > 0)
                    {
                        CombatFeedbackManager.Instance.ShowBanner("ENEMY MOVEMENT", $"{enemy.UnitName} is advancing!", 0.8f, new Color(0.95f, 0.45f, 0.2f));
                        List<HexTile3D> movePath = new List<HexTile3D>();
                        for (int i = 0; i < steps; i++)
                        {
                            movePath.Add(path[i]);
                        }

                        yield return enemy.MoveAlongPath(movePath, stepDuration: 0.22f);
                        yield return new WaitForSeconds(0.35f);
                    }
                }
            }

            // 2. COMBAT ACTION
            int newDist = enemy.Coordinates.DistanceTo(targetPlayer.Coordinates);

            // Option A: Adjacent (Melee Strike or Push)
            if (newDist == 1)
            {
                if (enemy.Archetype == UnitArchetype.Minion)
                {
                    CombatFeedbackManager.Instance.ShowBanner("MINION ATTACK!", $"{enemy.UnitName} strikes {targetPlayer.UnitName}!", 1.0f, new Color(0.95f, 0.35f, 0.2f));
                    yield return enemy.PlayAttackLunge(targetPlayer.transform.position, 0.22f);
                    targetPlayer.TakeDamage(enemy.EffectiveAttackDamage);
                    yield return new WaitForSeconds(0.5f);
                }
                else
                {
                    CombatFeedbackManager.Instance.ShowBanner("ENEMY ATTACK!", $"{enemy.UnitName} unleashes 💨 Kinetic Push!", 1.1f, new Color(0.85f, 0.25f, 0.95f));
                    yield return enemy.PlayAttackLunge(targetPlayer.transform.position, 0.24f);
                    yield return PushMechanic3D.ExecutePushRoutine(enemy, targetPlayer, HexGrid3D.Instance);
                    yield return new WaitForSeconds(0.6f);
                }
            }
            // Option B: Ranged Spell (Dracomancer Fireball)
            else if (newDist <= 3 && (enemy.Archetype == UnitArchetype.Commander || enemy.UnitName.Contains("Dracomancer")))
            {
                CombatFeedbackManager.Instance.ShowBanner("ENEMY SPELL!", $"{enemy.UnitName} casts 🔥 Fireball at {targetPlayer.UnitName}!", 1.1f, new Color(1.0f, 0.45f, 0.1f));
                yield return enemy.PlayAttackLunge(targetPlayer.transform.position, 0.22f);

                Vector3 casterHand = enemy.transform.position + Vector3.up * 0.8f;
                Vector3 targetChest = targetPlayer.transform.position + Vector3.up * 0.8f;
                yield return CombatFeedbackManager.Instance.SpawnSpellProjectile(casterHand, targetChest, new Color(1.0f, 0.4f, 0.1f), 0.28f);

                TerrainReactionSystem.ApplySpell(targetPlayer.CurrentTile, ElementType.Fire, damage: 3);
                yield return new WaitForSeconds(0.7f);
            }
            else
            {
                yield return new WaitForSeconds(0.3f);
            }
        }

        private TacticalUnit3D GetClosestUnit(TacticalUnit3D origin, List<TacticalUnit3D> targets)
        {
            TacticalUnit3D closest = null;
            int minDist = int.MaxValue;

            foreach (var t in targets)
            {
                if (t == null || t.CurrentHealth <= 0) continue;
                int d = origin.Coordinates.DistanceTo(t.Coordinates);
                if (d < minDist)
                {
                    minDist = d;
                    closest = t;
                }
            }

            return closest;
        }
    }
}

