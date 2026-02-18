#if UNITY_EDITOR
using System;
using System.Linq;
using CustomComponent;
using DataTable;
using Definition.DataStruct;
using Entity;
using Game.Utility;
using UnityEditor;
using UnityEngine;

namespace StarForce.Editor
{
    public class PlayerBuffConsoleWindow : EditorWindow
    {
        private const string WindowTitle = "Player Buff Console";

        private string _searchText = string.Empty;
        private int _selectedIndex;
        private int _addCount = 1;
        private Vector2 _scroll;
        private DRProp[] _allProps = Array.Empty<DRProp>();
        private DRProp[] _filteredProps = Array.Empty<DRProp>();
        private string[] _displayNames = Array.Empty<string>();

        private float _spawnRateScaleInput = 1f;
        private float _extendDurationSeconds = 30f;

        [MenuItem("StarForce/Debug/Player Buff Console")]
        private static void Open()
        {
            GetWindow<PlayerBuffConsoleWindow>(WindowTitle);
        }

        private void OnEnable()
        {
            RefreshPropList();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Player Buff Console (Editor Only)", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
            {
                DrawBuffSection();

                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField(string.Empty, GUI.skin.horizontalSlider);
                EditorGUILayout.Space(8f);

                DrawBattleControlSection();
            }

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play mode to use debug controls.", MessageType.Info);
            }
        }

        private void DrawBuffSection()
        {
            EditorGUILayout.LabelField("Buff Debug", EditorStyles.boldLabel);
            DrawToolbar();
            DrawBuffSelector();
            DrawSelectedBuffPreview();
            DrawBuffActions();
        }

        private void DrawBattleControlSection()
        {
            EditorGUILayout.LabelField("Battle Debug", EditorStyles.boldLabel);

            EnemyManagerComponent enemyManager = GameEntry.EnemyManager;
            Player player = FindPlayer();
            if (enemyManager == null)
            {
                EditorGUILayout.HelpBox("EnemyManager is unavailable.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Current Spawn Rate Scale", enemyManager.SpawnRateScale.ToString("F2"));
            EditorGUILayout.LabelField("Battle Time",
                $"{enemyManager.ElapsedBattleTime:F1}s / {enemyManager.BattleDuration:F1}s");

            _spawnRateScaleInput = Mathf.Clamp(EditorGUILayout.FloatField("Spawn Rate Scale", _spawnRateScaleInput), 0.1f,
                50f);
            _extendDurationSeconds = Mathf.Clamp(EditorGUILayout.FloatField("Add Duration (Seconds)", _extendDurationSeconds),
                1f, 3600f);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Spawn Rate"))
            {
                enemyManager.SetSpawnRateScale(_spawnRateScaleInput);
                ShowNotification(new GUIContent($"Spawn rate scale = {enemyManager.SpawnRateScale:F2}"));
            }

            if (GUILayout.Button("x0.5"))
            {
                _spawnRateScaleInput = Mathf.Max(0.1f, enemyManager.SpawnRateScale * 0.5f);
                enemyManager.SetSpawnRateScale(_spawnRateScaleInput);
            }

            if (GUILayout.Button("x2"))
            {
                _spawnRateScaleInput = enemyManager.SpawnRateScale * 2f;
                enemyManager.SetSpawnRateScale(_spawnRateScaleInput);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Add Battle Duration"))
            {
                enemyManager.AddBattleDuration(_extendDurationSeconds);
                ShowNotification(new GUIContent($"+{_extendDurationSeconds:F0}s Battle Duration"));
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Player Weapon", player == null ? "Player not found" : (player.WeaponEnabled ? "Enabled" : "Disabled"));
            using (new EditorGUI.DisabledScope(player == null))
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Disable Weapons"))
                {
                    player.SetWeaponEnabled(false);
                    ShowNotification(new GUIContent("Player weapons disabled"));
                }

                if (GUILayout.Button("Enable Weapons"))
                {
                    player.SetWeaponEnabled(true);
                    ShowNotification(new GUIContent("Player weapons enabled"));
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            _searchText = EditorGUILayout.TextField("Search", _searchText);
            if (GUILayout.Button("Refresh", GUILayout.Width(80f)))
            {
                RefreshPropList();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawBuffSelector()
        {
            ApplyFilter(_searchText);

            if (_displayNames.Length == 0)
            {
                EditorGUILayout.HelpBox("No Buff data found. Ensure data tables are loaded.", MessageType.Warning);
                return;
            }

            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _displayNames.Length - 1);
            _selectedIndex = EditorGUILayout.Popup("Buff", _selectedIndex, _displayNames);
        }

        private void DrawSelectedBuffPreview()
        {
            DRProp selectedProp = GetSelectedProp();
            if (selectedProp == null) return;

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(120f));
            EditorGUILayout.LabelField("Title", selectedProp.Title);
            EditorGUILayout.LabelField("Rarity", selectedProp.Rarity.ToString());
            EditorGUILayout.LabelField("Description");
            EditorGUILayout.HelpBox(ItemDescUtility.CreatePropDescription(selectedProp.Modifiers), MessageType.None);
            EditorGUILayout.EndScrollView();
        }

        private void DrawBuffActions()
        {
            DRProp selectedProp = GetSelectedProp();
            if (selectedProp == null) return;

            _addCount = Mathf.Clamp(EditorGUILayout.IntField("Add Count", _addCount), 1, 99);
            if (!GUILayout.Button("Add Buff To Player", GUILayout.Height(28f))) return;

            Player player = FindPlayer();
            if (player == null)
            {
                ShowNotification(new GUIContent("Player not found"));
                return;
            }

            for (int i = 0; i < _addCount; i++)
            {
                player.AddProp(new PropItem(selectedProp.Modifiers, selectedProp.Rarity, selectedProp.Title,
                    selectedProp.IconAssetName));
            }

            ShowNotification(new GUIContent($"Added Buff: {selectedProp.Title} x{_addCount}"));
        }

        private void RefreshPropList()
        {
            if (!EditorApplication.isPlaying || GameEntry.DataTable == null)
            {
                _allProps = Array.Empty<DRProp>();
                _filteredProps = Array.Empty<DRProp>();
                _displayNames = Array.Empty<string>();
                return;
            }

            var table = GameEntry.DataTable.GetDataTable<DRProp>();
            _allProps = table != null ? table.ToArray() : Array.Empty<DRProp>();
            _selectedIndex = 0;
            ApplyFilter(_searchText);
        }

        private void ApplyFilter(string searchText)
        {
            if (_allProps == null || _allProps.Length == 0)
            {
                _filteredProps = Array.Empty<DRProp>();
                _displayNames = Array.Empty<string>();
                return;
            }

            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredProps = _allProps;
            }
            else
            {
                string keyword = searchText.Trim();
                _filteredProps = _allProps.Where(p =>
                    p != null &&
                    (p.Title?.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0 ||
                     p.Id.ToString().Contains(keyword))).ToArray();
            }

            _displayNames = _filteredProps.Select(p => $"[{p.Id}] {p.Title} ({p.Rarity})").ToArray();
            if (_displayNames.Length == 0)
            {
                _selectedIndex = 0;
                return;
            }

            _selectedIndex = Mathf.Clamp(_selectedIndex, 0, _displayNames.Length - 1);
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

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode || state == PlayModeStateChange.EnteredEditMode)
            {
                RefreshPropList();
                Repaint();
            }
        }
    }
}
#endif
