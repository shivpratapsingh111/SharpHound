using System;
using System.IO;
using Microsoft.Win32;
using Sharphound.Client;
using SharpHoundCommonLib;

namespace Sharphound.Runtime
{
    public class ClientHelpers
    {
        private static readonly string ProcStartTime = $"{DateTime.Now:yyyyMMddHHmmss}";

        internal static string GetLoopFileName(IContext context)
        {
            var finalFilename =
                context.ZipFilename == null ? "BloodHoundLoopResults.zip" : $"{context.ZipFilename}.zip";

            if (context.Flags.RandomizeFilenames) finalFilename = $"{Path.GetRandomFileName()}.zip";

            finalFilename = $"{ProcStartTime}_{finalFilename}";

            if (context.OutputPrefix != null) finalFilename = $"{context.OutputPrefix}_{finalFilename}";

            var finalPath = Path.Combine(context.OutputDirectory, finalFilename);

            return finalPath;
        }

        internal static string GetBase64MachineID()
        {
            try
            {
                using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                {
                    var crypto = key.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography", false);
                    if (crypto == null) return $"{Helpers.Base64(Environment.MachineName)}";

                    var guid = crypto.GetValue("MachineGuid") as string;
                    return Helpers.Base64(guid);
                }
            }
            catch
            {
                return $"{Helpers.Base64(Environment.MachineName)}";
            }
        }
    }
}