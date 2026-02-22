#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using DataTable;
using Definition.DataStruct;
using Entity;
using CustomUtility;
using Procedure;
using UnityEngine;
using UnityGameFramework.Runtime;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace CustomComponent
{
    public class RuntimeDebugPanelComponent : GameFrameworkComponent
    {
        private const float MinSpawnRate = 0.1f;
        private const float CornerTapWindow = 0.6f;
        private const int RequiredCornerTapCount = 3;

        private Rect _windowRect = new Rect(20f, 60f, 460f, 620f);
        private bool _isPanelVisible;
        private int _windowId;

        private string _searchText = string.Empty;
        private int _selectedIndex;
        private int _addCount = 1;
        private Vector2 _buffScroll;

        private float _spawnRateScaleInput = 1f;
        private float _extendDurationSeconds = 30f;

        private DRProp[] _allProps = Array.Empty<DRProp>();
        private DRProp[] _filteredProps = Array.Empty<DRProp>();
        private string[] _displayNames = Array.Empty<string>();

        private int _cornerTapCount;
        private float _lastCornerTapTime = -10f;

        protected override void Awake()
        {
            base.Awake();

            _windowId = GetInstanceID();
        }

        private void Update()
        {
            if (IsTogglePressed())
            {
                _isPanelVisible = !_isPanelVisible;
            }

            HandleCornerTapGesture();
        }

        private void OnGUI()
        {
            DrawToggleButton();
            if (!_isPanelVisible) return;

            _windowRect = GUI.Window(_windowId, _windowRect, DrawWindow, "Runtime Debug Panel");
        }

        private void DrawToggleButton()
        {
            const float width = 64f;
            const float height = 30f;
            Rect buttonRect = new Rect(Screen.width - width - 12f, 10f, width, height);
            if (GUI.Button(buttonRect, "DEBUG"))
            {
                _isPanelVisible = !_isPanelVisible;
            }
        }

        private void DrawWindow(int windowId)
        {
            EnsurePropList();

            GUILayout.BeginVertical();

            DrawBuffSection();

            GUILayout.Space(8f);
            GUILayout.Label(string.Empty, GUI.skin.horizontalSlider);
            GUILayout.Space(8f);

            DrawBattleSection();

            GUILayout.Space(8f);
            GUILayout.Label("Tips: press `F8` or tap top-left corner 3 times to toggle.", GUILayout.Height(20f));

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, 10000, 22));
        }

        private void DrawBuffSection()
        {
            GUILayout.Label("Buff Debug");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search", GUILayout.Width(52f));
            _searchText = GUILayout.TextField(_searchText);
            if (GUILayout.Button("Refresh", GUILayout.Width(80f)))
            {
                EnsurePropList(true);
            }

            GUILayout.EndHorizontal();

            ApplyFilter(_searchText);
            if (_displayNames.Length == 0)
            {
                GUILayout.Label("No Buff data. Enter battle and try again.");
                return;
            }

            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _displayNames.Length - 1);
            _buffScroll = GUILayout.BeginScrollView(_buffScroll, GUILayout.Height(120f));
            _selectedIndex = GUILayout.SelectionGrid(_selectedIndex, _displayNames, 1);
            GUILayout.EndScrollView();

            DRProp selectedProp = GetSelectedProp();
            if (selectedProp == null) return;

            GUILayout.Label($"Selected: {selectedProp.Title} ({selectedProp.Rarity})");
            GUILayout.Label(ItemDescUtility.CreatePropDescription(selectedProp.Modifiers), GUILayout.Height(40f));

            GUILayout.BeginHorizontal();
            GUILayout.Label("Count", GUILayout.Width(52f));
            string addCountText = GUILayout.TextField(_addCount.ToString(), GUILayout.Width(60f));
            if (!int.TryParse(addCountText, out _addCount)) _addCount = 1;
            _addCount = Mathf.Clamp(_addCount, 1, 99);

            if (GUILayout.Button("Add Buff To Player", GUILayout.Height(24f)))
            {
                AddSelectedBuffToPlayer(selectedProp, _addCount);
            }

            GUILayout.EndHorizontal();
        }

        private void DrawBattleSection()
        {
            GUILayout.Label("Battle Debug");
            ProcedureGame procedure = GameEntry.Procedure.CurrentProcedure as ProcedureGame;
            EnemyManagerComponent enemyManager = GameEntry.EnemyManager;
            Player player = FindPlayer();

            if (enemyManager == null)
            {
                GUILayout.Label("EnemyManager unavailable.");
                return;
            }

            if (procedure == null)
            {
                GUILayout.Label("ProcedureGame unavailable.");
                return;
            }

            GUILayout.Label($"Spawn Rate: {enemyManager.SpawnRateScale:F2}");
            GUILayout.Label($"Battle Time: {enemyManager.ElapsedBattleTime:F1}s / {enemyManager.BattleDuration:F1}s");
            GUILayout.Label($"Enemy Count: {enemyManager.CurrentEnemyCount}");

            Simulation.SimulationWorld simulationWorld = GameEntry.SimulationWorld;
            if (simulationWorld != null)
            {
                GUILayout.Space(4f);
                GUILayout.Label(
                    $"Sim Switch: Move={(simulationWorld.UseSimulationMovement ? "On" : "Off")} Job={(simulationWorld.UseJobSimulation ? "On" : "Off")} Burst={(simulationWorld.UseBurstJobs ? "On" : "Off")}");
                GUILayout.Label(
                    $"Collision Queries: total {simulationWorld.LastCollisionQueryCount} (Projectile {simulationWorld.LastProjectileCollisionQueryCount} / Area {simulationWorld.LastAreaCollisionQueryCount})");
                GUILayout.Label(
                    $"Collision Candidates: total {simulationWorld.LastCollisionCandidateCount} (Projectile {simulationWorld.LastProjectileCollisionCandidateCount} / Area {simulationWorld.LastAreaCollisionCandidateCount})");
                GUILayout.Label(
                    $"Area Resolve: hits {simulationWorld.LastResolvedAreaHitCount}");
                GUILayout.Label(
                    $"Broad Phase: cell {simulationWorld.LastCollisionCellSize:F2}, hasEnemyTargets {(simulationWorld.LastCollisionHasEnemyTargets ? "Yes" : "No")}");
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("Rate", GUILayout.Width(52f));
            string rateText = GUILayout.TextField(_spawnRateScaleInput.ToString("F2"), GUILayout.Width(60f));
            if (float.TryParse(rateText, out float parsedRate))
            {
                _spawnRateScaleInput = Mathf.Clamp(parsedRate, MinSpawnRate, 50f);
            }

            if (GUILayout.Button("Apply", GUILayout.Width(70f)))
            {
                enemyManager.SetSpawnRateScale(_spawnRateScaleInput);
            }

            if (GUILayout.Button("x0.5", GUILayout.Width(60f)))
            {
                _spawnRateScaleInput = Mathf.Max(MinSpawnRate, enemyManager.SpawnRateScale * 0.5f);
                enemyManager.SetSpawnRateScale(_spawnRateScaleInput);
            }

            if (GUILayout.Button("x2", GUILayout.Width(60f)))
            {
                _spawnRateScaleInput = enemyManager.SpawnRateScale * 2f;
                enemyManager.SetSpawnRateScale(_spawnRateScaleInput);
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Add Sec", GUILayout.Width(52f));
            string durationText = GUILayout.TextField(_extendDurationSeconds.ToString("F0"), GUILayout.Width(60f));
            if (float.TryParse(durationText, out float parsedDuration))
            {
                _extendDurationSeconds = Mathf.Clamp(parsedDuration, 1f, 3600f);
            }

            if (GUILayout.Button("Extend Battle", GUILayout.Height(24f)))
            {
                if (procedure.CurrentGameState is GameStateBattle gameState)
                {
                    gameState.AddBattleDuration(_extendDurationSeconds);
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(4f);
            GUILayout.Label($"Enemy Separation Solver: {EnemySeparationSolverProvider.CurrentSolverName}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Use Naive O(N^2)", GUILayout.Height(24f)))
            {
                EnemySeparationSolverProvider.UseNaiveSolver();
            }

            if (GUILayout.Button("Use Grid Bucket", GUILayout.Height(24f)))
            {
                EnemySeparationSolverProvider.UseGridBucketSolver();
            }

            GUILayout.EndHorizontal();

            GUILayout.Label(
                $"Player Weapon: {(player == null ? "Player not found" : (player.WeaponEnabled ? "Enabled" : "Disabled"))}");
            GUILayout.BeginHorizontal();
            GUI.enabled = player != null;
            if (GUILayout.Button("Disable Weapons", GUILayout.Height(24f)))
            {
                player.SetWeaponEnabled(false);
            }

            if (GUILayout.Button("Enable Weapons", GUILayout.Height(24f)))
            {
                player.SetWeaponEnabled(true);
            }

            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        private void EnsurePropList(bool force = false)
        {
            if (!force && _allProps != null && _allProps.Length > 0) return;

            if (GameEntry.DataTable == null)
            {
                _allProps = Array.Empty<DRProp>();
                _filteredProps = Array.Empty<DRProp>();
                _displayNames = Array.Empty<string>();
                return;
            }

            var table = GameEntry.DataTable.GetDataTable<DRProp>();
            _allProps = table != null ? table.ToArray() : Array.Empty<DRProp>();
            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, Mathf.Max(0, _allProps.Length - 1));
            ApplyFilter(_searchText);
        }

        private void ApplyFilter(string keyword)
        {
            if (_allProps == null || _allProps.Length == 0)
            {
                _filteredProps = Array.Empty<DRProp>();
                _displayNames = Array.Empty<string>();
                return;
            }

            if (string.IsNullOrWhiteSpace(keyword))
            {
                _filteredProps = _allProps;
            }
            else
            {
                string search = keyword.Trim();
                _filteredProps = _allProps.Where(p =>
                    p != null &&
                    (p.Title?.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                     p.Id.ToString().Contains(search))).ToArray();
            }

            _displayNames = _filteredProps.Select(p => $"[{p.Id}] {p.Title} ({p.Rarity})").ToArray();
            if (_displayNames.Length == 0) _selectedIndex = 0;
            else _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _displayNames.Length - 1);
        }

        private DRProp GetSelectedProp()
        {
            if (_filteredProps == null || _filteredProps.Length == 0) return null;
            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _filteredProps.Length - 1);
            return _filteredProps[_selectedIndex];
        }

        private static Player FindPlayer()
        {
            return UnityEngine.Object.FindObjectOfType<Player>();
        }

        private static void AddSelectedBuffToPlayer(DRProp prop, int count)
        {
            Player player = FindPlayer();
            if (player == null || prop == null || prop.Modifiers == null) return;

            int applyCount = Mathf.Clamp(count, 1, 99);
            for (int i = 0; i < applyCount; i++)
            {
                player.AddProp(new PropItem(prop.Modifiers, prop.Rarity, prop.Title, prop.IconAssetName));
            }
        }

        private void HandleCornerTapGesture()
        {
            if (!TryGetTouchReleased(out Vector2 touchPosition)) return;
            if (touchPosition.x > 80f || touchPosition.y < Screen.height - 80f) return;

            float now = Time.unscaledTime;
            if (now - _lastCornerTapTime > CornerTapWindow)
            {
                _cornerTapCount = 0;
            }

            _lastCornerTapTime = now;
            _cornerTapCount++;

            if (_cornerTapCount >= RequiredCornerTapCount)
            {
                _cornerTapCount = 0;
                _isPanelVisible = !_isPanelVisible;
            }
        }

        private static bool IsTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return false;
            return keyboard.backquoteKey.wasPressedThisFrame || keyboard.f8Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyDown(KeyCode.F8);
#endif
        }

        private static bool TryGetTouchReleased(out Vector2 touchPosition)
        {
#if ENABLE_INPUT_SYSTEM
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                touchPosition = default;
                return false;
            }

            var touch = touchscreen.primaryTouch;
            if (!touch.press.wasReleasedThisFrame)
            {
                touchPosition = default;
                return false;
            }

            touchPosition = touch.position.ReadValue();
            return true;
#else
            if (Input.touchCount <= 0)
            {
                touchPosition = default;
                return false;
            }

            Touch touch = Input.GetTouch(0);
            if (touch.phase != TouchPhase.Ended)
            {
                touchPosition = default;
                return false;
            }

            touchPosition = touch.position;
            return true;
#endif
        }
    }
}
#endif
