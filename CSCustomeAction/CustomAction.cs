using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using WixToolset.Dtf.WindowsInstaller;

namespace CustomeAction
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult RegisterMonitor(Session session)
        {
            CustomActionData customActionData = session.CustomActionData;
            RegisterDLL(Path.Combine(customActionData["INSTALLFOLDER"], "DWDWeatherBand.dll"));
            return ActionResult.Success;
        }

        [CustomAction]
        public static ActionResult UnregisterMonitor(Session session)
        {
            CustomActionData customActionData = session.CustomActionData;
            RegisterDLL(Path.Combine(customActionData["INSTALLFOLDER"], "DWDWeatherBand.dll"), true);
            return ActionResult.Success;
        }

        private static bool RegisterDLL(string target, bool unregister = false)
        {
            string args = unregister ? "/unregister" : "/nologo /codebase";
            var regAsmPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), @"Microsoft.NET\Framework64\v4.0.30319\regasm.exe");
            string output = RunProgram(regAsmPath, $@"{args} ""{target}""");
        #if DEBUG
            MessageBox.Show(
                target + "\n" + output,
                "Output",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        #endif
            return true;
        }

        private static string RunProgram(string path, string args, bool wait = true)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = path;
                process.StartInfo.Arguments = args;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                if (wait)
                {
                    process.WaitForExit();
                    string output = process.StandardOutput.ReadToEnd();
                    return output;
                }

                return null;
            }
        }
    }
}
