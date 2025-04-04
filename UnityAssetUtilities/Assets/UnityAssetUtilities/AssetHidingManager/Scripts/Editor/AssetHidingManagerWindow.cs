using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.IO;
using System.Linq;

namespace UnityAssetUtilities
{
    public class AssetHidingManagerWindow : EditorWindow
    {
        [SerializeField]
        private TreeViewState assetsTreeViewState;

        [SerializeField]
        private bool showHelp;

        [SerializeField]
        private AssetsTreeView assetsTreeView;

        private const string defaultSettingsAssetPath = "Assets/Settings/AssetHidingManagerSettings.asset";

        private static AssetHidingManagerSettings assetHidingManagerSettings;

        private static Texture AssetHidingManagerIcon => EditorGUIUtility.isProSkin ? assetHidingManagerSettings.iconSet["assetsHidingManager_icon"] : assetHidingManagerSettings.iconSet["assetsHidingManager_icon_dark"];
        private static Texture AssetHidingManagerShownIcon => EditorGUIUtility.isProSkin ? assetHidingManagerSettings.iconSet["assetHidingManager_shown_icon"] : assetHidingManagerSettings.iconSet["assetHidingManager_shown_icon_dark"];
        private static Texture AssetHidingManagerHiddenIcon => EditorGUIUtility.isProSkin ? assetHidingManagerSettings.iconSet["assetHidingManager_hidden_icon"] : assetHidingManagerSettings.iconSet["assetHidingManager_hidden_icon_dark"];

        private static readonly List<int> emptySelectionList = new List<int>();

        [MenuItem("Tools/Asset Hiding Manager")]
        private static void ShowWindow()
        {
            LoadAssetHidingManagerSettings();
            var window = GetWindow<AssetHidingManagerWindow>();
            window.titleContent = new GUIContent("Asset Hiding Manager", AssetHidingManagerIcon);
            window.Show();
        }

        private void OnEnable()
        {
            assetsTreeViewState ??= new TreeViewState();
            assetsTreeView = new AssetsTreeView(assetsTreeViewState);
            assetsTreeView.SetExpanded(1, true);
            Selection.selectionChanged += OnSelectionChanged;
        }
        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.FlexibleSpace();
            bool wasSelectionSyncEnabled = assetHidingManagerSettings.IsSelectionSyncEnabled();
            bool isSelectionSyncEnabled = assetHidingManagerSettings.SetSelectionSync(GUILayout.Toggle(wasSelectionSyncEnabled, new GUIContent(/*"Sync selection", */EditorGUIUtility.IconContent("d_Refresh").image, $"Synchronizes selection with project asset browser.\nSynchronization is {(wasSelectionSyncEnabled ? "enabled" : "disabled")}."), EditorStyles.toolbarButton));

            showHelp = GUILayout.Toggle(showHelp, new GUIContent(/*"Info",*/ EditorGUIUtility.IconContent("d__Help").image, "Shows help message."), EditorStyles.toolbarButton);
            EditorGUILayout.EndHorizontal();
            if (showHelp)
            {
                EditorGUILayout.HelpBox($"This is a simple tool to hide and unhide assets for import.\nAll it does is renaming assets and their meta files by adding or removing '{AssetHidingManager.hiddenAssetPrefix}' prefix at the beginning of the filename.\nHidden assets are not visible in Project window, and won't be included in build. There are also ommited during import.\nTo hide/unhide asset press eye icon next to its name.", MessageType.Info);
            }

            var rect = EditorGUILayout.GetControlRect();
            assetsTreeView.syncSelection = isSelectionSyncEnabled;
            assetsTreeView.OnGUI(new Rect(rect.x, rect.y, position.width, position.height - rect.y));
        }

        private void OnSelectionChanged()
        {
            if (assetHidingManagerSettings.IsSelectionSyncEnabled())
            {
                if (Selection.activeObject != null)
                {
                    var path = AssetDatabase.GetAssetPath(Selection.activeObject);
                    assetsTreeView.SelectAsset(path);
                }
                else
                {
                    if (!assetsTreeView.changedSelection)
                    {
                        assetsTreeView.SetSelection(emptySelectionList);
                    }
                }
                assetsTreeView.changedSelection = false;
                Repaint();
            }
        }

        private static void LoadAssetHidingManagerSettings()
        {
            string[] assetGUIDs = AssetDatabase.FindAssets($"t:{nameof(assetHidingManagerSettings)}");
            if (assetGUIDs != null && assetGUIDs.Length > 0)
            {
                assetHidingManagerSettings = AssetsUtility.LoadAssetAtGUID<AssetHidingManagerSettings>(assetGUIDs[0]);
            }
            else
            {
                assetHidingManagerSettings = AssetDatabase.LoadAssetAtPath(defaultSettingsAssetPath, typeof(AssetHidingManagerSettings)) as AssetHidingManagerSettings;
                if (assetHidingManagerSettings == null)
                {
                    CreateAssetHidingManagerSettingsAsset();
                }
            }
            if (assetHidingManagerSettings == null)
            {
                Debug.LogError($"{nameof(assetHidingManagerSettings)} asset couldn't be loaded or created.");
            }
        }
        private static void CreateAssetHidingManagerSettingsAsset()
        {
            assetHidingManagerSettings = ScriptableObject.CreateInstance<AssetHidingManagerSettings>();
            if (!AssetDatabase.IsValidFolder("Assets/Settings")) AssetDatabase.CreateFolder("Assets", "Settings");
            foreach (var iconSet in Resources.FindObjectsOfTypeAll<IconSet>())
            {
                if (iconSet.name.Equals("AssetHidingManagerIconSet"))
                {
                    assetHidingManagerSettings.iconSet = iconSet;
                }
            }
            AssetDatabase.CreateAsset(assetHidingManagerSettings, AssetDatabase.GenerateUniqueAssetPath("Assets/Settings/AssetHidingManagerSettings.asset"));
            AssetDatabase.SaveAssets();
            Selection.activeObject = assetHidingManagerSettings;
        }


        private class AssetsTreeView : TreeView
        {
            public bool changedSelection = false;
            public bool syncSelection = false;

            [SerializeField]
            private static Dictionary<int, string> paths = new Dictionary<int, string>();
            [SerializeField]
            private static List<TreeViewItem> allItems = new List<TreeViewItem>();

            private TreeViewItem draggedItem;
            private const float iconWidth = 18;
            private const float toggleWidth = 18;

            public AssetsTreeView(TreeViewState treeViewState)
                : base(treeViewState)
            {
                Reload();
            }

            public void SelectAsset(string unityAssetPath)
            {
                string assetPath = AssetsUtility.UnifyDirectorySeparators(unityAssetPath.StartsWith(AssetsUtility.assetsFolderName) ? AssetsUtility.AssetsPathToAbsolutePath(unityAssetPath) : unityAssetPath);
                foreach (var item in allItems)
                {
                    if (paths.TryGetValue(item.id, out string path))
                    {
                        if (path.Equals(assetPath))
                        {
                            this.SetSelection(new List<int>() { item.id });
                            var parent = item.parent;
                            while (parent != null)
                            {
                                this.SetExpanded(parent.id, true);
                                parent = parent.parent;
                            }
                        }
                    }
                }
            }

            protected override TreeViewItem BuildRoot()
            {
                paths.Clear();

                var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };

                allItems = new List<TreeViewItem>();
                int id = 0;
                var assetsInfo = new System.IO.DirectoryInfo(Application.dataPath);
                BuildTreeNode(assetsInfo, null, 0, ref allItems, ref id);

                SetupParentsAndChildrenFromDepths(root, allItems);

                return root;
            }

            private void BuildTreeNode(System.IO.DirectoryInfo directory, System.IO.FileInfo file, int depth, ref List<TreeViewItem> items, ref int id)
            {
                ++id;
                paths.Add(id, file != null ? file.FullName : directory.FullName);
                items.Add(new TreeViewItem { id = id, depth = depth, displayName = file != null ? file.Name : directory.Name });
                if (directory != null)
                {
                    foreach (var dir in directory.GetDirectories())
                    {
                        BuildTreeNode(dir, null, depth + 1, ref items, ref id);
                    }
                    var files = new List<FileInfo>(directory.GetFiles());
                    files.Sort((a, b) => AssetHidingManager.GetHiddenAssetPath(a.Name, false).CompareTo(AssetHidingManager.GetHiddenAssetPath(b.Name, false)));
                    foreach (var fil in files)
                    {
                        if (fil.Extension != AssetsUtility.metaFileExtension)
                        {
                            BuildTreeNode(null, fil, depth + 1, ref items, ref id);
                        }
                    }
                }
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                //base.RowGUI(args);
                var item = args.item;

                CellGUI(args.rowRect, item, ref args);
            }
            private void CellGUI(Rect cellRect, TreeViewItem item, ref RowGUIArgs args)
            {
                bool isHidden = AssetHidingManager.IsAssetHidden(args.label);
                bool isOpened = IsExpanded(item.id);
                if (isHidden) GUI.color = new Color(0.75f, 0.35f, 0);

                // Center the cell rect vertically using EditorGUIUtility.singleLineHeight.
                // This makes it easier to place controls and icons in the cells.
                CenterRectUsingSingleLineHeight(ref cellRect);
                cellRect.x += GetContentIndent(item);

                Rect iconRect = cellRect;
                iconRect.width = iconWidth;
                cellRect.x += iconRect.width;
                cellRect.width -= iconRect.width;
                if (item.hasChildren)
                {
                    EditorGUI.LabelField(iconRect, EditorGUIUtility.IconContent(isOpened ? "d_FolderOpened Icon" : "d_Folder Icon"));
                }

                if (item.id > 1)
                {
                    paths.TryGetValue(item.id, out string path);

                    if (!item.hasChildren)
                    {
                        if (!string.IsNullOrEmpty(path))
                        {
                            EditorGUI.LabelField(iconRect, new GUIContent(GetIconForFile(path)));
                        }
                    }
                    // Make a toggle button to the left of the label text
                    Rect toggleRect = cellRect;
                    toggleRect.width = toggleWidth;
                    cellRect.x += toggleRect.width;
                    cellRect.width -= toggleRect.width;

                    bool hide = isHidden;
                    if (isHidden)
                    {
                        if (GUI.Button(toggleRect, AssetHidingManagerHiddenIcon, GUIStyle.none))
                        {
                            hide = false;
                        }
                    }
                    else
                    {
                        if (GUI.Button(toggleRect, AssetHidingManagerShownIcon, GUIStyle.none))
                        {
                            hide = true;
                        }
                    }

                    if (isHidden != hide)
                    {
                        if (AssetHidingManager.SetAssetHidden(path, hide, refreshAssetDatabase: true))
                        {
                            Reload();
                            if (!hide && syncSelection)
                            {
                                var asset = AssetDatabase.LoadAssetAtPath(AssetsUtility.AbsolutePathToAssetsPath(AssetHidingManager.GetHiddenAssetPath(path, false)), typeof(UnityEngine.Object));
                                EditorGUIUtility.PingObject(asset);
                                Selection.activeObject = asset;
                                changedSelection = true;
                            }
                        }
                    }
                }

                // Default icon and label
                EditorGUI.LabelField(cellRect, new GUIContent(AssetHidingManager.GetHiddenAssetPath(args.label, false)));
                //base.RowGUI(args);
                GUI.color = Color.white;
            }

            protected override bool CanStartDrag(CanStartDragArgs args)
            {
                draggedItem = args.draggedItem;
                if (args.draggedItem != null)
                {
                    if (args.draggedItem.id <= 1)
                    {
                        draggedItem = null;
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                return false;
            }
            protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
            {
                base.SetupDragAndDrop(args);

                DragAndDrop.PrepareStartDrag();
                var draggedRows = GetRows().Where(item => args.draggedItemIDs.Contains(item.id)).ToList();
                DragAndDrop.SetGenericData("GenericDragColumnDragging", draggedRows);
                DragAndDrop.objectReferences = new Object[] { };
                string title = draggedRows.Count == 1 ? draggedRows[0].displayName : "< Multiple >";
                DragAndDrop.StartDrag(title);
            }
            protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
            {
                if (args.parentItem != null && paths.ContainsKey(args.parentItem.id))
                {
                    if (args.performDrop)
                    {
                        string destinationPath = paths[args.parentItem.id];
                        if (!Directory.Exists(destinationPath))
                        {
                            destinationPath = Path.GetDirectoryName(destinationPath);
                        }
                        string path = paths[draggedItem.id];
                        string metaFilePath = AssetsUtility.GetMetaFilePath(path);

                        string filename = Path.GetFileName(path);
                        string metaFilename = Path.GetFileName(metaFilePath);

                        File.Move(path, Path.Combine(destinationPath, filename));
                        File.Move(metaFilePath, Path.Combine(destinationPath, metaFilename));
                        draggedItem = null;
                        Reload();
                    }
                }
                return DragAndDropVisualMode.Move;
            }

            protected override void SingleClickedItem(int id)
            {
                base.SingleClickedItem(id);
                if (syncSelection && paths.ContainsKey(id))
                {
                    string unityAssetPath = AssetsUtility.AbsolutePathToAssetsPath(paths[id]);
                    var asset = AssetDatabase.LoadAssetAtPath(unityAssetPath, typeof(UnityEngine.Object));
                    EditorGUIUtility.PingObject(asset);
                    Selection.activeObject = asset;
                    changedSelection = true;
                }
            }
            protected override void DoubleClickedItem(int id)
            {
                base.DoubleClickedItem(id);
                if (paths.ContainsKey(id))
                {
                    if (Directory.Exists(paths[id]))
                    {
                        SetExpanded(id, !IsExpanded(id));
                    }
                    else
                    {
                        Open(paths[id]);
                    }
                }
            }

            protected override void ContextClicked()
            {
                base.ContextClicked();
                if (GetSelection().Count <= 0)
                {
                    Vector2 mousePos = Event.current.mousePosition;
                    Selection.SetActiveObjectWithContext(null, null);
                    EditorUtility.DisplayPopupMenu(new Rect(mousePos.x, mousePos.y, 0, 0), $"Assets/", null);
                }
            }

            protected override void ContextClickedItem(int id)
            {
                base.ContextClickedItem(id);
                if (paths.ContainsKey(id))
                {
                    Vector2 mousePos = Event.current.mousePosition;
                    string absolutePath = paths[id].Trim();
                    string path = AssetsUtility.AbsolutePathToAssetsPath(absolutePath);

                    var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                    if (asset != null)
                    {
                        Selection.SetActiveObjectWithContext(asset, null);
                        EditorUtility.DisplayPopupMenu(new Rect(mousePos.x, mousePos.y, 0, 0), $"Assets/", null);
                    }
                    else
                    {
                        GenericMenu menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Open"), false, Open, paths[id]);
                        menu.AddItem(new GUIContent("Show in Explorer"), false, ShowInExplorer, paths[id]);
                        menu.AddItem(new GUIContent("Delete"), false, Delete, paths[id]);
                        menu.AddItem(new GUIContent("Unhide"), false, Unhide, paths[id]);
                        menu.ShowAsContext();
                    }
                }
            }

            private void Open(object parameter)
            {
                if (parameter is string path)
                {
                    System.Diagnostics.Process.Start("explorer.exe", path);
                }
            }
            private void ShowInExplorer(object parameter)
            {
                if (parameter is string path)
                {
                    System.Diagnostics.Process.Start("explorer.exe", Directory.Exists(path) ? path : Path.GetDirectoryName(path));
                }
            }
            private void Delete(object parameter)
            {
                if (parameter is string path)
                {
                    if (EditorUtility.DisplayDialog("Delete asset", "Are you sure to delete hidden asset?", "Yes", "No"))
                    {
                        string metaFilePath = AssetsUtility.GetMetaFilePath(path);
                        try
                        {
                            File.Delete(path);
                            File.Delete(metaFilePath);
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogError(e);
                        }
                    }
                }
            }
            private void Unhide(object parameter)
            {
                if (parameter is string path)
                {
                    bool isHidden = AssetHidingManager.IsAssetAtPathHidden(path);
                    if (isHidden)
                    {
                        if (AssetHidingManager.SetAssetHidden(path, false, refreshAssetDatabase: true))
                        {
                            Reload();
                            if (syncSelection)
                            {
                                var asset = AssetDatabase.LoadAssetAtPath(AssetsUtility.AbsolutePathToAssetsPath(AssetHidingManager.GetHiddenAssetPath(path, false)), typeof(UnityEngine.Object));
                                EditorGUIUtility.PingObject(asset);
                                Selection.activeObject = asset;
                                changedSelection = true;
                            }
                        }
                    }
                }
            }

            private Texture GetIconForFile(string path)
            {
                string unityAssetPath = AssetsUtility.AbsolutePathToAssetsPath(path);
                var icon = AssetDatabase.GetCachedIcon(unityAssetPath);
                if (icon != null)
                {
                    assetHidingManagerSettings.CacheAssetIcon(System.IO.Path.GetExtension(path), icon);
                    return icon;
                }
                else if (assetHidingManagerSettings.TryGetIconForAsset(System.IO.Path.GetExtension(path), out var cachedIcon))
                {
                    return cachedIcon;
                }
                else
                {
                    const string defaultAssetIcon = "DefaultAsset Icon";
                    return EditorGUIUtility.IconContent(defaultAssetIcon).image;
                }
            }
        }
    }
}