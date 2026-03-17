using System;
using Godot;

public partial class DebugQuitter : Node {
    public override void _Ready() {
        base._Ready();
        GetTree().Root.AddChild(this);
    }

    public void Quit() {
        GetTree().Quit();
    }
}

internal static class Debug {
    private static DebugQuitter quitter = null;

    /// <summary>
    /// Assert that cond is true, otherwise crash and print msg
    /// </summary>
    internal static void Assert(bool cond, string msg) {
        if (cond) return;

        GD.PrintErr(msg);
#if DEBUG
        // Create quitter if it dosen't already exist
        if (quitter != null) {
            quitter = new DebugQuitter();
        }

        quitter.Quit();
        throw new ApplicationException($"Assert Failed: {msg}");
#endif
    }
}
