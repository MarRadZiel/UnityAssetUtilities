# Unity Asset Utilities
Set of tools designed to help managing assets in Unity projects.</br>
It's a personal project of mine that is still in development.</br>
New tools and updates can appear over time. :)

## External Assets
Solution for managing assets outside of "Assets" folder.
Allows defining a binding between file outside of "Assets" folder and Unity asset. It will be automatically updated to reflect the state of the external file.</br>
This can come in handy if you plan to use same file for multiple projects. You could create an external asset binding so every time this file is modified, asset in the project is automatically updated.</br>
External asset can be used for example to synchronize your dll library shared by your Unity projects, so that always it's newest version could be used.</br>
Generally this solution could help if you plan to use asset stored outside of "Assets" folder for any reason.

## IconSet
Simple ScriptableObject allowing to store multiple Textures in a single asset.</br>
It is used by other utilities in the project to simplify referencing textures used by editor scripts.

## EditorIcons
Allows to extract Unity Editor icons. To get editor icons simply create EditorIcons asset.</br>
Based on <a href="https://github.com/halak/unity-editor-icons">halak/unity-editor-icons<a> 
