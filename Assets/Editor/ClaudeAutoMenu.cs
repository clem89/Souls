using UnityEditor;
using UnityEngine;
using System.Diagnostics;

public static class ClaudeAutoMenu
{
    [MenuItem("Tools/Claude Auto")]
    public static void OpenClaudeAuto()
    {
        string projectPath = Application.dataPath.Replace("/Assets", "");

        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/k \"cd /d \"{projectPath}\" && claude --dangerously-skip-permissions\"",
            UseShellExecute = true,
        };

        Process.Start(psi);
    }
}
