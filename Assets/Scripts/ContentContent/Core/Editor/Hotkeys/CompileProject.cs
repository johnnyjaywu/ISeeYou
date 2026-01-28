using UnityEditor;
using UnityEditor.Compilation;

namespace ContentContent.Editor
{
    public static class CompileProject
    {
        [MenuItem("File/Compile _F5")]
        private static void Compile()
        {
            CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.None);
        }

        // Ctrl + Shift + F5
        [MenuItem("File/Compile and Clean Build Cache %#F5")]
        private static void CompileCleanBuildCache()
        {
            CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
        }
    }
}