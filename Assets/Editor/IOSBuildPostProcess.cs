#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

// Xcode 15+ defaults ENABLE_USER_SCRIPT_SANDBOXING = YES, which sandboxes build-phase scripts and
// makes Unity's IL2CPP step fail with "Sandbox: deny(1) file-read-data … il2cpp" on the GameAssembly
// target. Turn it off on the generated project every build so iOS builds don't need a manual Xcode
// toggle each time.
public static class IOSBuildPostProcess
{
    [PostProcessBuild(999)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS) return;

        string projPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        var proj = new PBXProject();
        proj.ReadFromFile(projPath);

        foreach (var guid in new[]
        {
            proj.ProjectGuid(),
            proj.GetUnityMainTargetGuid(),
            proj.GetUnityFrameworkTargetGuid(),
        })
        {
            proj.SetBuildProperty(guid, "ENABLE_USER_SCRIPT_SANDBOXING", "NO");
        }

        proj.WriteToFile(projPath);
    }
}
#endif
