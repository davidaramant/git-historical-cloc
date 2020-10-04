using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace GitHistoricalCloc
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                ShowHelpAndExit();
            }

            var repoPath = Path.GetFullPath(args[0]);

            if (!Directory.Exists(repoPath))
            {
                ShowHelpAndExit();
            }

            var clocHistory = CaptureHistory(repoPath, branch: "master");
        }

        private static void ShowHelpAndExit()
        {
            Console.Out.WriteLine("Expected one argument - a path to a git repository");
            Environment.Exit(1);
        }

        private static object CaptureHistory(string repoPath, string branch)
        {
            var result = RunCloc(repoPath);

            foreach (var pair in result)
            {
                Console.Out.WriteLine($"{pair.Key}: {pair.Value}");
            }

            return null;
        }

        private static ImmutableDictionary<string, string> RunCloc(string path)
        {
            var clocInfo = new ProcessStartInfo
            {
                FileName = "cloc",
                Arguments = $"--csv {path}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };

            using var p = Process.Start(clocInfo);

            var totalOutput = p.StandardOutput.ReadToEnd();

            p.WaitForExit();

            var languageToLines = new Dictionary<string, string>();

            var resultLines =
                totalOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .SkipWhile(l => !l.StartsWith("files"))
                    .Skip(1);

            foreach (var line in resultLines)
            {
                var columns = line.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var language = columns[1];
                var total = columns[4];

                if (language != "SUM")
                {
                    languageToLines[language] = total;
                }
            }

            return languageToLines.ToImmutableDictionary();
        }


    }
}
