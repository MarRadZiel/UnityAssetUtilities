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
            if (assetsTreeViewState == null)
                assetsTreeViewState = new TreeViewState();

            assetsTreeView = new AssetsTreeView(assetsTreeViewState);
            assetsTreeView.SetExpanded(1, true);
        }

        private void OnGUI()
        {
            showHelp = GUILayout.Toggle(showHelp, new GUIContent("Info", EditorGUIUtility.IconContent("d__Help").image), "Button");
            if (showHelp)
            {
                EditorGUILayout.HelpBox("This is a simple tool to hide and unhide assets for import.\nAll it does is renaming assets and their meta files by adding or removing '.' character at the beginning of the filename.\nHidden assets are not visible in Project window, and won't be included in build. There are also ommited during import.\nTo hide/unhide asset press eye icon next to its name.", MessageType.Info);
            }
            var rect = EditorGUILayout.GetControlRect();

            assetsTreeView.OnGUI(new Rect(rect.x, rect.y, position.width, position.height - rect.y));
        }


        private static void LoadAssetHidingManagerSettings()
        {
            string[] assetGUIDs = AssetDatabase.FindAssets($"t:{nameof(assetHidingManagerSettings)}");
            if (assetGUIDs != null && assetGUIDs.Length > 0)
            {
                assetHidingManagerSettings = AssetDatabase.LoadAssetAtPath<AssetHidingManagerSettings>(AssetDatabase.GUIDToAssetPath(assetGUIDs[0]));
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
            [SerializeField]
            private static Dictionary<int, string> paths = new Dictionary<int, string>();
            [SerializeField]
            private static List<TreeViewItem> allItems = new List<TreeViewItem>();

            private TreeViewItem draggedItem;
            private float iconWidth = 18;
            private float toggleWidth = 18;

            public AssetsTreeView(TreeViewState treeViewState)
                : base(treeViewState)
            {
                Reload();
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
                    foreach (var fil in directory.GetFiles())
                    {
                        if (fil.Extension != ".meta")
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
                bool isHidden = args.label.StartsWith(".");
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
                        SetAssetHidden(path, hide);
                    }
                }

                // Default icon and label
                EditorGUI.LabelField(cellRect, new GUIContent(isHidden ? args.label.Substring(1) : args.label));
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
                        string metaFilePath = $"{path}.meta";

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
                    EditorUtility.DisplayPopupMenu(new Rect(mousePos.x, mousePos.y, 0, 0), "Assets/", null);
                }
            }

            protected override void ContextClickedItem(int id)
            {
                base.ContextClickedItem(id);
                if (paths.ContainsKey(id))
                {
                    Vector2 mousePos = Event.current.mousePosition;
                    string absolutePath = paths[id].Replace('\\', '/').Trim();
                    string path = AssetsUtility.AbsolutePathToAssetsPath(absolutePath);

                    var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
                    if (asset != null)
                    {
                        Selection.SetActiveObjectWithContext(asset, null);
                        EditorUtility.DisplayPopupMenu(new Rect(mousePos.x, mousePos.y, 0, 0), "Assets/", null);
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
                        string metaFilePath = $"{path}.meta";
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
                    bool isHidden = Path.GetFileName(path).StartsWith(".");

                    if (isHidden)
                    {
                        SetAssetHidden(path, false);
                    }
                }
            }

            private void SetAssetHidden(string path, bool hide)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    string filename = Path.GetFileName(path);
                    bool isFile = Path.HasExtension(path) && !string.IsNullOrEmpty(filename.Replace(Path.GetExtension(filename), string.Empty));
                    string metaFilePath = $"{path}.meta";
                    string targetPath = GetHiddenAssetPath(path, hide);
                    string targetMetaFilePath = GetHiddenAssetPath(metaFilePath, hide);

                    bool success = true;

                    if (isFile)
                    {
                        if (File.Exists(path) && !File.Exists(targetPath))
                        {
                            File.Move(path, targetPath);
                        }
                        else success = false;
                    }
                    else
                    {
                        if (Directory.Exists(path) && !Directory.Exists(targetPath))
                        {
                            Directory.Move(path, targetPath);
                        }
                        else success = false;

                    }
                    if (File.Exists(metaFilePath) && !File.Exists(targetMetaFilePath))
                    {
                        File.Move(metaFilePath, targetMetaFilePath);
                    }
                    else success = false;

                    if (!success)
                    {
                        Debug.LogError($"Could not {(hide ? "hide" : "unhide")} asset because of naming problem.\nThis could be caused by already existing file with same name as target {(hide ? "hidden" : "unhidden")} asset. This also applies to assets .meta files.");
                    }
                    else
                    {
                        Reload();
                    }
                }
            }

            private Texture GetIconForFile(string path)
            {
                string unityAssetPath = path.Replace("\\", "/").Replace(Application.dataPath, "Assets");
                var icon = AssetDatabase.GetCachedIcon(unityAssetPath);
                if (icon != null) return icon;
                else
                {
                    return EditorGUIUtility.IconContent("DefaultAsset Icon").image;
                }
            }

            private string GetHiddenAssetPath(string assetPath, bool hidden)
            {
                string fileName = Path.GetFileName(assetPath);
                bool isHidden = fileName.StartsWith(".");

                if (hidden)
                {
                    if (!isHidden)
                    {
                        return Path.Combine(Path.GetDirectoryName(assetPath), $".{fileName}");
                    }
                }
                else
                {
                    if (isHidden)
                    {
                        return Path.Combine(Path.GetDirectoryName(assetPath), fileName.Substring(1));
                    }
                }
                return assetPath;
            }
        }
    }
}