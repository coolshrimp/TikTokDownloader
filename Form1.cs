using System;
using System.Collections.Generic;
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
        private bool isDownloading;

        // videoList column indexes
        private const int ColCheck = 0;
        private const int ColID = 1;
        private const int ColDesc = 2;
        private const int ColViews = 3;
        private const int ColUrl = 4;
        private const int ColPreview = 5;
        private const int ColDownload = 6;

        // Tray icon / background-download support
        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private ToolStripMenuItem trayStatusItem;
        private bool trayBalloonShown;
        private bool exitRequested;

        private const string FallbackUserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

        private static readonly HttpClient httpClient = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                UseCookies = false, // we attach the WebView2 session cookies manually
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            return new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) };
        }

        // Result of resolving a TikTok page to a direct video URL
        private class VideoSource
        {
            public string VideoUrl;
            public string ThumbUrl;
            public string Method;          // "TikTok" or "SnapTik"
            public bool NeedsTikTokSession; // true when the URL requires TikTok cookies
        }

        private readonly Size _size1 = new Size(810, 1018);
        private readonly Size _size2 = new Size(1835, 1018);
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

            InitializeTrayIcon();
            InitializeTooltips();

            // Commit checkbox toggles immediately so batch selection reads current state
            videoList.CurrentCellDirtyStateChanged += (s, ev) =>
            {
                if (videoList.IsCurrentCellDirty && videoList.CurrentCell?.ColumnIndex == ColCheck)
                    videoList.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            videoList.ColumnHeaderMouseClick += videoList_ColumnHeaderMouseClick;

            // Ensure WebView2 is initialized
            Console.WriteLine("Form1_Load: Ensuring WebView2 is initialized...");
            await webBrowser.EnsureCoreWebView2Async();
            if (webBrowser.CoreWebView2 != null)
            {
                webBrowser.CoreWebView2.Settings.AreDevToolsEnabled = false;
                webBrowser.CoreWebView2.Settings.IsStatusBarEnabled = false;
            }

            // Start compact (browser panel hidden) with the how-to guide
            // overlay over the list; it hides as soon as videos are loaded.
            this.Size = _size1;
            await ShowGuideOverlayAsync();
            statusTXT.Text = "Enter a username, then click Latest Videos or All Videos";

            Console.WriteLine("Form1_Load: Load event complete.");
        }

        // In-app how-to guide, shown in an overlay covering the video list
        private async Task ShowGuideOverlayAsync()
        {
            await guideView.EnsureCoreWebView2Async();
            if (guideView.CoreWebView2 == null) return;
            guideView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            guideView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            guideView.CoreWebView2.NavigateToString(GuideHtml);
            guideView.Visible = true;
            guideView.BringToFront();
            guideBTN.Text = "✖";
        }

        private void HideGuideOverlay()
        {
            guideView.Visible = false;
            guideBTN.Text = "❓";
        }

        private async void guideBTN_Click(object sender, EventArgs e)
        {
            if (guideView.Visible) HideGuideOverlay();
            else await ShowGuideOverlayAsync();
        }

        private async void ShowHowToUse()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            await ShowGuideOverlayAsync();
        }

        private void InitializeTooltips()
        {
            var tips = new ToolTip { AutoPopDelay = 10000 };
            tips.SetToolTip(userTXT, "TikTok username without the @");
            tips.SetToolTip(getVideosDDB, "Load the videos currently visible on the profile.\nDropdown: Reposts / Liked / Favorited (login required for some).");
            tips.SetToolTip(allVideoDDB, "Auto-scroll the whole profile to load every video.\nDropdown: All Reposts / All Liked / All Favorited.");
            tips.SetToolTip(downloadAllBTN, "Download every video in the list to a folder.\nDropdown: download only the checked videos.\nAlready-downloaded videos are skipped.");
            tips.SetToolTip(stopBTN, "Stop the current bulk download");
            tips.SetToolTip(thumbCHK, "Also save a .jpg thumbnail next to each video");
            tips.SetToolTip(openFolderBTN, "Open the last download folder");
            tips.SetToolTip(SaveBTN, "Export the video list to a CSV file.\nDropdown: export only the checked videos.");
            tips.SetToolTip(expandBTN, "Show/hide the browser panel (TikTok login, download progress)");
            tips.SetToolTip(guideBTN, "Show/hide the How to Use guide");
        }

        private const string GuideHtml = @"<!DOCTYPE html>
<html><head><meta charset='utf-8'><title>How to Use</title><style>
  body { font-family: 'Segoe UI', sans-serif; background: #121212; color: #eee; margin: 0; padding: 32px 40px; line-height: 1.55; }
  h1 { color: #fff; font-size: 26px; margin-top: 0; }
  h1 .tik { color: #25F4EE; } h1 .tok { color: #FE2C55; }
  h2 { color: #25F4EE; font-size: 17px; margin: 26px 0 8px; }
  ol li, ul li { margin-bottom: 8px; }
  .card { background: #1e1e1e; border: 1px solid #333; border-radius: 10px; padding: 16px 20px; margin-bottom: 14px; }
  .pill { display: inline-block; background: #FE2C55; color: #fff; border-radius: 20px; padding: 1px 10px; font-size: 12px; margin-right: 6px; }
  .pill.alt { background: #444; }
  b { color: #fff; }
  .muted { color: #999; font-size: 13px; }
</style></head><body>
  <h1><span class='tik'>Tik</span><span class='tok'>Tok</span> Downloader &mdash; How to Use</h1>

  <div class='card'>
    <h2 style='margin-top:0'>Quick Start</h2>
    <ol>
      <li>Enter a TikTok <b>username</b> (without the @) in the box on the left.</li>
      <li>Click <b>📺 Latest Videos</b> for what's visible, or <b>🎞 All Videos</b> to auto-scroll and load the entire profile.</li>
      <li>Click <b>Download</b> on a single row, or <b>📥 Download All</b> to save everything to a folder.</li>
      <li>Or <b>tick the checkboxes</b> in the first column (click the ☑ header to toggle all) and use the dropdown arrow on <b>Download All</b> &rarr; <b>Download Selected</b> for just that batch. The <b>Save List</b> dropdown can export the checked rows too.</li>
      <li>Optional: check <b>Thumbnails</b> to also save a .jpg preview per video, and use <b>💾 Save List</b> to export a CSV.</li>
    </ol>
  </div>

  <div class='card'>
    <h2 style='margin-top:0'>How Downloads Work</h2>
    <p><span class='pill'>Primary</span> Videos are pulled <b>directly from TikTok</b> using this browser panel's session &mdash; the same source as TikTok's own download button.</p>
    <p><span class='pill alt'>Fallback</span> If a video can't be fetched directly (downloads disabled, region lock, rate limit), <b>SnapTik</b> is used automatically.</p>
  </div>

  <div class='card'>
    <h2 style='margin-top:0'>Logging In (optional but recommended)</h2>
    <ul>
      <li>Liked / Favorited tabs and some videos require being logged in.</li>
      <li>Click <b>Expand ⮞</b> to open the browser panel and log in to <b>tiktok.com</b> there &mdash; your session stays in the embedded browser profile. This app <b>never sees or stores your credentials</b>.</li>
    </ul>
  </div>

  <div class='card'>
    <h2 style='margin-top:0'>Tray Icon &amp; Background Downloads</h2>
    <ul>
      <li><b>Closing (X)</b> hides the app to the system tray while downloads keep running; minimize goes to the taskbar as usual. Quit via the tray menu's <b>Exit</b>.</li>
      <li>Hover the tray icon for live progress (e.g. <b>Downloading 20/300</b>); right-click it for Show/Hide, Open Folder, Stop, and Exit.</li>
    </ul>
  </div>

  <p class='muted'>Green rows = downloaded &bull; Red rows = failed &bull; Already-downloaded files are skipped in bulk mode.<br>
  Close this guide with the <b>✖</b> button at the top of the app (it reopens with <b>❓</b>). Loading a video list closes it automatically.<br>
  Created by Coolshrimp Modz &mdash; coolshrimpmodz.com</p>
</body></html>";

        // Helper to wait for next page NavigationCompleted.
        // Times out instead of hanging forever if navigation stalls (this was
        // freezing the app when SnapTik/TikTok never finished loading).
        private async Task<bool> WaitForPageLoadAsync(int timeoutMs = 30000)
        {
            var tcs = new TaskCompletionSource<bool>();
            void handler(object s, CoreWebView2NavigationCompletedEventArgs e)
            {
                webBrowser.NavigationCompleted -= handler;
                tcs.TrySetResult(true);
            }
            webBrowser.NavigationCompleted += handler;

            var finished = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
            if (finished != tcs.Task)
            {
                webBrowser.NavigationCompleted -= handler;
                return false;
            }
            return true;
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

            HideGuideOverlay();
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
                newRow.Cells[ColCheck].Value = false;
                newRow.Cells[ColID].Value = videoID;
                newRow.Cells[ColDesc].Value = videoDesc;
                newRow.Cells[ColViews].Value = videoStats;
                newRow.Cells[ColUrl].Value = videoUrl;

                var previewButton = new DataGridViewButtonCell { Value = "▶" };
                newRow.Cells[ColPreview] = previewButton;

                var downloadButton = new DataGridViewButtonCell { Value = "Download" };
                newRow.Cells[ColDownload] = downloadButton;

                videoList.Rows.Add(newRow);

                processedVideoCount++;
                double progressPercentage = (double)processedVideoCount / totalVideoCount * 100;
                progressBar.Value = (int)progressPercentage;
                statusTXT.Text = $"Processed {processedVideoCount} of {totalVideoCount}";

                // Make URL cell clickable
                var urlCell = newRow.Cells[ColUrl];
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

        // Click the checkbox column header => toggle all checkboxes
        private void videoList_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex != ColCheck) return;
            videoList.EndEdit();
            bool checkAll = videoList.Rows.Cast<DataGridViewRow>()
                .Any(r => !(r.Cells[ColCheck].Value is bool b && b));
            foreach (DataGridViewRow row in videoList.Rows)
                row.Cells[ColCheck].Value = checkAll;
            statusTXT.Text = checkAll ? "All videos checked" : "All videos unchecked";
        }

        // Rows the user has ticked in the checkbox column
        private List<DataGridViewRow> GetCheckedRows()
        {
            videoList.EndEdit();
            return videoList.Rows.Cast<DataGridViewRow>()
                .Where(r => r.Cells[ColCheck].Value is bool b && b)
                .ToList();
        }

        // Clicking a URL cell => open in default browser
        private void videoList_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == ColUrl && e.RowIndex >= 0)
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

        // Preview a video in the side panel before downloading
        private void PreviewVideo(string videoUrl)
        {
            if (webBrowser.CoreWebView2 == null || string.IsNullOrWhiteSpace(videoUrl)) return;
            if (isDownloading)
            {
                statusTXT.Text = "Can't preview while a download is running (the browser panel is in use).";
                return;
            }
            if (_isSize1)
            {
                _isSize1 = false;
                this.Size = _size2;
                expandBTN.Text = "Shrink ⮜";
            }
            statusTXT.Text = "Previewing video in side panel...";
            webBrowser.CoreWebView2.Navigate(videoUrl);
        }

        // Click download button
        private void videoList_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == ColPreview && e.RowIndex >= 0)
            {
                PreviewVideo(videoList.Rows[e.RowIndex].Cells[ColUrl].Value?.ToString());
                return;
            }
            if (e.ColumnIndex == ColDownload && e.RowIndex >= 0)
            {
                var videoUrl = videoList.Rows[e.RowIndex].Cells[e.ColumnIndex].Tag?.ToString();
                if (!string.IsNullOrEmpty(videoUrl))
                {
                    statusTXT.Text = "Downloading video...";
                    progressBar.Value = 0;
                    DownloadTikTokVideoAsync(videoList.Rows[e.RowIndex].Cells[ColUrl].Value?.ToString(), e.RowIndex);
                }
            }
        }

        // ===== Tray icon / background downloads =====

        private void InitializeTrayIcon()
        {
            trayStatusItem = new ToolStripMenuItem("Idle") { Enabled = false };

            trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add(trayStatusItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Show / Hide Window", null, (s, e) => ToggleWindowVisibility());
            trayMenu.Items.Add("How to Use", null, (s, e) => ShowHowToUse());
            trayMenu.Items.Add("Open Download Folder", null, (s, e) => openFolderBTN_Click(s, e));
            trayMenu.Items.Add("Stop Downloads", null, (s, e) => cancellationTokenSource?.Cancel());
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Exit", null, (s, e) => { exitRequested = true; Close(); });

            trayIcon = new NotifyIcon
            {
                Icon = this.Icon,
                Text = "TikTok Downloader",
                Visible = true,
                ContextMenuStrip = trayMenu
            };
            trayIcon.DoubleClick += (s, e) => ToggleWindowVisibility();
        }

        private void ToggleWindowVisibility()
        {
            if (Visible && WindowState != FormWindowState.Minimized)
            {
                Hide();
            }
            else
            {
                Show();
                WindowState = FormWindowState.Normal;
                Activate();
            }
        }

        // Updates the tray tooltip + context menu status line (e.g. "Downloading 20/300")
        private void UpdateTrayStatus(string text)
        {
            if (trayIcon == null) return;
            trayStatusItem.Text = text;

            var tip = "TikTok Downloader - " + text;
            if (tip.Length > 63) tip = tip.Substring(0, 63); // NotifyIcon.Text hard limit
            trayIcon.Text = tip;
        }

        // Toggle form size
        private void expandBTN_Click(object sender, EventArgs e)
        {
            _isSize1 = !_isSize1;
            this.Size = _isSize1 ? _size1 : _size2;
            expandBTN.Text = _isSize1 ? "Expand ⮞" : "Shrink ⮜";
        }

        // ===== Video source resolution: native TikTok first, SnapTik fallback =====

        // Escape a string for embedding inside a single-quoted JS literal
        private static string EscapeJsString(string value)
        {
            return value.Replace("\\", "\\\\").Replace("'", "\\'");
        }

        // ExecuteScriptAsync returns a JSON-encoded value; unwrap it to a plain string
        private static string UnwrapJsResult(string resultJson)
        {
            if (string.IsNullOrEmpty(resultJson) || resultJson == "null")
                return "";
            var s = resultJson;
            if (s.StartsWith("\"") && s.EndsWith("\"") && s.Length > 1)
                s = s.Substring(1, s.Length - 2);
            return Regex.Unescape(s);
        }

        private string GetBrowserUserAgent()
        {
            try
            {
                var ua = webBrowser.CoreWebView2?.Settings?.UserAgent;
                return string.IsNullOrWhiteSpace(ua) ? FallbackUserAgent : ua;
            }
            catch
            {
                return FallbackUserAgent;
            }
        }

        private async Task<string> GetTikTokCookieHeaderAsync()
        {
            var cookies = await webBrowser.CoreWebView2.CookieManager.GetCookiesAsync("https://www.tiktok.com/");
            return string.Join("; ", cookies.Select(c => c.Name + "=" + c.Value));
        }

        // Try native TikTok first, then fall back to SnapTik
        private async Task<VideoSource> ResolveVideoSourceAsync(string tikTokVideoUrl, CancellationToken ct)
        {
            var native = await TryGetNativeVideoSourceAsync(tikTokVideoUrl, ct);
            if (native != null) return native;
            if (ct.IsCancellationRequested) return null;

            statusTXT.Text = "TikTok direct download unavailable, trying SnapTik...";
            return await TryGetSnapTikVideoSourceAsync(tikTokVideoUrl, ct);
        }

        // Reads the direct video URL from TikTok's own page data
        // (same URL TikTok's built-in download button uses).
        private async Task<VideoSource> TryGetNativeVideoSourceAsync(string tikTokVideoUrl, CancellationToken ct)
        {
            try
            {
                statusTXT.Text = "Getting video directly from TikTok...";
                webBrowser.CoreWebView2.Navigate(tikTokVideoUrl);
                if (!await WaitForPageLoadAsync()) return null;
                await Task.Delay(1500);

                const string extractScript = @"
                    (function() {
                        try {
                            var el = document.getElementById('__UNIVERSAL_DATA_FOR_REHYDRATION__');
                            if (!el) return '';
                            var data = JSON.parse(el.textContent);
                            var scope = data['__DEFAULT_SCOPE__'] || {};
                            var detail = scope['webapp.video-detail'];
                            var item = detail && detail.itemInfo && detail.itemInfo.itemStruct;
                            if (!item || !item.video) return '';
                            var v = item.video.downloadAddr || item.video.playAddr || '';
                            var t = item.video.cover || item.video.originCover || '';
                            if (!v) {
                                var vid = document.querySelector('video');
                                if (vid && vid.src && vid.src.indexOf('blob:') !== 0) v = vid.src;
                            }
                            if (!v) return '';
                            return encodeURIComponent(v) + '||' + encodeURIComponent(t);
                        } catch (e) { return ''; }
                    })()";

                for (int i = 0; i < 10; i++)
                {
                    if (ct.IsCancellationRequested) return null;

                    var result = UnwrapJsResult(await webBrowser.CoreWebView2.ExecuteScriptAsync(extractScript));
                    if (result.Contains("||"))
                    {
                        var parts = result.Split(new[] { "||" }, StringSplitOptions.None);
                        var videoUrl = Uri.UnescapeDataString(parts[0]);
                        var thumbUrl = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "";
                        if (!string.IsNullOrWhiteSpace(videoUrl))
                        {
                            return new VideoSource
                            {
                                VideoUrl = videoUrl,
                                ThumbUrl = thumbUrl,
                                Method = "TikTok",
                                NeedsTikTokSession = true
                            };
                        }
                    }
                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Native TikTok extraction failed: " + ex.Message);
            }
            return null;
        }

        // SnapTik fallback. Sets the input through the native value setter and fires
        // input/change events so SnapTik's reactive UI actually registers the URL
        // (plain .value assignment was silently ignored, which broke downloads).
        private async Task<VideoSource> TryGetSnapTikVideoSourceAsync(string tikTokVideoUrl, CancellationToken ct)
        {
            try
            {
                statusTXT.Text = "Navigating to SnapTik...";
                webBrowser.CoreWebView2.Navigate("https://snaptik.app");
                if (!await WaitForPageLoadAsync())
                {
                    statusTXT.Text = "SnapTik did not load.";
                    return null;
                }
                await Task.Delay(2000);
                if (ct.IsCancellationRequested) return null;

                string fillScript = @"
                    (function() {
                        var input = document.getElementById('url')
                            || document.querySelector('input[name=""url""]')
                            || document.querySelector('form input[type=""url""], form input[type=""text""]');
                        if (!input) return 'no-input';
                        var setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, 'value').set;
                        setter.call(input, '" + EscapeJsString(tikTokVideoUrl) + @"');
                        input.dispatchEvent(new Event('input', { bubbles: true }));
                        input.dispatchEvent(new Event('change', { bubbles: true }));
                        var btn = (input.form && input.form.querySelector('button[type=""submit""]'))
                            || document.querySelector('button[type=""submit""]');
                        if (btn) { btn.click(); return 'submitted'; }
                        if (input.form) { input.form.submit(); return 'form-submitted'; }
                        return 'no-button';
                    })()";
                var fillResult = UnwrapJsResult(await webBrowser.CoreWebView2.ExecuteScriptAsync(fillScript));
                if (fillResult == "no-input")
                {
                    statusTXT.Text = "SnapTik page layout not recognized.";
                    return null;
                }
                await Task.Delay(2000);

                const string linkSelector =
                    ".video-links a.button.download-file, a.button.download-file, " +
                    "a[data-event*='download'], .download-box a[href], .video-links a[href]";

                bool foundLink = false;
                for (int i = 0; i < 30; i++)
                {
                    if (ct.IsCancellationRequested) return null;
                    var check = await webBrowser.CoreWebView2.ExecuteScriptAsync(
                        $"document.querySelector(\"{linkSelector}\") !== null");
                    if (check.Contains("true")) { foundLink = true; break; }
                    await Task.Delay(1000);
                }
                if (!foundLink)
                {
                    statusTXT.Text = "Could not find SnapTik download link.";
                    return null;
                }

                var rawUrl = UnwrapJsResult(await webBrowser.CoreWebView2.ExecuteScriptAsync(
                    $"document.querySelector(\"{linkSelector}\").getAttribute('href')"));
                var thumbUrl = UnwrapJsResult(await webBrowser.CoreWebView2.ExecuteScriptAsync(
                    "document.querySelector('img#thumbnail') ? document.querySelector('img#thumbnail').getAttribute('src') : ''"));

                if (string.IsNullOrWhiteSpace(rawUrl) || rawUrl == "null")
                {
                    statusTXT.Text = "SnapTik returned no video URL.";
                    return null;
                }

                return new VideoSource
                {
                    VideoUrl = rawUrl,
                    ThumbUrl = thumbUrl,
                    Method = "SnapTik",
                    NeedsTikTokSession = false
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("SnapTik extraction failed: " + ex.Message);
                return null;
            }
        }

        // Download any file, attaching the TikTok browser session (cookies/UA/referer) when required
        private async Task<bool> DownloadFileAsync(string url, string filePath, bool useTikTokSession)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.TryAddWithoutValidation("User-Agent", GetBrowserUserAgent());
                if (useTikTokSession)
                {
                    request.Headers.TryAddWithoutValidation("Cookie", await GetTikTokCookieHeaderAsync());
                    request.Headers.TryAddWithoutValidation("Referer", "https://www.tiktok.com/");
                }

                using (var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Download failed, HTTP {(int)response.StatusCode} for {url}");
                        return false;
                    }
                    using (var inStream = await response.Content.ReadAsStreamAsync())
                    using (var outStream = new FileStream(filePath, FileMode.Create))
                    {
                        await inStream.CopyToAsync(outStream);
                    }
                    return true;
                }
            }
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
            if (isDownloading)
            {
                statusTXT.Text = "A download is already in progress.";
                return;
            }

            isDownloading = true;
            UpdateTrayStatus("Downloading 1 video...");
            try
            {
                // Extract ID
                string videoID = "TikTokVideo";
                var idMatch = Regex.Match(tikTokVideoUrl, @"/(?:video|v)/(\d+)");
                if (idMatch.Success) videoID = idMatch.Groups[1].Value;

                progressBar.Value = 10;
                var source = await ResolveVideoSourceAsync(tikTokVideoUrl, CancellationToken.None);
                if (source == null)
                {
                    statusTXT.Text = "Could not get a download link (TikTok and SnapTik both failed).";
                    progressBar.Value = 0;
                    return;
                }
                progressBar.Value = 50;

                // Prompt for final name
                SaveFileDialog sfd = new SaveFileDialog
                {
                    Filter = "MP4 Files|*.mp4",
                    FileName = $"{videoID}.mp4",
                    InitialDirectory = lastDownloadedFolderPath
                };
                if (sfd.ShowDialog() != DialogResult.OK)
                {
                    progressBar.Value = 0;
                    return;
                }

                lastDownloadedFolderPath = Path.GetDirectoryName(sfd.FileName);
                statusTXT.Text = $"Downloading video via {source.Method}...";
                try
                {
                    if (await DownloadFileAsync(source.VideoUrl, sfd.FileName, source.NeedsTikTokSession))
                    {
                        videoList.Rows[videoURLrow].DefaultCellStyle.BackColor = Color.LightGreen;
                        openFolderBTN.Enabled = true;
                        statusTXT.Text = $"Video saved ({source.Method}): {sfd.FileName}";
                    }
                    else
                    {
                        statusTXT.Text = "Failed to download video file.";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error downloading: " + ex.Message);
                    statusTXT.Text = "Error downloading video.";
                }

                // If user wants thumbnails
                if (thumbCHK.Checked && !string.IsNullOrWhiteSpace(source.ThumbUrl))
                {
                    string thumbPath = Path.Combine(Path.GetDirectoryName(sfd.FileName), $"{videoID}.jpg");
                    try
                    {
                        await DownloadFileAsync(source.ThumbUrl, thumbPath, source.NeedsTikTokSession);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error downloading thumbnail => " + ex.Message);
                    }
                }
                progressBar.Value = 100;
            }
            finally
            {
                isDownloading = false;
                UpdateTrayStatus("Idle");
            }
        }

        // Bulk download of an arbitrary set of rows (all rows or checked rows)
        private async void DownloadRowsAsync(List<DataGridViewRow> rows)
        {
            if (isDownloading)
            {
                statusTXT.Text = "A download is already in progress.";
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            var folderBrowserDialog = new FolderBrowserDialog { SelectedPath = lastDownloadedFolderPath };

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                lastDownloadedFolderPath = folderBrowserDialog.SelectedPath;
                stopBTN.Enabled = true;
                isDownloading = true;
                trayBalloonShown = false;

                int totalVideos = rows
                    .Count(r => !string.IsNullOrWhiteSpace(r.Cells[ColUrl].Value?.ToString()));
                int currentVideo = 0;

                try
                {
                    foreach (DataGridViewRow row in rows)
                    {
                        if (cancellationTokenSource.Token.IsCancellationRequested)
                            break;

                        string tikTokVideoUrl = row.Cells[ColUrl].Value?.ToString();
                        if (string.IsNullOrWhiteSpace(tikTokVideoUrl))
                            continue;

                        currentVideo++;
                        UpdateTrayStatus($"Downloading {currentVideo}/{totalVideos}");

                        string videoId = "TikTokVideo";
                        var m = Regex.Match(tikTokVideoUrl, @"(?<=video/)\d+");
                        if (m.Success) videoId = m.Value;

                        string filePath = Path.Combine(folderBrowserDialog.SelectedPath, $"{videoId}.mp4");
                        if (File.Exists(filePath))
                        {
                            row.DefaultCellStyle.BackColor = Color.LightGreen;
                            continue;
                        }

                        statusTXT.Text = $"Resolving {tikTokVideoUrl}...";
                        progressBar.Value = 10;

                        var source = await ResolveVideoSourceAsync(tikTokVideoUrl, cancellationTokenSource.Token);
                        if (cancellationTokenSource.Token.IsCancellationRequested)
                            break;
                        if (source == null)
                        {
                            statusTXT.Text = $"Could not get a download link for {videoId}, skipping.";
                            row.DefaultCellStyle.BackColor = Color.MistyRose;
                            continue;
                        }

                        progressBar.Value = 60;
                        statusTXT.Text = $"Downloading {videoId} via {source.Method}...";

                        try
                        {
                            if (await DownloadFileAsync(source.VideoUrl, filePath, source.NeedsTikTokSession))
                            {
                                statusTXT.Text = $"Video saved to {filePath}";
                                lastDownloadedFolderPath = Path.GetDirectoryName(filePath);
                                openFolderBTN.Enabled = true;
                                row.DefaultCellStyle.BackColor = Color.LightGreen;
                            }
                            else
                            {
                                row.DefaultCellStyle.BackColor = Color.MistyRose;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error downloading video: " + ex.Message);
                            row.DefaultCellStyle.BackColor = Color.MistyRose;
                        }

                        // Download thumbnail if checked
                        if (thumbCHK.Checked && !string.IsNullOrWhiteSpace(source.ThumbUrl))
                        {
                            string thumbPath = Path.Combine(folderBrowserDialog.SelectedPath, $"{videoId}.jpg");
                            try
                            {
                                await DownloadFileAsync(source.ThumbUrl, thumbPath, source.NeedsTikTokSession);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Error downloading thumbnail => " + ex.Message);
                            }
                        }
                    }
                }
                finally
                {
                    isDownloading = false;
                    stopBTN.Enabled = false;
                    UpdateTrayStatus(cancellationTokenSource.Token.IsCancellationRequested
                        ? $"Stopped at {currentVideo}/{totalVideos}"
                        : $"Complete: {currentVideo}/{totalVideos}");
                }

                progressBar.Value = 100;
                statusTXT.Text = "All Video Downloads complete";
                MessageBox.Show("All Video Downloads complete");
            }
        }

        // Click Download All button
        private void downloadAllBTN_Click(object sender, EventArgs e)
        {
            DownloadRowsAsync(videoList.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).ToList());
        }

        // Download only the checked videos (split-button dropdown)
        private void downloadSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var rows = GetCheckedRows();
            if (rows.Count == 0)
            {
                MessageBox.Show("No videos are checked. Tick the boxes in the first column, then try again.", "Nothing Selected");
                return;
            }
            DownloadRowsAsync(rows);
        }

        // Export full list to CSV
        private void button1_Click(object sender, EventArgs e)
        {
            ExportToCsv(false);
        }

        // Export only the checked videos to CSV (split-button dropdown)
        private void saveSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportToCsv(true);
        }

        private void ExportToCsv(bool selectedOnly)
        {
            var rows = selectedOnly
                ? GetCheckedRows()
                : videoList.Rows.Cast<DataGridViewRow>().Where(r => !r.IsNewRow).ToList();

            if (rows.Count == 0)
            {
                MessageBox.Show(selectedOnly
                    ? "No videos are checked. Tick the boxes in the first column, then try again."
                    : "The video list is empty.", "Nothing to Export");
                return;
            }

            var saveDialog = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = selectedOnly ? "Tiktok_Videos_Selected.csv" : "Tiktok_Videos_List.csv",
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
                        // Headers (skip checkbox and download-button columns)
                        var headers = new List<string>();
                        for (int i = 0; i < videoList.Columns.Count; i++)
                        {
                            if (i == ColCheck || i == ColPreview || i == ColDownload) continue;
                            headers.Add(videoList.Columns[i].HeaderText.Replace(",", ""));
                        }
                        streamWriter.WriteLine(string.Join(",", headers));

                        // Rows
                        foreach (DataGridViewRow row in rows)
                        {
                            var values = new List<string>();
                            for (int i = 0; i < row.Cells.Count; i++)
                            {
                                if (i == ColCheck || i == ColPreview || i == ColDownload) continue;
                                string cellValue = row.Cells[i].FormattedValue?.ToString().Replace(",", "") ?? "";
                                // leading ' to preserve IDs with leading zeros
                                if (i == ColID) cellValue = "'" + cellValue;
                                values.Add(cellValue);
                            }
                            streamWriter.WriteLine(string.Join(",", values));
                        }
                    }
                    MessageBox.Show($"Exported {rows.Count} video(s) to {saveDialog.FileName}", "Export Complete");
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

        // Close (X) hides to the tray; the app only exits via the tray menu.
        // Minimize behaves normally (taskbar).
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!exitRequested && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                if (!trayBalloonShown && trayIcon != null)
                {
                    trayBalloonShown = true;
                    trayIcon.BalloonTipTitle = "TikTok Downloader";
                    trayIcon.BalloonTipText = "Still running in the tray. Double-click the icon to reopen, or right-click and Exit to quit.";
                    trayIcon.ShowBalloonTip(3000);
                }
                return;
            }

            Properties.Settings.Default.lastUser = userTXT.Text;
            Properties.Settings.Default.lastFolder = lastDownloadedFolderPath;
            Properties.Settings.Default.lastThumb = thumbCHK.Checked;
            Properties.Settings.Default.Save();

            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                trayIcon = null;
            }
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

            HideGuideOverlay();

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
