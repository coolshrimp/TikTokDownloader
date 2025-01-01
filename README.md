# TikTok Downloader

TikTok Downloader is a Windows-based utility designed to simplify bulk or single-video downloads from TikTok user profiles, liked videos, reposts, and favorites. It provides a friendly GUI with the ability to **auto-scroll** to load all user videos, parse video URLs, and easily download them via SnapTik integration.

![Main Interface Screenshot](./screenshots/main-interface.png)

---

## Features

- **Load User Videos**: Enter a TikTok username and instantly see their posted content.
- **Batch or Single Download**: Download all videos in bulk or pick individual videos to save.
- **Auto Scroll**: Automatically scroll to the bottom of a user’s TikTok page to load all content.
- **Likes / Favorites / Reposts**: Retrieve liked, favorited, or reposted videos (if you’re logged in).
- **No Watermark**: Uses SnapTik for downloads, typically retrieving no-watermark versions.
- **CSV Export**: Export the collected video list to a CSV file.
- **Progress Tracking**: Shows real-time status and progress for scraping or downloads.
- **Thumbnail Option**: Download thumbnails alongside the videos for easy reference.

---

## Installation

1. **Download** the latest release from the [Releases](./releases) section (an EXE file).
2. **Run** the `.exe`—no formal install required.
3. Upon first launch, it will initialize the embedded WebView2 control.

**Requirements**:
- **Windows 10 or later**  
- [**.NET 6.0 Desktop Runtime**](https://dotnet.microsoft.com/en-us/download) or newer  
- A stable internet connection

---

## Usage

1. **Enter TikTok Username**  
   - Type the username in the text field (e.g. `@username` without the `@`).

2. **Choose Video Scope**  
   - Click **Latest Videos** to load visible posts without autoscroll.  
   - Click **All Videos** (or use the drop-down) to auto-scroll and load the entire feed (including reposts, liked, or favorited videos if you’re logged in).

3. **Load Videos**  
   - Wait for the progress bar to reach 100%. The DataGridView will list each found video.

4. **Download Videos**  
   - **Single Download**: Click the **Download** button in the row you want.  
   - **Download All**: Use the “Download All” button to save every video in the list to a selected folder.

5. **Optional CSV Export**  
   - Use the “Save List” button to export the entire video list (IDs, Descriptions, URLs) to a CSV file.

---

## Thumbnail Downloads

If **Thumbnails** are enabled (checkbox in the tool), each downloaded video also includes a `.jpg` thumbnail in the same folder.

---

## SnapTik Integration

Under the hood, the tool navigates to [SnapTik](https://snaptik.app) in a hidden or embedded WebView2 to retrieve direct download links. If SnapTik fails or requires a CAPTCHA, the tool will notify you in the status bar.

---

## Troubleshooting

- **WebView2 Not Initialized**: Install the [WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) or ensure it’s up to date.
- **No Videos Found**: Double-check the username spelling or ensure you have an active internet connection.
- **Login-Required Tabs Not Working**: If you’re not logged in to TikTok in the embedded browser, “liked” or “favorited” videos may not load.
- **Stuck or Slow**: Large accounts or slow internet might require more time. Check the progress bar or status for updates.

---

## Version History

### v1.0.0
- **Initial Release**
  - Load user videos (posted content).
  - Single or All videos download with SnapTik.
  - CSV export of the video list.
  - Auto scroll to load more videos.
  - Thumbnail downloads (optional).
  - Basic login checks for liked/favorited content.

### v1.1.0
- **Added**: Reposts / Liked / Favorited content retrieval (logged in).
- **Enhanced**: Improved SnapTik integration and progress tracking.
- **Fixed**: Minor bug with CSV export not including stats properly.

### v1.2.0
- **New**: Hidden WebView2 for login status checks without altering the main browsing flow.
- **Improved**: Stability of auto-scroll for large accounts.
- **Optimized**: Performance for multi-video downloads.

---

## Contributing

1. **Fork** the repository and create a new branch for your changes.
2. **Submit** a pull request with a clear explanation of your modifications.

---

## License

This project is licensed under the [MIT License](./LICENSE).

---

**Enjoy downloading TikTok videos!** If you find a bug or have suggestions, feel free to open an [issue](./issues) or contribute to this project.
