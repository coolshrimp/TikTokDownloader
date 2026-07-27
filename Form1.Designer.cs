using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace TikTok_Downloader
{
    partial class Form1
    {
        private IContainer components = null;

        // Single WebView2 control
        private WebView2 webBrowser;
        private TextBox userTXT;
        private DataGridView videoList;
        private Label label1;
        private ProgressBar progressBar;
        private Label statusTXT;
        private Button expandBTN;
        private DataGridViewCheckBoxColumn ColumnCheck;
        private DataGridViewTextBoxColumn Column5;
        private DataGridViewTextBoxColumn Column1;
        private DataGridViewTextBoxColumn Column4;
        private DataGridViewTextBoxColumn Column2;
        private DataGridViewButtonColumn Column3;
        private wyDay.Controls.SplitButton downloadAllBTN;
        private wyDay.Controls.SplitButton SaveBTN;
        private ContextMenuStrip contextMenuStripDownload;
        private ToolStripMenuItem downloadSelectedToolStripMenuItem;
        private ContextMenuStrip contextMenuStripSave;
        private ToolStripMenuItem saveSelectedToolStripMenuItem;
        private Button openFolderBTN;
        private Label label2;
        private Button stopBTN;
        private CheckBox thumbCHK;

        // Extra controls
        private wyDay.Controls.SplitButton getVideosDDB;
        private ContextMenuStrip contextMenuStripvideos;
        private ToolStripMenuItem likedVideosToolStripMenuItem;
        private ToolStripMenuItem repostsToolStripMenuItem;
        private ToolStripMenuItem favoritedVideosToolStripMenuItem;
        private ContextMenuStrip contextMenuStripAllVideos;
        private wyDay.Controls.SplitButton allVideoDDB;
        private ToolStripMenuItem allRepostsToolStripMenuItem;
        private ToolStripMenuItem allLikedToolStripMenuItem;
        private ToolStripMenuItem allFavoritedToolStripMenuItem;

        /// <summary>
        /// Dispose resources.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.userTXT = new System.Windows.Forms.TextBox();
            this.videoList = new System.Windows.Forms.DataGridView();
            this.ColumnCheck = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.Column5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Column3 = new System.Windows.Forms.DataGridViewButtonColumn();
            this.label1 = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.statusTXT = new System.Windows.Forms.Label();
            this.expandBTN = new System.Windows.Forms.Button();
            this.downloadAllBTN = new wyDay.Controls.SplitButton();
            this.SaveBTN = new wyDay.Controls.SplitButton();
            this.contextMenuStripDownload = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.downloadSelectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStripSave = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.saveSelectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFolderBTN = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.stopBTN = new System.Windows.Forms.Button();
            this.thumbCHK = new System.Windows.Forms.CheckBox();
            this.webBrowser = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.getVideosDDB = new wyDay.Controls.SplitButton();
            this.contextMenuStripvideos = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.repostsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.likedVideosToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.favoritedVideosToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStripAllVideos = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.allRepostsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.allLikedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.allFavoritedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.allVideoDDB = new wyDay.Controls.SplitButton();
            this.DonateBTN = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.videoList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.webBrowser)).BeginInit();
            this.contextMenuStripvideos.SuspendLayout();
            this.contextMenuStripAllVideos.SuspendLayout();
            this.SuspendLayout();
            // 
            // userTXT
            // 
            this.userTXT.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.userTXT.Location = new System.Drawing.Point(227, 12);
            this.userTXT.Name = "userTXT";
            this.userTXT.Size = new System.Drawing.Size(402, 23);
            this.userTXT.TabIndex = 1;
            this.userTXT.TextChanged += new System.EventHandler(this.userTXT_TextChanged);
            // 
            // videoList
            // 
            this.videoList.AllowUserToAddRows = false;
            this.videoList.AllowUserToDeleteRows = false;
            this.videoList.AllowUserToResizeRows = false;
            this.videoList.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.videoList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.videoList.RowHeadersVisible = false;
            this.videoList.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.ColumnCheck,
            this.Column5,
            this.Column1,
            this.Column4,
            this.Column2,
            this.Column3});
            this.videoList.Location = new System.Drawing.Point(12, 81);
            this.videoList.Name = "videoList";
            this.videoList.Size = new System.Drawing.Size(708, 833);
            this.videoList.TabIndex = 2;
            this.videoList.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.videoList_CellClick);
            this.videoList.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.videoList_CellContentClick);
            //
            // ColumnCheck
            //
            this.ColumnCheck.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.ColumnCheck.HeaderText = "☑";
            this.ColumnCheck.Name = "ColumnCheck";
            this.ColumnCheck.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.ColumnCheck.ToolTipText = "Check videos to download or export as a batch. Click the header to toggle all.";
            this.ColumnCheck.Width = 34;
            //
            // Column5
            //
            this.Column5.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.Column5.HeaderText = "ID";
            this.Column5.Name = "Column5";
            this.Column5.Width = 43;
            // 
            // Column1
            // 
            this.Column1.HeaderText = "Description";
            this.Column1.Name = "Column1";
            // 
            // Column4
            // 
            this.Column4.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.Column4.HeaderText = "Views";
            this.Column4.Name = "Column4";
            this.Column4.Width = 60;
            // 
            // Column2
            // 
            this.Column2.HeaderText = "Video URL";
            this.Column2.Name = "Column2";
            // 
            // Column3
            // 
            this.Column3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
            this.Column3.HeaderText = "Download";
            this.Column3.Name = "Column3";
            this.Column3.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.Column3.Text = "Download Now";
            this.Column3.Width = 80;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F);
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(214, 24);
            this.label1.TabIndex = 5;
            this.label1.Text = "Enter TikTok Username:";
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(67, 954);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(599, 13);
            this.progressBar.TabIndex = 6;
            // 
            // statusTXT
            // 
            this.statusTXT.BackColor = System.Drawing.Color.Transparent;
            this.statusTXT.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.statusTXT.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F);
            this.statusTXT.Location = new System.Drawing.Point(78, 924);
            this.statusTXT.Name = "statusTXT";
            this.statusTXT.Size = new System.Drawing.Size(577, 27);
            this.statusTXT.TabIndex = 7;
            this.statusTXT.Text = "----";
            this.statusTXT.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // expandBTN
            // 
            this.expandBTN.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.expandBTN.Location = new System.Drawing.Point(635, 8);
            this.expandBTN.Name = "expandBTN";
            this.expandBTN.Size = new System.Drawing.Size(85, 31);
            this.expandBTN.TabIndex = 8;
            this.expandBTN.Text = "Expand ⮞";
            this.expandBTN.UseVisualStyleBackColor = true;
            this.expandBTN.Click += new System.EventHandler(this.expandBTN_Click);
            // 
            // downloadAllBTN
            // 
            this.downloadAllBTN.ContextMenuStrip = this.contextMenuStripDownload;
            this.downloadAllBTN.SplitMenuStrip = this.contextMenuStripDownload;
            this.downloadAllBTN.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.downloadAllBTN.Location = new System.Drawing.Point(251, 43);
            this.downloadAllBTN.Name = "downloadAllBTN";
            this.downloadAllBTN.Size = new System.Drawing.Size(118, 31);
            this.downloadAllBTN.TabIndex = 9;
            this.downloadAllBTN.Text = "📥 Download All";
            this.downloadAllBTN.UseVisualStyleBackColor = true;
            this.downloadAllBTN.Click += new System.EventHandler(this.downloadAllBTN_Click);
            // 
            // SaveBTN
            // 
            this.SaveBTN.ContextMenuStrip = this.contextMenuStripSave;
            this.SaveBTN.SplitMenuStrip = this.contextMenuStripSave;
            this.SaveBTN.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.SaveBTN.Location = new System.Drawing.Point(622, 42);
            this.SaveBTN.Name = "SaveBTN";
            this.SaveBTN.Size = new System.Drawing.Size(98, 32);
            this.SaveBTN.TabIndex = 10;
            this.SaveBTN.Text = "💾 Save List";
            this.SaveBTN.UseVisualStyleBackColor = true;
            this.SaveBTN.Click += new System.EventHandler(this.button1_Click);
            // 
            // openFolderBTN
            // 
            this.openFolderBTN.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.openFolderBTN.Location = new System.Drawing.Point(539, 42);
            this.openFolderBTN.Name = "openFolderBTN";
            this.openFolderBTN.Size = new System.Drawing.Size(77, 33);
            this.openFolderBTN.TabIndex = 11;
            this.openFolderBTN.Text = "🗀 Folder";
            this.openFolderBTN.UseVisualStyleBackColor = true;
            this.openFolderBTN.Click += new System.EventHandler(this.openFolderBTN_Click);
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Cursor = System.Windows.Forms.Cursors.Hand;
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Underline);
            this.label2.ForeColor = System.Drawing.Color.RoyalBlue;
            this.label2.Location = new System.Drawing.Point(1175, 940);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(236, 27);
            this.label2.TabIndex = 12;
            this.label2.Text = "Created By: Coolshrimp Modz";
            this.label2.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            this.label2.Click += new System.EventHandler(this.label2_Click);
            // 
            // stopBTN
            // 
            this.stopBTN.Enabled = false;
            this.stopBTN.Location = new System.Drawing.Point(467, 42);
            this.stopBTN.Name = "stopBTN";
            this.stopBTN.Size = new System.Drawing.Size(66, 32);
            this.stopBTN.TabIndex = 13;
            this.stopBTN.Text = "🛑 Stop";
            this.stopBTN.UseVisualStyleBackColor = true;
            this.stopBTN.Click += new System.EventHandler(this.stopBTN_Click);
            // 
            // thumbCHK
            // 
            this.thumbCHK.AutoSize = true;
            this.thumbCHK.Location = new System.Drawing.Point(375, 51);
            this.thumbCHK.Name = "thumbCHK";
            this.thumbCHK.Size = new System.Drawing.Size(86, 17);
            this.thumbCHK.TabIndex = 14;
            this.thumbCHK.Text = "Thumbnails?";
            this.thumbCHK.UseVisualStyleBackColor = true;
            // 
            // webBrowser
            // 
            this.webBrowser.AllowExternalDrop = true;
            this.webBrowser.CreationProperties = null;
            this.webBrowser.DefaultBackgroundColor = System.Drawing.Color.White;
            this.webBrowser.Location = new System.Drawing.Point(737, 13);
            this.webBrowser.Name = "webBrowser";
            this.webBrowser.Size = new System.Drawing.Size(1005, 901);
            this.webBrowser.TabIndex = 3;
            this.webBrowser.ZoomFactor = 1D;
            // 
            // getVideosDDB
            // 
            this.getVideosDDB.AutoSize = true;
            this.getVideosDDB.ContextMenuStrip = this.contextMenuStripvideos;
            this.getVideosDDB.Location = new System.Drawing.Point(12, 42);
            this.getVideosDDB.Name = "getVideosDDB";
            this.getVideosDDB.Size = new System.Drawing.Size(119, 32);
            this.getVideosDDB.SplitMenuStrip = this.contextMenuStripvideos;
            this.getVideosDDB.TabIndex = 15;
            this.getVideosDDB.Text = "📺 Latest Videos";
            this.getVideosDDB.UseVisualStyleBackColor = true;
            this.getVideosDDB.Click += new System.EventHandler(this.getVideosDDB_Click);
            // 
            // contextMenuStripvideos
            // 
            this.contextMenuStripvideos.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.repostsToolStripMenuItem,
            this.likedVideosToolStripMenuItem,
            this.favoritedVideosToolStripMenuItem});
            this.contextMenuStripvideos.Name = "contextMenuStripvideos";
            this.contextMenuStripvideos.Size = new System.Drawing.Size(175, 70);
            // 
            // repostsToolStripMenuItem
            // 
            this.repostsToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.repostsToolStripMenuItem.Name = "repostsToolStripMenuItem";
            this.repostsToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.repostsToolStripMenuItem.Text = "🔁 Reposts";
            this.repostsToolStripMenuItem.Click += new System.EventHandler(this.repostsToolStripMenuItem_Click);
            // 
            // likedVideosToolStripMenuItem
            // 
            this.likedVideosToolStripMenuItem.Name = "likedVideosToolStripMenuItem";
            this.likedVideosToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.likedVideosToolStripMenuItem.Text = "❤️ Liked Videos";
            this.likedVideosToolStripMenuItem.Click += new System.EventHandler(this.likedVideosToolStripMenuItem_Click);
            // 
            // favoritedVideosToolStripMenuItem
            // 
            this.favoritedVideosToolStripMenuItem.Name = "favoritedVideosToolStripMenuItem";
            this.favoritedVideosToolStripMenuItem.Size = new System.Drawing.Size(174, 22);
            this.favoritedVideosToolStripMenuItem.Text = "⭐ Favorited Videos";
            this.favoritedVideosToolStripMenuItem.Click += new System.EventHandler(this.favoritedVideosToolStripMenuItem_Click);
            //
            // contextMenuStripDownload
            //
            this.contextMenuStripDownload.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.downloadSelectedToolStripMenuItem});
            this.contextMenuStripDownload.Name = "contextMenuStripDownload";
            this.contextMenuStripDownload.Size = new System.Drawing.Size(210, 26);
            //
            // downloadSelectedToolStripMenuItem
            //
            this.downloadSelectedToolStripMenuItem.Name = "downloadSelectedToolStripMenuItem";
            this.downloadSelectedToolStripMenuItem.Size = new System.Drawing.Size(209, 22);
            this.downloadSelectedToolStripMenuItem.Text = "📥 Download Selected ☑";
            this.downloadSelectedToolStripMenuItem.Click += new System.EventHandler(this.downloadSelectedToolStripMenuItem_Click);
            //
            // contextMenuStripSave
            //
            this.contextMenuStripSave.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveSelectedToolStripMenuItem});
            this.contextMenuStripSave.Name = "contextMenuStripSave";
            this.contextMenuStripSave.Size = new System.Drawing.Size(200, 26);
            //
            // saveSelectedToolStripMenuItem
            //
            this.saveSelectedToolStripMenuItem.Name = "saveSelectedToolStripMenuItem";
            this.saveSelectedToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
            this.saveSelectedToolStripMenuItem.Text = "💾 Save Selected ☑";
            this.saveSelectedToolStripMenuItem.Click += new System.EventHandler(this.saveSelectedToolStripMenuItem_Click);
            //
            // contextMenuStripAllVideos
            //
            this.contextMenuStripAllVideos.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.allRepostsToolStripMenuItem,
            this.allLikedToolStripMenuItem,
            this.allFavoritedToolStripMenuItem});
            this.contextMenuStripAllVideos.Name = "contextMenuStripAllVideos";
            this.contextMenuStripAllVideos.Size = new System.Drawing.Size(154, 70);
            // 
            // allRepostsToolStripMenuItem
            // 
            this.allRepostsToolStripMenuItem.Name = "allRepostsToolStripMenuItem";
            this.allRepostsToolStripMenuItem.Size = new System.Drawing.Size(153, 22);
            this.allRepostsToolStripMenuItem.Text = "🔁 All Reposts";
            this.allRepostsToolStripMenuItem.Click += new System.EventHandler(this.allRepostsToolStripMenuItem_Click);
            // 
            // allLikedToolStripMenuItem
            // 
            this.allLikedToolStripMenuItem.Name = "allLikedToolStripMenuItem";
            this.allLikedToolStripMenuItem.Size = new System.Drawing.Size(153, 22);
            this.allLikedToolStripMenuItem.Text = "❤️ All Liked";
            this.allLikedToolStripMenuItem.Click += new System.EventHandler(this.allLikedToolStripMenuItem_Click);
            // 
            // allFavoritedToolStripMenuItem
            // 
            this.allFavoritedToolStripMenuItem.Name = "allFavoritedToolStripMenuItem";
            this.allFavoritedToolStripMenuItem.Size = new System.Drawing.Size(153, 22);
            this.allFavoritedToolStripMenuItem.Text = "⭐ All Favorited";
            this.allFavoritedToolStripMenuItem.Click += new System.EventHandler(this.allFavoritedToolStripMenuItem_Click);
            // 
            // allVideoDDB
            // 
            this.allVideoDDB.AutoSize = true;
            this.allVideoDDB.ContextMenuStrip = this.contextMenuStripAllVideos;
            this.allVideoDDB.Location = new System.Drawing.Point(137, 43);
            this.allVideoDDB.Name = "allVideoDDB";
            this.allVideoDDB.Size = new System.Drawing.Size(108, 32);
            this.allVideoDDB.SplitMenuStrip = this.contextMenuStripAllVideos;
            this.allVideoDDB.TabIndex = 17;
            this.allVideoDDB.Text = "🎞 All Videos";
            this.allVideoDDB.UseVisualStyleBackColor = true;
            this.allVideoDDB.Click += new System.EventHandler(this.allVideoDDB_Click);
            // 
            // DonateBTN
            // 
            this.DonateBTN.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(42)))), ((int)(((byte)(84)))));
            this.DonateBTN.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.DonateBTN.Location = new System.Drawing.Point(1632, 924);
            this.DonateBTN.Name = "DonateBTN";
            this.DonateBTN.Size = new System.Drawing.Size(110, 43);
            this.DonateBTN.TabIndex = 19;
            this.DonateBTN.Text = "Donate";
            this.DonateBTN.UseVisualStyleBackColor = false;
            this.DonateBTN.Click += new System.EventHandler(this.DonateBTN_Click);
            // 
            // Form1
            // 
            this.ClientSize = new System.Drawing.Size(1754, 979);
            this.Controls.Add(this.DonateBTN);
            this.Controls.Add(this.allVideoDDB);
            this.Controls.Add(this.getVideosDDB);
            this.Controls.Add(this.webBrowser);
            this.Controls.Add(this.thumbCHK);
            this.Controls.Add(this.stopBTN);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.openFolderBTN);
            this.Controls.Add(this.SaveBTN);
            this.Controls.Add(this.downloadAllBTN);
            this.Controls.Add(this.expandBTN);
            this.Controls.Add(this.statusTXT);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.videoList);
            this.Controls.Add(this.userTXT);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.Text = "TikTok Downloader - Coolshrimp Modz";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            ((System.ComponentModel.ISupportInitialize)(this.videoList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.webBrowser)).EndInit();
            this.contextMenuStripvideos.ResumeLayout(false);
            this.contextMenuStripAllVideos.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private Button DonateBTN;
    }
}
