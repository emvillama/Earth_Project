#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

// Two Xcode-15+ defaults break stock Unity iOS builds; fix them on the generated project every
// build so we never have to toggle them by hand:
//   • ENABLE_USER_SCRIPT_SANDBOXING = YES  → sandboxes build scripts, so IL2CPP fails with
//     "Sandbox: deny(1) file-read-data … il2cpp" on the GameAssembly target.
//   • ENABLE_MODULE_VERIFIER = YES  → the new Module Verifier compiles UnityFramework as a
//     standalone Clang module (a synthetic "Test" module) and fails, because Unity's ObjC headers
//     aren't module-clean → "could not build module 'Test'" + a cascade of umbrella-header errors.
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
            proj.SetBuildProperty(guid, "ENABLE_MODULE_VERIFIER", "NO");
        }

        proj.WriteToFile(projPath);
    }
}
#endif
