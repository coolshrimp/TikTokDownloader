using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TikTok_Downloader
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            PreloadWebView2Loader();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        // The exe is a single file: the native WebView2Loader.dll for both
        // architectures is embedded as a resource. Extract the one matching
        // the current process and load it before any WebView2 call, so the
        // DllImport("WebView2Loader.dll") inside the WebView2 SDK resolves
        // to the already-loaded module instead of searching the disk.
        private static void PreloadWebView2Loader()
        {
            try
            {
                string arch = Environment.Is64BitProcess ? "x64" : "x86";
                string resourceName = $"WebView2Loader.{arch}.dll";

                using (var resource = typeof(Program).Assembly.GetManifestResourceStream(resourceName))
                {
                    if (resource == null) return; // not embedded; fall back to normal DLL search

                    string dir = Path.Combine(Path.GetTempPath(), "TikTokDownloader", arch);
                    Directory.CreateDirectory(dir);
                    string path = Path.Combine(dir, "WebView2Loader.dll");

                    try
                    {
                        if (!File.Exists(path) || new FileInfo(path).Length != resource.Length)
                        {
                            using (var file = new FileStream(path, FileMode.Create, FileAccess.Write))
                                resource.CopyTo(file);
                        }
                    }
                    catch (IOException)
                    {
                        // Locked by another running instance - the existing file is fine to load.
                    }

                    LoadLibrary(path);
                }
            }
            catch
            {
                // Never block startup; WebView2 will surface its own error if the loader is truly missing.
            }
        }
    }
}
