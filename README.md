# Unity Asset Utilities
Set of tools designed to help managing assets in Unity projects.</br>
New tools and updates may appear over time.

## External Assets
Solution for managing assets outside of "Assets" folder.
Allows defining a binding between file outside of "Assets" folder and Unity asset.<br/>
Files can be automatically updated to reflect the state of eachother. Assets can be updated based on source files, source files based on assets or both.</br>
This can come in handy if you plan to use same file for multiple projects. You could create an external asset binding so every time this file is modified, asset in the project is automatically updated.</br>
External asset can be used for example to synchronize your dll library shared by your Unity projects, so that always it's newest version could be used.</br>
Generally this solution could help if you plan to use asset stored outside of "Assets" folder for any reason.

## IconSet
Simple ScriptableObject allowing to store multiple Textures in a single asset.</br>
It is used by other utilities in the project to simplify referencing textures used by editor scripts.

## EditorIcons
Allows to extract Unity Editor icons. To get editor icons information simply create EditorIcons asset.</br>
Extraction of an icon asset is done by saving its copy as an EditorIcons asset's subassets.</br>
Based on <a href="https://github.com/halak/unity-editor-icons">halak/unity-editor-icons<a> 

## IconChanger
Simple solution that allows to change selected asset icon.

## Asset Hiding Manager
Solution for managing hidden assets.
Displays and allows to hide/unhide assets from Unity. Hidden assets are not included in build and asset importing process.</br>
This can help to reduce build size and project import time when there are assets that won't be used in current build.</br>
Hiding and unhiding of assets is done by renaming files to special names ignored by Unity asset database (names starting with dot).
