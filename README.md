# Unity-FileSync
Synchronize files from outside your Unity project. Useful for models, images, or other assets you might save in external directories that need to be copied into your project. This will copy those files for you. Configuration includes multiple directories, and file-type exclusions if you wish to export with the maser files but don't wish to include the master file in the project.

## Installation
Include the Editor folder with AutoSync.cs somewhere in your Assets.

## Setup
Edit AutoSync.cs
- You'll need to assign input and output paths in the script, right at the top under "Constants."
> Input is the system directory.

> Output is relative to your Assets folder.

- Exclusions are for additional configuration if you'd like any file-types not to be copied over.

## Editor's Note
Your workflow can probably be optimized to export directly into your project, but sometimes that requires plugins for software that might not exist or doesn't include the functionality you're looking for.

For example, Blender has the plugin, [FBX Bundle](https://bitbucket.org/renderhjs/blender-addon-fbx-bundle), which makes it convenient to export directly into your project. However, it does not support armatures (as far as I know), so using this for those cases might be easier than exporting to your Unity project directory since Blender doesn't remember the export location across instances (unlike FBX Bundle).