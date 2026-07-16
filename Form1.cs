using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Web.WebView2.Core;
using HtmlAgilityPack;

namespace TikTok_Downloader
{
    public partial class Form1 : Form
    {
        private CancellationTokenSource cancellationTokenSource;
        private string html;
        private string lastDownloadedFolderPath;

        private readonly Size _size1 = new Size(745, 1018);
        private readonly Size _size2 = new Size(1770, 1018);
        private bool _isSize1 = true;

        public Form1()
        {
            Console.WriteLine("Constructor: Initializing Form1...");
            InitializeComponent();
            Console.WriteLine("Constructor: Form1 initialization complete.");
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            Console.WriteLine("Form1_Load: Starting load event.");

            userTXT.Text = Properties.Settings.Default.lastUser;
            lastDownloadedFolderPath = Properties.Settings.Default.lastFolder;
            thumbCHK.Checked = Properties.Settings.Default.lastThumb;

            // Ensure WebView2 is initialized
            Console.WriteLine("Form1_Load: Ensuring WebView2 is initialized...");
            await webBrowser.EnsureCoreWebView2Async();
            if (webBrowser.CoreWebView2 != null)
            {
                webBrowser.CoreWebView2.Settings.AreDevToolsEnabled = false;
                webBrowser.CoreWebView2.Settings.IsStatusBarEnabled = false;
            }

            Console.WriteLine("Form1_Load: Load event complete.");
        }

        // Helper to wait for next page NavigationCompleted
        private Task WaitForPageLoadAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            void handler(object s, CoreWebView2NavigationCompletedEventArgs e)
            {
                webBrowser.NavigationCompleted -= handler;
                tcs.SetResult(true);
            }
            webBrowser.NavigationCompleted += handler;
            return tcs.Task;
        }

        // Retrieve HTML from WebView2
        private async Task<string> GetHtmlSourceAsync()
        {
            var resultJson = await webBrowser.CoreWebView2.ExecuteScriptAsync("document.documentElement.outerHTML");
            var unescaped = Regex.Unescape(resultJson);

            if (unescaped.StartsWith("\"") && unescaped.EndsWith("\"") && unescaped.Length > 1)
                unescaped = unescaped.Substring(1, unescaped.Length - 2);

            return unescaped;
        }

        // Evaluate script returning integer
        private async Task<int> EvaluateIntAsync(string script)
        {
            var resultJson = await webBrowser.CoreWebView2.ExecuteScriptAsync(script);
            resultJson = resultJson.Trim('"');
            return int.TryParse(resultJson, out int parsed) ? parsed : 0;
        }

        // Load a profile page and optionally scroll
        private async void LoadUserProfilePage(string selection)
        {
            if (webBrowser.CoreWebView2 == null) return;

            var targetUrl = "https://www.tiktok.com/@" + userTXT.Text;
            webBrowser.CoreWebView2.Navigate(targetUrl);
            statusTXT.Text = "Navigating to: " + targetUrl;
            progressBar.Value = 20;

            await WaitForPageLoadAsync();
            await Task.Delay(2000);

            // Small scroll
            await webBrowser.CoreWebView2.ExecuteScriptAsync("window.scrollTo(0, 1);");
            await Task.Delay(200);
            await webBrowser.CoreWebView2.ExecuteScriptAsync("window.scrollTo(0, -1);");
            await Task.Delay(200);

            if (selection == "all")
            {
                statusTXT.Text = "Auto scrolling to bottom of page for all videos";
                while (true)
                {
                    int currentPosition = await EvaluateIntAsync("window.scrollY");
                    int documentHeight = await EvaluateIntAsync("document.body.scrollHeight");
                    await webBrowser.CoreWebView2.ExecuteScriptAsync("window.scrollTo(0, document.body.scrollHeight);");
                    await Task.Delay(1000);

                    int newPosition = await EvaluateIntAsync("window.scrollY");
                    int newDocumentHeight = await EvaluateIntAsync("document.body.scrollHeight");

                    if (newPosition == currentPosition || newDocumentHeight == documentHeight)
                        break;
                }
                statusTXT.Text = "Page loaded fully";
                progressBar.Value = 60;
            }

            // Grab HTML
            html = await GetHtmlSourceAsync();
            LoadVideos(html);
            statusTXT.Text = "Videos loaded successfully";
            progressBar.Value = 100;
        }

        // Parse HTML to find TikTok videos
        private void LoadVideos(string html)
        {
            if (html == null)
            {
                statusTXT.Text = "Error: could not scrape videos";
                MessageBox.Show("Error: could not load profile, check your connection");
                progressBar.Value = 100;
                return;
            }

            videoList.Rows.Clear();
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            var videoItems = doc.DocumentNode.SelectNodes(
                // First part: handle single video blocks
                "//div[@data-e2e='user-post-item' or @data-e2e='user-repost-item' or @data-e2e='favorites-item' or @data-e2e='user-liked-item']"

                + " | " // union

                // Second part: handle parent “-list” containers
                + "//div[@data-e2e='user-repost-item-list' or @data-e2e='favorites-item-list' or @data-e2e='user-liked-item-list']"
                + "//div[@data-e2e='user-post-item' or @data-e2e='user-repost-item' or @data-e2e='favorites-item' or @data-e2e='user-liked-item']"
            );

            if (videoItems == null)
            {
                statusTXT.Text = "Error: no videos found";
                MessageBox.Show("Error: no videos found, check username");
                progressBar.Value = 100;
                return;
            }

            int totalVideoCount = videoItems.Count;
            int processedVideoCount = 0;

            foreach (var videoItem in videoItems)
            {
                var videoUrl = videoItem.Descendants("a").FirstOrDefault()?.GetAttributeValue("href", "");
                if (string.IsNullOrEmpty(videoUrl)) continue;

                string pattern = @"\/video\/(\d+)";
                var videoID = Regex.Match(videoUrl, pattern).Groups[1].Value;
                var videoDesc = videoItem.Descendants("img").FirstOrDefault()?.GetAttributeValue("alt", "");
                var videoStats = videoItem.Descendants("strong")
                    .FirstOrDefault(div => div.GetAttributeValue("data-e2e", "") == "video-views")
                    ?.InnerText;

                var newRow = new DataGridViewRow();
                newRow.CreateCells(videoList);

                // Fill columns
                newRow.Cells[0].Value = videoID;
                newRow.Cells[1].Value = videoDesc;
                newRow.Cells[2].Value = videoStats;
                newRow.Cells[3].Value = videoUrl;

                var downloadButton = new DataGridViewButtonCell { Value = "Download" };
                newRow.Cells[4] = downloadButton;

                videoList.Rows.Add(newRow);

                processedVideoCount++;
                double progressPercentage = (double)processedVideoCount / totalVideoCount * 100;
                progressBar.Value = (int)progressPercentage;
                statusTXT.Text = $"Processed {processedVideoCount} of {totalVideoCount}";

                // Make URL cell clickable
                var urlCell = newRow.Cells[3];
                urlCell.Style.ForeColor = Color.Blue;
                urlCell.Style.Font = new Font(urlCell.InheritedStyle.Font, FontStyle.Underline);
                urlCell.Tag = videoUrl;
                urlCell.ToolTipText = "Click to open in browser";
                urlCell.ReadOnly = true;

                // Tag the download button
                downloadButton.Tag = videoUrl;
                downloadButton.UseColumnTextForButtonValue = false;
            }

            progressBar.Value = 0;
        }

        // Loads "ALL videos"
        // (Not tied to a button in designer by default)
        private void LoadBTN_Click(object sender, EventArgs e)
        {
            Console.WriteLine("LoadBTN_Click: Loading ALL videos...");
            LoadUserProfilePage("all");
        }

        // Clicking a URL cell => open in default browser
        private void videoList_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 3 && e.RowIndex >= 0)
            {
                var videoUrl = videoList.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();
                if (!string.IsNullOrEmpty(videoUrl))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = videoUrl,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error opening URL: " + ex.Message);
                    }
                }
            }
        }

        // Click download button
        private void videoList_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 4 && e.RowIndex >= 0)
            {
                var videoUrl = videoList.Rows[e.RowIndex].Cells[e.ColumnIndex].Tag?.ToString();
                if (!string.IsNullOrEmpty(videoUrl))
                {
                    statusTXT.Text = "Downloading video...";
                    progressBar.Value = 0;
                    DownloadTikTokVideoAsync(videoList.Rows[e.RowIndex].Cells[3].Value?.ToString(), e.RowIndex);
                    progressBar.Value = 100;
                }
            }
        }

        // Toggle form size
        private void expandBTN_Click(object sender, EventArgs e)
        {
            _isSize1 = !_isSize1;
            this.Size = _isSize1 ? _size1 : _size2;
            expandBTN.Text = _isSize1 ? "Expand ⮞" : "Shrink ⮜";
        }

        // Single-video download logic
        private async void DownloadTikTokVideoAsync(string tikTokVideoUrl, int videoURLrow)
        {
            if (string.IsNullOrWhiteSpace(tikTokVideoUrl))
            {
                statusTXT.Text = "The TikTok video URL is blank.";
                progressBar.Value = 0;
                return;
            }

            // Extract ID
            string videoID = "TikTokVideo";
            var idMatch = Regex.Match(tikTokVideoUrl, @"/(?:video|v)/(\d+)");
            if (idMatch.Success) videoID = idMatch.Groups[1].Value;

            // Navigate SnapTik
            statusTXT.Text = "Navigating to SnapTik...";
            webBrowser.CoreWebView2.Navigate("https://snaptik.app");
            await WaitForPageLoadAsync();
            await Task.Delay(2000);

            // Fill input & submit
            await webBrowser.CoreWebView2.ExecuteScriptAsync($"document.getElementById('url').value = '{tikTokVideoUrl}'");
            await webBrowser.CoreWebView2.ExecuteScriptAsync("document.getElementById('url').click();");
            await webBrowser.CoreWebView2.ExecuteScriptAsync("document.querySelector('button[type=\"submit\"]').click()");
            await Task.Delay(2000);

            // Wait for link
            bool foundLink = false;
            for (int i = 0; i < 30; i++)
            {
                var checkSelector = await webBrowser.CoreWebView2.ExecuteScriptAsync(
                    "document.querySelector('.video-links a.button.download-file') !== null"
                );
                if (checkSelector.Contains("true"))
                {
                    foundLink = true;
                    break;
                }
                await Task.Delay(1000);
            }
            if (!foundLink)
            {
                statusTXT.Text = "Could not find SnapTik download link.";
                return;
            }

            // Extract final video link
            var hrefJson = await webBrowser.CoreWebView2.ExecuteScriptAsync(
               "document.querySelector('.video-links a.button.download-file').getAttribute('href')"
            );
            string rawUrl = Regex.Unescape(hrefJson);
            if (rawUrl.StartsWith("\"") && rawUrl.EndsWith("\"") && rawUrl.Length > 1)
                rawUrl = rawUrl.Substring(1, rawUrl.Length - 2);

            // Extract thumbnail
            var thumbJson = await webBrowser.CoreWebView2.ExecuteScriptAsync(
               "document.querySelector('img#thumbnail') ? document.querySelector('img#thumbnail').getAttribute('src') : ''"
            );
            string thumbUrl = Regex.Unescape(thumbJson);
            if (thumbUrl.StartsWith("\"") && thumbUrl.EndsWith("\"") && thumbUrl.Length > 1)
                thumbUrl = thumbUrl.Substring(1, thumbUrl.Length - 2);

            // Prompt for final name
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "MP4 Files|*.mp4",
                FileName = $"{videoID}.mp4",
                InitialDirectory = lastDownloadedFolderPath
            };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                lastDownloadedFolderPath = Path.GetDirectoryName(sfd.FileName);
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        var resp = await client.GetAsync(rawUrl);
                        if (resp.IsSuccessStatusCode)
                        {
                            using (var sourceStream = await resp.Content.ReadAsStreamAsync())
                            using (var fileStream = new FileStream(sfd.FileName, FileMode.Create))
                            {
                                await sourceStream.CopyToAsync(fileStream);
                            }
                            videoList.Rows[videoURLrow].DefaultCellStyle.BackColor = Color.LightGreen;
                            statusTXT.Text = $"Video saved to: {sfd.FileName}";
                        }
                        else
                        {
                            statusTXT.Text = $"Failed to download. HTTP code {resp.StatusCode}";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error downloading: " + ex.Message);
                    statusTXT.Text = "Error downloading video.";
                }

                // If user wants thumbnails
                if (thumbCHK.Checked && !string.IsNullOrWhiteSpace(thumbUrl))
                {
                    string thumbPath = Path.Combine(Path.GetDirectoryName(sfd.FileName), $"{videoID}.jpg");
                    try
                    {
                        await DownloadThumbnailAsync(thumbUrl, thumbPath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error downloading thumbnail => " + ex.Message);
                    }
                }
            }
        }

        // Download thumbnail
        private async Task DownloadThumbnailAsync(string thumbnailUrl, string savePath)
        {
            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync(thumbnailUrl);
                using (var streamToReadFrom = await response.Content.ReadAsStreamAsync())
                using (var streamToWriteTo = File.Open(savePath, FileMode.Create))
                {
                    await streamToReadFrom.CopyToAsync(streamToWriteTo);
                }
            }
        }

        // Bulk download
        private async void DownloadAllTikTokVideosAsync()
        {
            cancellationTokenSource = new CancellationTokenSource();
            var folderBrowserDialog = new FolderBrowserDialog { SelectedPath = lastDownloadedFolderPath };

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                lastDownloadedFolderPath = folderBrowserDialog.SelectedPath;
                stopBTN.Enabled = true;

                foreach (DataGridViewRow row in videoList.Rows)
                {
                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        stopBTN.Enabled = false;
                        break;
                    }

                    string tikTokVideoUrl = row.Cells[3].Value?.ToString();
                    if (!string.IsNullOrWhiteSpace(tikTokVideoUrl))
                    {
                        string videoId = "TikTokVideo";
                        var m = Regex.Match(tikTokVideoUrl, @"(?<=video/)\d+");
                        if (m.Success) videoId = m.Value;

                        string fileName = $"{videoId}.mp4";
                        string filePath = Path.Combine(folderBrowserDialog.SelectedPath, fileName);
                        if (File.Exists(filePath))
                        {
                            row.DefaultCellStyle.BackColor = Color.LightGreen;
                            continue;
                        }

                        statusTXT.Text = $"Downloading {tikTokVideoUrl}...";
                        progressBar.Value = 0;

                        // SnapTik
                        webBrowser.CoreWebView2.Navigate("https://snaptik.app");
                        await WaitForPageLoadAsync();
                        await Task.Delay(2000);
                        if (cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            stopBTN.Enabled = false;
                            break;
                        }

                        await webBrowser.CoreWebView2.ExecuteScriptAsync($"document.getElementById('url').value = '{tikTokVideoUrl}'");
                        await webBrowser.CoreWebView2.ExecuteScriptAsync("document.getElementById('url').click();");
                        await webBrowser.CoreWebView2.ExecuteScriptAsync("document.querySelector('button[type=\"submit\"]').click()");
                        await Task.Delay(2000);

                        if (cancellationTokenSource.Token.IsCancellationRequested)
                        {
                            stopBTN.Enabled = false;
                            break;
                        }

                        bool foundLink = false;
                        for (int i = 0; i < 30; i++)
                        {
                            var checkSelector = await webBrowser.CoreWebView2.ExecuteScriptAsync(
                               "document.querySelector('.video-links a.button.download-file') !== null"
                            );
                            if (checkSelector.Contains("true"))
                            {
                                foundLink = true;
                                break;
                            }
                            await Task.Delay(1000);
                            if (cancellationTokenSource.Token.IsCancellationRequested) break;
                        }
                        if (!foundLink)
                        {
                            statusTXT.Text = "No SnapTik download link found.";
                            continue;
                        }

                        var hrefJson = await webBrowser.CoreWebView2.ExecuteScriptAsync(
                           "document.querySelector('.video-links a.button.download-file').getAttribute('href')"
                        );
                        string rawUrl = Regex.Unescape(hrefJson);
                        if (rawUrl.StartsWith("\"") && rawUrl.EndsWith("\"") && rawUrl.Length > 1)
                            rawUrl = rawUrl.Substring(1, rawUrl.Length - 2);

                        progressBar.Value = 60;

                        // Thumbnail
                        var thumbJson = await webBrowser.CoreWebView2.ExecuteScriptAsync(
                          "document.querySelector('img#thumbnail') ? document.querySelector('img#thumbnail').getAttribute('src') : ''"
                        );
                        string thumbUrl = Regex.Unescape(thumbJson);
                        if (thumbUrl.StartsWith("\"") && thumbUrl.EndsWith("\"") && thumbUrl.Length > 1)
                            thumbUrl = thumbUrl.Substring(1, thumbUrl.Length - 2);

                        // Download video
                        try
                        {
                            using (HttpClient client = new HttpClient())
                            {
                                var response = await client.GetAsync(rawUrl);
                                if (response.IsSuccessStatusCode)
                                {
                                    using (var inStream = await response.Content.ReadAsStreamAsync())
                                    using (var outFile = File.Open(filePath, FileMode.Create))
                                    {
                                        await inStream.CopyToAsync(outFile);
                                    }
                                    statusTXT.Text = $"Video saved to {filePath}";
                                    lastDownloadedFolderPath = Path.GetDirectoryName(filePath);
                                    openFolderBTN.Enabled = true;
                                    row.DefaultCellStyle.BackColor = Color.LightGreen;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error downloading video: " + ex.Message);
                        }

                        // Download thumbnail if checked
                        if (thumbCHK.Checked && !string.IsNullOrWhiteSpace(thumbUrl))
                        {
                            string thumbPath = Path.Combine(folderBrowserDialog.SelectedPath, $"{videoId}.jpg");
                            try
                            {
                                await DownloadThumbnailAsync(thumbUrl, thumbPath);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Error downloading thumbnail => " + ex.Message);
                            }
                        }
                    }
                }

                progressBar.Value = 100;
                statusTXT.Text = "All Video Downloads complete";
                stopBTN.Enabled = false;
                MessageBox.Show("All Video Downloads complete");
            }
        }

        // Click Download All button
        private void downloadAllBTN_Click(object sender, EventArgs e)
        {
            DownloadAllTikTokVideosAsync();
        }

        // Export to CSV
        private void button1_Click(object sender, EventArgs e)
        {
            ExportToCsv(videoList);
        }

        private void ExportToCsv(DataGridView dataGridView)
        {
            var saveDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = "Tiktok_Videos_List.csv",
                InitialDirectory = lastDownloadedFolderPath
            };
            if (saveDialog.ShowDialog() == DialogResult.OK)
            {
                lastDownloadedFolderPath = Path.GetDirectoryName(saveDialog.FileName);
                progressBar.Value = 0;
                statusTXT.Text = "Exporting to CSV file...";

                try
                {
                    using (var streamWriter = new StreamWriter(saveDialog.FileName))
                    {
                        // Headers (skip download column)
                        for (int i = 0; i < dataGridView.Columns.Count; i++)
                        {
                            if (i != 4)
                            {
                                string columnHeaderText = dataGridView.Columns[i].HeaderText.Replace(",", "");
                                streamWriter.Write(columnHeaderText);
                                if (i < dataGridView.Columns.Count - 1)
                                    streamWriter.Write(",");
                            }
                        }
                        streamWriter.WriteLine();

                        // Rows
                        foreach (DataGridViewRow row in dataGridView.Rows)
                        {
                            if (!row.IsNewRow)
                            {
                                for (int i = 0; i < row.Cells.Count; i++)
                                {
                                    if (i != 4)
                                    {
                                        string cellValue = row.Cells[i].FormattedValue.ToString().Replace(",", "");
                                        // leading ' to preserve IDs with leading zeros
                                        if (i == 0) streamWriter.Write("'");
                                        streamWriter.Write(cellValue);
                                        if (i < row.Cells.Count - 1)
                                            streamWriter.Write(",");
                                    }
                                }
                                streamWriter.WriteLine();
                                progressBar.Value = row.Cells.Count;
                            }
                        }
                    }
                    MessageBox.Show($"Data exported to {saveDialog.FileName}", "Export Complete");
                    progressBar.Value = 100;
                    statusTXT.Text = $"Export complete: {saveDialog.FileName}";
                }
                catch (IOException ex)
                {
                    if (IsFileLocked(ex))
                    {
                        MessageBox.Show("The file is locked. Please close it or try a different name.", "File Locked");
                    }
                    else
                    {
                        MessageBox.Show($"Error exporting data: {ex.Message}", "Error");
                    }
                }
            }
        }

        private static bool IsFileLocked(IOException exception)
        {
            int errorCode = Marshal.GetHRForException(exception) & ((1 << 16) - 1);
            return errorCode == 32 || errorCode == 33;
        }

        private void openFolderBTN_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(lastDownloadedFolderPath) && Directory.Exists(lastDownloadedFolderPath))
            {
                Process.Start("explorer.exe", lastDownloadedFolderPath);
            }
            else
            {
                MessageBox.Show("No folder to open. Please download a video first.", "Folder Not Found");
            }
        }

        private void userTXT_TextChanged(object sender, EventArgs e)
        {
            videoList.Rows.Clear();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.lastUser = userTXT.Text;
            Properties.Settings.Default.lastFolder = lastDownloadedFolderPath;
            Properties.Settings.Default.lastThumb = thumbCHK.Checked;
            Properties.Settings.Default.Save();
        }

        private void label2_Click(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://CoolshrimpModz.com",
                UseShellExecute = true
            });
        }

        private void stopBTN_Click(object sender, EventArgs e)
        {
            cancellationTokenSource?.Cancel();
        }

        // Check if user is logged in (example usage)
    //    private async Task<bool> IsUserLoggedIn()
    //    {
    //        await WaitForPageLoadAsync();
    //        var checkLoginButton = await webBrowser.CoreWebView2.ExecuteScriptAsync(
    //            "document.querySelector('button[data-e2e=\"top-login-button\"]') !== null"
    //        );
    //        bool loginButtonExists = checkLoginButton?.Contains("true") == false;
    //        return !loginButtonExists;
    //    }
    //    private async Task UpdateLoginStatusAsync()
    //    {
    //        if (webBrowser.CoreWebView2 == null) return;
    //        bool isLoggedIn = await IsUserLoggedIn();
    //        loginStatusLBL.Text = isLoggedIn
    //            ? "Login Status: Logged In"
    //            : "Login Status: Logged Out";
    //    }

        // Load user videos with different modes
        private async void loadUserVideos(string selection)
        {
            if (string.IsNullOrWhiteSpace(userTXT.Text))
            {
                statusTXT.Text = "No user entered.";
                return;
            }
            if (webBrowser.CoreWebView2 == null)
            {
                return;
            }

            var targetUrl = "https://www.tiktok.com/@" + userTXT.Text;
            webBrowser.CoreWebView2.Navigate(targetUrl);
            statusTXT.Text = "Navigating to: " + targetUrl;
            progressBar.Value = 20;

            await WaitForPageLoadAsync();
            await Task.Delay(2000);

            // Small scroll
            await webBrowser.CoreWebView2.ExecuteScriptAsync("window.scrollTo(0, 1);");
            await Task.Delay(200);
            await webBrowser.CoreWebView2.ExecuteScriptAsync("window.scrollTo(0, -1);");
            await Task.Delay(200);

            switch (selection.ToLower())
            {
                case "visible":
                    break;
                case "all":
                case "videos":
                    statusTXT.Text = "Loading all videos";
                    await webBrowser.CoreWebView2.ExecuteScriptAsync("document.querySelector('p[data-e2e=\"videos-tab\"]')?.click();");
                    await Task.Delay(1000);
                    await AutoScrollToBottom();
                    progressBar.Value = 60;
                    break;
                case "likedvideos":
                    statusTXT.Text = "Loading liked videos...";
                    await webBrowser.CoreWebView2.ExecuteScriptAsync("document.querySelector('p[data-e2e=\"liked-tab\"]')?.click();");
                    await Task.Delay(1000);
                    break;
                case "allliked":
                    statusTXT.Text = "Loading all liked videos...";
                    await webBrowser.CoreWebView2.ExecuteScriptAsync("document.querySelector('p[data-e2e=\"liked-tab\"]')?.click();");
                    await Task.Delay(1000);
                    await AutoScrollToBottom();
                    progressBar.Value = 60;
                    break;
                case "reposts":
                    statusTXT.Text = "Loading reposts...";
                    await webBrowser.CoreWebView2.ExecuteScriptAsync("document.querySelector('p[data-e2e=\"repost-tab\"]')?.click();");
                    await Task.Delay(1000);
                    break;
                case "allreposts":
                    statusTXT.Text = "Loading all reposts...";
                    await webBrowser.CoreWebView2.ExecuteScriptAsync("document.querySelector('p[data-e2e=\"repost-tab\"]')?.click();");
                    await Task.Delay(1000);
                    await AutoScrollToBottom();
                    progressBar.Value = 60;
                    break;
                case "favoritedvideos":
                        statusTXT.Text = "Loading favorited videos...";
                        await webBrowser.CoreWebView2.ExecuteScriptAsync(@"
                            (function() {
                                let favTab = Array.from(document.querySelectorAll('p[role=""tab""]'))
                                    .find(el => el.innerText.includes('Favorites'));
                                if (favTab) favTab.click();
                            })();
                        ");
                    await Task.Delay(1000);
                    break;
                case "allfavorited":
                     statusTXT.Text = "Loading all favorited videos...";
                     await webBrowser.CoreWebView2.ExecuteScriptAsync(@"
                        (function() {
                            let favTab = Array.from(document.querySelectorAll('p[role=""tab""]'))
                                .find(el => el.innerText.includes('Favorites'));
                            if (favTab) favTab.click();
                        })();
                    ");
                    await Task.Delay(1000);
                    await AutoScrollToBottom();
                    progressBar.Value = 60;
                    break;

                default:
                    break;
            }

            // Grab HTML
            html = await GetHtmlSourceAsync();
            LoadVideos(html);
            statusTXT.Text = "Videos loaded successfully";
            progressBar.Value = 100;
        }

        private async Task AutoScrollToBottom()
        {
            while (true)
            {
                int currentPosition = await EvaluateIntAsync("window.scrollY");
                int documentHeight = await EvaluateIntAsync("document.body.scrollHeight");
                await webBrowser.CoreWebView2.ExecuteScriptAsync("window.scrollTo(0, document.body.scrollHeight);");
                await Task.Delay(1000);

                int newPosition = await EvaluateIntAsync("window.scrollY");
                int newDocumentHeight = await EvaluateIntAsync("document.body.scrollHeight");

                if (newPosition == currentPosition || newDocumentHeight == documentHeight)
                    break;
            }
            statusTXT.Text = "Reached bottom of page";
        }

        // "Latest Videos"
        private void getVideosDDB_Click(object sender, EventArgs e)
        {
            loadUserVideos("visible");
        }
        private void likedVideosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loadUserVideos("likedvideos");
        }
        private void repostsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loadUserVideos("reposts");
        }
        private void favoritedVideosToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loadUserVideos("favoritedvideos");
        }

        // "All Videos"
        private void allVideoDDB_Click(object sender, EventArgs e)
        {
            loadUserVideos("all");
        }
        private void allRepostsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loadUserVideos("allreposts");
        }
        private void allLikedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loadUserVideos("allliked");
        }
        private void allFavoritedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loadUserVideos("allfavorited");
        }

        private void DonateBTN_Click(object sender, EventArgs e)
        {
            // Simple approach: use the default browser
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://links.coolshrimpmodz.com",
                UseShellExecute = true
            });
        }
    }
}
