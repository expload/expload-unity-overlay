using System.IO;
using System.Linq;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Expload.UnityEditor
{
    public class BuildPostProcessor
    {
        const string CefWindowsPluginDirectory = "/Cef/Windows";
        const string CefMacOsPluginDirectory = "/Cef/MacOS";
        const string CefGluePluginDirectory = "/Cefglue";

        [PostProcessBuild(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            // Only Windows x86 and x86_64 atm.
            if (target != BuildTarget.StandaloneOSX && target != BuildTarget.StandaloneWindows64)
                return;

            ////////////////////////////////////////////////////////////////
            // COPY EVERY FILE IN THE PLUGIN DIRECTORY TO THE BUILD PATH. //
            ////////////////////////////////////////////////////////////////

            // Get the source directory (Assets/Plugins or Assets/Plugins/x86 or Assets/Plugins/x86_64).
            string srcPluginsFolderPrefix = string.Format("{0}/{1}", Application.dataPath, "Expload/Plugins");

            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                    PrepareWindows(pathToBuiltProject, srcPluginsFolderPrefix);
                    break;

                case BuildTarget.StandaloneOSX:
                    PrepareMacOs(pathToBuiltProject, srcPluginsFolderPrefix);
                    break;

                default:
                    throw new System.Exception("Unsupported paltform" + BuildTarget.StandaloneWindows);
            }
        }

        private static void PrepareMacOs(string pathToBuiltProject, string srcPluginsFolderPrefix)
        {
            string srcPluginsFolder = srcPluginsFolderPrefix + CefMacOsPluginDirectory;
            string libcefPathDest = pathToBuiltProject + "/Contents/Frameworks/MonoEmbedRuntime/osx/libcef";

            // Create /Contents/Frameworks/MonoEmbedRuntime/osx/ if not exists
            FileInfo libcefFile = new FileInfo(libcefPathDest);
            if (!libcefFile.Directory.Exists)
                libcefFile.Directory.Create();

            // Copy stared library
            File.Copy(srcPluginsFolder + "/libcef", libcefPathDest, true);

            DirectoryCopy(
              srcPluginsFolder + "/Chromium Embedded Framework.framework",
              pathToBuiltProject + "/Contents/Frameworks/Chromium Embedded Framework.framework",
              true
            );
        }

        private static void PrepareWindows(string pathToBuiltProject, string srcPluginsFolderPrefix)
        {
            string srcPluginsFolder = srcPluginsFolderPrefix + CefWindowsPluginDirectory;

            if (!Directory.Exists(srcPluginsFolder))
                throw new DirectoryNotFoundException(srcPluginsFolder + " not found!");

            // Debug.Log("pathToBuiltProject = " + pathToBuiltProject);

            // Get the destination directory (<BUILT_EXE_PATH>/<EXE_NAME>_Data/Plugins).
            int splitIndex = pathToBuiltProject.LastIndexOf('/');
            string buildPath = pathToBuiltProject.Substring(0, splitIndex);
            string executableName = pathToBuiltProject.Substring(splitIndex + 1);
            string buildPluginsPath = string.Format("{0}/{1}_Data/Plugins", buildPath, Path.GetFileNameWithoutExtension(executableName));

            DirectoryInfo srcPluginsFolderInfo = new DirectoryInfo(srcPluginsFolder);
            DirectoryInfo buildPluginsPathInfo = new DirectoryInfo(buildPluginsPath);

            // Exclude some files (.dlls should already be there!)
            var srcPluginsFolderFiles = srcPluginsFolderInfo.GetFiles("*.*", SearchOption.TopDirectoryOnly)
                                                            .Where(fi => !fi.Name.EndsWith(".meta") && !fi.Name.EndsWith(".dll") &&
                                                                         !fi.Name.EndsWith(".pdb") && !fi.Name.EndsWith(".lib") &&
                                                                         !fi.Name.EndsWith(".mdb") && !fi.Name.EndsWith(".xml") &&
                                                                         !fi.Name.EndsWith(".DS_Store"));

            var srcPluginsFolderDirectories = srcPluginsFolderInfo.GetDirectories();

            // Copy selected files and sub-directories.
            foreach (var dir in srcPluginsFolderDirectories)
            {
                Debug.Log("Copy " + dir.FullName + " to " + string.Format("{0}/{1}", buildPluginsPathInfo.FullName, dir.Name));
                DirectoryCopy(dir.FullName, string.Format("{0}/{1}", buildPluginsPathInfo.FullName, dir.Name), true);
            }
            foreach (var file in srcPluginsFolderFiles)
            {
                Debug.Log("Copy " + file.FullName + " to " + string.Format("{0}/{1}", buildPluginsPathInfo.FullName, file.Name));
                File.Copy(file.FullName, string.Format("{0}/{1}", buildPluginsPathInfo.FullName, file.Name), false);
            }
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);

            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
                Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}