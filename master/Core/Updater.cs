using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TTG_Tools
{
    internal static class Updater
    {
        private const string GithubOwner = "HeitorSpectre";
        private const string GithubRepo = "TTG-Tools";
        private const string LatestReleaseApi = "https://api.github.com/repos/" + GithubOwner + "/" + GithubRepo + "/releases/latest";
        private const string ReleasesPageUrl = "https://github.com/" + GithubOwner + "/" + GithubRepo + "/releases";

        private class ReleaseInfo
        {
            public string Version;
            public string DownloadUrl;
            public string HtmlUrl;
        }

        public static void CheckForUpdatesAsync(Form owner)
        {
            Task.Run(() =>
            {
                try
                {
                    ReleaseInfo release = GetLatestRelease();
                    if (release == null)
                        return;

                    Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                    Version latestVersion;

                    if (!TryParseVersion(release.Version, out latestVersion) || latestVersion <= currentVersion)
                        return;

                    owner.BeginInvoke((Action)(() => PromptAndUpdate(owner, release, currentVersion, latestVersion)));
                }
                catch
                {
                    // Update checks should not break the app startup.
                }
            });
        }

        private static void PromptAndUpdate(Form owner, ReleaseInfo release, Version currentVersion, Version latestVersion)
        {
            string message = string.Format(
                "A new TTG Tools version is available.\n\nCurrent version: {0}\nLatest version: {1}\n\nDo you want to update now?",
                currentVersion,
                latestVersion);

            DialogResult result = MessageBox.Show(owner, message, "Update available", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (result != DialogResult.Yes)
                return;

            try
            {
                DownloadAndInstallUpdate(owner, release);
            }
            catch (Exception ex)
            {
                DialogResult openBrowser = MessageBox.Show(
                    owner,
                    "Automatic update failed:\n" + ex.Message + "\n\nOpen releases page in browser?",
                    "Updater",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (openBrowser == DialogResult.Yes)
                {
                    Process.Start(release.HtmlUrl ?? ReleasesPageUrl);
                }
            }
        }

        private static void DownloadAndInstallUpdate(Form owner, ReleaseInfo release)
        {
            if (string.IsNullOrEmpty(release.DownloadUrl))
            {
                throw new InvalidOperationException("No supported downloadable asset (.zip/.exe) found in the latest GitHub release.");
            }

            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            string extension = Path.GetExtension(release.DownloadUrl).ToLowerInvariant();
            string downloadFile = Path.Combine(Path.GetTempPath(), "TTGToolsUpdate_" + Guid.NewGuid().ToString("N") + extension);

            using (WebClient client = new WebClient())
            {
                client.Headers[HttpRequestHeader.UserAgent] = "TTG-Tools-Updater";
                client.DownloadFile(release.DownloadUrl, downloadFile);
            }

            if (extension == ".exe")
            {
                Process.Start(downloadFile);
                MessageBox.Show(owner, "The updater executable was launched. TTG Tools will close now.", "Updater", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Exit();
                return;
            }

            if (extension != ".zip")
            {
                throw new InvalidOperationException("Unsupported update package format: " + extension);
            }

            string extractDir = Path.Combine(Path.GetTempPath(), "TTGToolsExtract_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(extractDir);
            ZipFile.ExtractToDirectory(downloadFile, extractDir);

            string appPath = Application.ExecutablePath;
            string appDir = Path.GetDirectoryName(appPath);
            string appExeName = Path.GetFileName(appPath);

            string extractedExePath = Path.Combine(extractDir, appExeName);
            if (!File.Exists(extractedExePath))
            {
                string[] exes = Directory.GetFiles(extractDir, "*.exe", SearchOption.AllDirectories);
                if (exes.Length == 0)
                    throw new InvalidOperationException("Downloaded package does not contain an executable file.");

                extractedExePath = exes[0];
            }

            int currentPid = Process.GetCurrentProcess().Id;
            string scriptPath = Path.Combine(Path.GetTempPath(), "TTGToolsUpdater_" + Guid.NewGuid().ToString("N") + ".cmd");
            string script = string.Join("\r\n", new[]
            {
                "@echo off",
                "setlocal",
                ":waitloop",
                string.Format("tasklist /FI \"PID eq {0}\" | find \"{0}\" >nul", currentPid),
                "if not errorlevel 1 (",
                "  timeout /t 1 /nobreak >nul",
                "  goto waitloop",
                ")",
                string.Format("xcopy \"{0}\\*\" \"{1}\\\" /E /Y /I >nul", extractDir, appDir),
                string.Format("start \"\" \"{0}\"", Path.Combine(appDir, Path.GetFileName(extractedExePath))),
                string.Format("rmdir /S /Q \"{0}\"", extractDir),
                string.Format("del /Q \"{0}\"", downloadFile),
                "del /Q \"%~f0\""
            });

            File.WriteAllText(scriptPath, script);
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/C \"" + scriptPath + "\"",
                CreateNoWindow = true,
                UseShellExecute = false
            });

            MessageBox.Show(owner, "Update downloaded. TTG Tools will close and restart automatically.", "Updater", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Application.Exit();
        }

        private static ReleaseInfo GetLatestRelease()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            string json;
            using (WebClient client = new WebClient())
            {
                client.Headers[HttpRequestHeader.UserAgent] = "TTG-Tools-Updater";
                json = client.DownloadString(LatestReleaseApi);
            }

            if (string.IsNullOrEmpty(json))
                return null;

            ReleaseInfo release = new ReleaseInfo
            {
                Version = ReadJsonString(json, "tag_name"),
                DownloadUrl = SelectDownloadUrl(json),
                HtmlUrl = ReadJsonString(json, "html_url")
            };

            if (string.IsNullOrEmpty(release.Version))
                return null;

            return release;
        }

        private static string SelectDownloadUrl(string json)
        {
            MatchCollection matches = Regex.Matches(json, "\\\"browser_download_url\\\"\\s*:\\s*\\\"([^\\\"]+)\\\"");
            if (matches.Count == 0)
                return null;

            List<string> urls = new List<string>();
            foreach (Match match in matches)
            {
                urls.Add(JsonUnescape(match.Groups[1].Value));
            }

            string zip = urls.Find(u => u.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(zip))
                return zip;

            string exe = urls.Find(u => u.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(exe))
                return exe;

            return null;
        }

        private static string ReadJsonString(string json, string key)
        {
            Match match = Regex.Match(json, "\\\"" + Regex.Escape(key) + "\\\"\\s*:\\s*\\\"([^\\\"]*)\\\"");
            return match.Success ? JsonUnescape(match.Groups[1].Value) : string.Empty;
        }

        private static string JsonUnescape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            string normalized = value.Replace("\\/", "/");
            return Regex.Unescape(normalized);
        }

        private static bool TryParseVersion(string rawVersion, out Version version)
        {
            version = null;
            if (string.IsNullOrEmpty(rawVersion))
                return false;

            string normalized = rawVersion.Trim();
            normalized = Regex.Replace(normalized, "^[^0-9]+", string.Empty);

            int suffixStart = normalized.IndexOf('-');
            if (suffixStart >= 0)
                normalized = normalized.Substring(0, suffixStart);

            return Version.TryParse(normalized, out version);
        }
    }
}
