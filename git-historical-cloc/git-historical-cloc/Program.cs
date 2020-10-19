using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using LibGit2Sharp;
using ShellProgressBar;

namespace GitHistoricalCloc
{
    using CommitCounts = ImmutableDictionary<string, string>;
    sealed class CommitInfo
    {
        public DateTimeOffset When { get; }
        public string Sha { get; }
        public CommitCounts Counts { get; }

        public CommitInfo(DateTimeOffset when, string sha, CommitCounts counts)
        {
            When = when;
            Sha = sha;
            Counts = counts;
        }
    }

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

            OutputResults(clocHistory);
        }

        private static void ShowHelpAndExit()
        {
            Console.Out.WriteLine("Expected one argument - a path to a git repository");
            Environment.Exit(1);
        }

        private static List<CommitInfo> CaptureHistory(string repoPath, string branch)
        {
            const string clocBranchName = "git-historical-cloc";

            using var repo = new Repository(repoPath);

            var targetBranch = repo.Branches[branch];
            var targetCommits = targetBranch.Commits;

            var clocBranch = repo.Branches[clocBranchName];
            if (clocBranch == null)
            {
                clocBranch = repo.CreateBranch(clocBranchName);
            }

            Commands.Checkout(repo, clocBranch);
            repo.RemoveUntrackedFiles();

            var historicalData = new List<CommitInfo>();

            var commitCount = targetCommits.Count();

            using (var progressBar = new ProgressBar(commitCount, "Examining commits..."))
            {
                foreach (var commit in targetCommits)
                {
                    repo.Reset(ResetMode.Hard, commit);
                    var commitCounts = RunCloc(repoPath);
                    historicalData.Add(new CommitInfo(commit.Committer.When, commit.Sha, commitCounts));

                    progressBar.Tick();
                }
            }

            Commands.Checkout(repo, targetBranch);

            repo.Branches.Remove(clocBranchName);

            return historicalData;
        }

        private static CommitCounts RunCloc(string path)
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
                var language = columns[1].Trim();
                var total = columns[4].Trim();

                if (language != "SUM")
                {
                    languageToLines[language] = total;
                }
            }

            return languageToLines.ToImmutableDictionary();
        }

        private static void OutputResults(List<CommitInfo> history)
        {
            string CsvLine(IEnumerable<string> columns) =>
                string.Join(',', columns.Select(col => $"\"{col.Replace("\"", "\"\"\"")}\""));

            var allLanguages = new HashSet<string>(history.SelectMany(pair => pair.Counts.Keys));

            var outputPath = "results.csv";

            using var outputFile = new FileStream(outputPath, FileMode.Create);
            using var writer = new StreamWriter(outputFile);

            void AddRow(IEnumerable<string> columns) => writer.WriteLine(CsvLine(columns));

            AddRow(new[] { "Sha", "Date" }.Concat(allLanguages));

            history.Reverse();

            foreach (var commitInfo in history)
            {
                AddRow(
                    new[] { commitInfo.Sha, commitInfo.When.DateTime.ToString("s").Replace('T', ' ') }
                        .Concat(allLanguages.Select(name => commitInfo.Counts.TryGetValue(name, out var realCount) ? realCount : "0")));
            }

            Console.Out.WriteLine($"Wrote results to {outputPath}");
        }
    }
}
