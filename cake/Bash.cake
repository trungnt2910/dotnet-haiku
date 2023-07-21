using System;
using Pr = System.Diagnostics.Process;
using PSI = System.Diagnostics.ProcessStartInfo;

class Bash
{
    public static void RunCommand(string command, string workingDirectory = null)
    {
        var proc = Pr.Start(new PSI()
        {
            FileName = "bash",
            Arguments = $"-c \"{command}\"",
            WorkingDirectory = workingDirectory
        });

        proc.WaitForExit();

        if (proc.ExitCode != 0)
        {
            throw new Exception($"Command '{command}' failed with exit code {proc.ExitCode}");
        }
    }
}
