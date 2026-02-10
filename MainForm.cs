using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MP3te
{
    // --- Helper per Scrollbars Scure (Win10/11) ---
    public static class DarkModeCS
    {
        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam);

        public static void ApplyDarkTheme(Control ctl)
        {
            if (Environment.OSVersion.Version.Major >= 10)
            {
                SetWindowTheme(ctl.Handle, "DarkMode_Explorer", null);
                SendMessage(ctl.Handle, 0x000B, IntPtr.Zero, IntPtr.Zero); 
                SendMessage(ctl.Handle, 0x000B, (IntPtr)1, IntPtr.Zero);   
                ctl.Invalidate();
            }
        }
    }

    public class MainForm : Form
    {
        // Controls
        private Panel panelTopStatus; 
        private Panel panelLeftContainer; 
        
        private Label lblFileCount;
        private Button btnClean; // Rinominato da DEFAULT a CLEAN
        private TextBox txtPathBar;
        private Button btnGoPath;
        
        private TreeView treeDrives;
        private PictureBox picCover;
        
        private DataGridView gridMain;     
        private DataGridView gridDetails;  
        
        private ListBox listHistory;
        private RichTextBox rtxLog;

        // Layout Containers (Splitters)
        private SplitContainer splitMainVertical;       
        private SplitContainer splitCenterRightVertical; 
        
        private SplitContainer splitLeftHorizontal;     
        private SplitContainer splitCenterHorizontal;   
        private SplitContainer splitRightHorizontal;    

        // Colors
        private Color colorBack = Color.FromArgb(32, 32, 32);       
        private Color colorControl = Color.FromArgb(45, 45, 48);    
        private Color colorText = Color.FromArgb(220, 220, 220);    
        private Color colorBorder = Color.FromArgb(60, 60, 60);     
        private Color colorHighlight = Color.SteelBlue;             

        private string _currentDirectory = "";
        private List<Mp3Metadata> _currentFiles = new List<Mp3Metadata>();

        public MainForm()
        {
            InitializeComponents();
            
            this.Padding = new Padding(2); 
            
            SetupLayout();
            SetupEvents();
            
            Logger.RegisterControl(rtxLog);
            Logger.Info("Interface loaded. Dark Mode applied.");

            AppConfig.Load(); 
            LoadDrives();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            
            // 1. Applica Layout Default (Sempre) e Settings Colonne
            this.BeginInvoke(new Action(() => {
                SetDefaultLayout();
                LoadColumnSettings(); // Solo colonne, niente splitter
                ApplyThemeToControls();
            }));

            // 2. Ripristina ultimo percorso
            string lastPath = AppConfig.Get("LastPath", "");
            if (!string.IsNullOrEmpty(lastPath) && Directory.Exists(lastPath))
            {
                LoadFiles(lastPath);
                Task.Factory.StartNew(() => {
                    try { this.Invoke(new Action(() => SyncTreeWithPath(lastPath))); } catch {}
                });
            }
            
            RefreshHistoryList();
        }

        private void ApplyThemeToControls()
        {
            DarkModeCS.ApplyDarkTheme(treeDrives);
            DarkModeCS.ApplyDarkTheme(listHistory);
            DarkModeCS.ApplyDarkTheme(rtxLog);
            DarkModeCS.ApplyDarkTheme(gridMain); 
            DarkModeCS.ApplyDarkTheme(gridDetails);
            DarkModeCS.ApplyDarkTheme(txtPathBar);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveSettings(); // Salva solo path e colonne
            AppConfig.Save();
            base.OnFormClosing(e);
        }

        private void InitializeComponents()
        {
            this.Text = "MP3te - Dark Edition";
            this.Size = new Size(1300, 800);
            this.BackColor = colorBorder; 
            
            try {
                this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            } catch {
                this.Icon = SystemIcons.Application;
            }

            // --- BARRA DI STATO (Top) ---
            panelTopStatus = new Panel { 
                Dock = DockStyle.Top, Height = 30, BackColor = colorControl,
                Padding = new Padding(5)
            };
            
            lblFileCount = new Label { 
                Text = "Files: 0", Dock = DockStyle.Left, 
                ForeColor = Color.Cyan, AutoSize = true, TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Bold) 
            };

            // Tasto CLEAN
            btnClean = new Button {
                Text = "[CLEAN]", Dock = DockStyle.Right, Width = 80,
                FlatStyle = FlatStyle.Flat, ForeColor = Color.Orange,
                Cursor = Cursors.Hand, Font = new Font("Segoe UI", 8, FontStyle.Bold)
            };
            btnClean.FlatAppearance.BorderSize = 0;
            btnClean.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 60, 60);
            
            var toolTip = new ToolTip();
            toolTip.SetToolTip(btnClean, "Clean logs and history. Does not revert changes.");
            btnClean.Click += (s, e) => CleanSession();

            panelTopStatus.Controls.Add(lblFileCount);
            panelTopStatus.Controls.Add(btnClean);

            // --- CONTROLLI ---
            txtPathBar = new TextBox { 
                Dock = DockStyle.Fill, 
                BackColor = colorControl, ForeColor = colorText, 
                BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9)
            };
            btnGoPath = new Button {
                Text = "GO", Dock = DockStyle.Right, Width = 40,
                FlatStyle = FlatStyle.Flat, ForeColor = colorText, BackColor = colorBorder
            };
            btnGoPath.FlatAppearance.BorderSize = 0;
            
            var panelPathWrapper = new Panel { Dock = DockStyle.Top, Height = 25, Padding = new Padding(0,0,0,2), BackColor = colorBack };
            panelPathWrapper.Controls.Add(txtPathBar);
            panelPathWrapper.Controls.Add(btnGoPath);

            treeDrives = new TreeView { 
                Dock = DockStyle.Fill, BackColor = colorBack, ForeColor = colorText, 
                LineColor = colorText, BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9),
                ShowLines = true, FullRowSelect = true, ItemHeight = 22
            };

            picCover = new PictureBox { 
                Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, 
                BackColor = Color.FromArgb(20, 20, 20), BorderStyle = BorderStyle.None 
            };
            var ctxCover = new ContextMenuStrip();
            ctxCover.Items.Add("Change Cover...", null, (s, e) => ChangeCover());
            picCover.ContextMenuStrip = ctxCover;

            gridMain = CreateDarkGrid(false); 
            SetupGridColumnsMain();
            AttachColumnSelector(gridMain);

            gridDetails = CreateDarkGrid(true); 
            SetupGridColumnsDetails();
            AttachColumnSelector(gridDetails);

            listHistory = new ListBox { 
                Dock = DockStyle.Fill, BackColor = colorBack, ForeColor = Color.LightGray, 
                BorderStyle = BorderStyle.None, Font = new Font("Segoe UI", 8),
                SelectionMode = SelectionMode.MultiExtended, IntegralHeight = false
            };
            var ctxHistory = new ContextMenuStrip();
            ctxHistory.Items.Add("Undo Selected Changes", null, (s, e) => ExecuteUndo());
            listHistory.ContextMenuStrip = ctxHistory;
            
            rtxLog = new RichTextBox { 
                Dock = DockStyle.Fill, BackColor = Color.Black, ForeColor = Color.Lime, 
                Font = new Font("Consolas", 8), ReadOnly = true, BorderStyle = BorderStyle.None,
                DetectUrls = false
            };

            EventHandler loadAction = (s, e) => {
                if(Directory.Exists(txtPathBar.Text)) {
                    LoadFiles(txtPathBar.Text);
                    SyncTreeWithPath(txtPathBar.Text);
                }
            };
            txtPathBar.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { loadAction(s,e); e.SuppressKeyPress = true; }};
            btnGoPath.Click += loadAction;

            panelLeftContainer = new Panel { Dock = DockStyle.Fill, BackColor = colorBack };
            panelLeftContainer.Controls.Add(treeDrives);
            panelLeftContainer.Controls.Add(panelPathWrapper);
        }

        private DataGridView CreateDarkGrid(bool readOnly)
        {
            var dgv = new DataGridView { 
                Dock = DockStyle.Fill, BackColor = colorBack, ForeColor = colorText,
                BackgroundColor = colorBack, GridColor = colorBorder,
                BorderStyle = BorderStyle.None, EnableHeadersVisualStyles = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false, RowHeadersVisible = false,
                AllowUserToResizeRows = false, ReadOnly = readOnly,
                AllowUserToOrderColumns = true, AllowUserToAddRows = false, 
                Font = new Font("Segoe UI", 9),
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single 
            };
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle { 
                BackColor = colorControl, ForeColor = colorText, SelectionBackColor = colorControl, 
                Padding = new Padding(4), Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            dgv.DefaultCellStyle = new DataGridViewCellStyle { 
                BackColor = colorBack, ForeColor = colorText, SelectionBackColor = colorHighlight, 
                SelectionForeColor = Color.White 
            };
            return dgv;
        }

        private void SetupLayout()
        {
            splitMainVertical = CreateSplitter(Orientation.Vertical);
            splitCenterRightVertical = CreateSplitter(Orientation.Vertical);

            splitLeftHorizontal = CreateSplitter(Orientation.Horizontal);
            splitCenterHorizontal = CreateSplitter(Orientation.Horizontal);
            splitRightHorizontal = CreateSplitter(Orientation.Horizontal);

            splitLeftHorizontal.Panel1.Controls.Add(panelLeftContainer); 
            splitLeftHorizontal.Panel2.Controls.Add(picCover);           
            
            splitCenterHorizontal.Panel1.Controls.Add(gridMain);
            splitCenterHorizontal.Panel2.Controls.Add(listHistory);

            splitRightHorizontal.Panel1.Controls.Add(gridDetails);
            splitRightHorizontal.Panel2.Controls.Add(rtxLog);

            splitMainVertical.Panel1.Controls.Add(splitLeftHorizontal); 
            splitMainVertical.Panel2.Controls.Add(splitCenterRightVertical); 

            splitCenterRightVertical.Panel1.Controls.Add(splitCenterHorizontal); 
            splitCenterRightVertical.Panel2.Controls.Add(splitRightHorizontal);  

            this.Controls.Add(splitMainVertical);
            this.Controls.Add(panelTopStatus);
        }

        private SplitContainer CreateSplitter(Orientation orientation)
        {
            return new SplitContainer {
                Dock = DockStyle.Fill,
                Orientation = orientation,
                BackColor = colorBorder, 
                SplitterWidth = 2,
                Panel1MinSize = 50, 
                Panel2MinSize = 50, 
                Panel1 = { BackColor = colorBack, Padding = new Padding(0) }, 
                Panel2 = { BackColor = colorBack, Padding = new Padding(0) }
            };
        }

        // --- GESTIONE LAYOUT (FIXED DEFAULT) ---

        private void SetDefaultLayout()
        {
            int w = this.ClientSize.Width;
            int h = this.ClientSize.Height;
            if (w == 0) w = 1200;
            if (h == 0) h = 800;

            // 1. Larghezze (25% - 50% - 25%)
            int s1 = (int)(w * 0.25);
            SetSplitterPos(splitMainVertical, s1);

            // Calcola rimanente per il centro
            int remaining = w - s1;
            int s2 = (int)(remaining * 0.666); // 2/3 del rimanente (~50% del totale)
            SetSplitterPos(splitCenterRightVertical, s2);

            // 2. Altezze (75% Alto - 25% Basso)
            int hSplit = (int)(h * 0.75);
            SetSplitterPos(splitLeftHorizontal, hSplit);
            SetSplitterPos(splitCenterHorizontal, hSplit);
            SetSplitterPos(splitRightHorizontal, hSplit);
        }

        private void SetSplitterPos(SplitContainer sc, int pos)
        {
            int max = (sc.Orientation == Orientation.Vertical) ? sc.Width : sc.Height;
            if (pos < 50) pos = 50;
            if (pos > max - 50) pos = max - 50;
            try { sc.SplitterDistance = pos; } catch {}
        }

        private void SaveSettings()
        {
            AppConfig.Set("LastPath", _currentDirectory);
            // NON salviamo più gli splitter. Solo path e colonne.
            
            foreach(DataGridViewColumn col in gridMain.Columns) {
                AppConfig.Set("MainCol_W_" + col.Name, col.Width.ToString());
                AppConfig.Set("MainCol_V_" + col.Name, col.Visible.ToString());
            }
            foreach(DataGridViewColumn col in gridDetails.Columns) {
                AppConfig.Set("DetCol_W_" + col.Name, col.Width.ToString());
                AppConfig.Set("DetCol_V_" + col.Name, col.Visible.ToString());
            }
        }

        private void LoadColumnSettings()
        {
            ApplyGridSettings(gridMain, "MainCol");
            ApplyGridSettings(gridDetails, "DetCol");
            gridMain.Columns["FilePath"].Visible = false;
        }

        private void ApplyGridSettings(DataGridView dgv, string prefix)
        {
            foreach(DataGridViewColumn col in dgv.Columns) {
                int wCol = AppConfig.GetInt(prefix + "_W_" + col.Name, -1);
                if (wCol > 0) col.Width = wCol;
                col.Visible = AppConfig.GetBool(prefix + "_V_" + col.Name, true);
            }
        }

        // --- CLEAN SESSION ---
        private void CleanSession()
        {
            if (MessageBox.Show("Clean logs and history?", "Clean Session", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.No)
                return;

            listHistory.Items.Clear();
            rtxLog.Clear();
            HistoryStore.Clear();
            Logger.Clear();
            Logger.Info("Session cleaned.");
        }

        // --- LOGICA TREEVIEW, FILE, ETC. ---
        
        private void SyncTreeWithPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists) return;

            TreeNode currentNode = null;
            foreach (TreeNode root in treeDrives.Nodes) {
                if (path.StartsWith(root.Text, StringComparison.OrdinalIgnoreCase)) {
                    currentNode = root;
                    break;
                }
            }
            if (currentNode == null) return;
            currentNode.Expand();

            var parts = path.Substring(currentNode.Text.Length).Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts) {
                bool found = false;
                foreach (TreeNode child in currentNode.Nodes) {
                    if (child.Text.Equals(part, StringComparison.OrdinalIgnoreCase)) {
                        currentNode = child;
                        currentNode.Expand();
                        found = true;
                        break;
                    }
                }
                if (!found) break;
            }
            treeDrives.SelectedNode = currentNode;
            treeDrives.Select();
        }

        private void SetupGridColumnsMain()
        {
            gridMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "FilePath", Visible = false });
            gridMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "FileName", HeaderText = "File Name", ReadOnly = true });
            gridMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Title", HeaderText = "Title" });
            gridMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Artist", HeaderText = "Artist" });
            gridMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Album", HeaderText = "Album" });
            gridMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Year", HeaderText = "Year" });
            gridMain.Columns.Add(new DataGridViewTextBoxColumn { Name = "Genre", HeaderText = "Genre" });
        }

        private void SetupGridColumnsDetails()
        {
            gridDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "Bitrate", HeaderText = "Bitrate" });
            gridDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "Duration", HeaderText = "Duration" });
            gridDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "SampleRate", HeaderText = "Hz" });
            gridDetails.Columns.Add(new DataGridViewTextBoxColumn { Name = "Mode", HeaderText = "Mode" });
        }

        private void AttachColumnSelector(DataGridView dgv)
        {
            dgv.ColumnHeaderMouseClick += (s, e) => {
                if (e.Button == MouseButtons.Right) {
                    ContextMenuStrip mnu = new ContextMenuStrip();
                    foreach (DataGridViewColumn col in dgv.Columns) {
                        if (col.Name == "FilePath") continue;
                        var item = new ToolStripMenuItem(col.HeaderText);
                        item.Checked = col.Visible;
                        item.CheckOnClick = true;
                        item.Click += (obj, args) => { col.Visible = item.Checked; };
                        mnu.Items.Add(item);
                    }
                    mnu.Show(Cursor.Position);
                }
            };
        }

        private void SetupEvents()
        {
            treeDrives.BeforeExpand += TreeDrives_BeforeExpand;
            treeDrives.AfterSelect += TreeDrives_AfterSelect;
            gridMain.CellEndEdit += GridMain_CellEndEdit;
            gridMain.SelectionChanged += GridMain_SelectionChanged;
            listHistory.KeyDown += ListHistory_KeyDown;
            listHistory.DoubleClick += ListHistory_DoubleClick;
        }

        private void LoadDrives()
        {
            treeDrives.Nodes.Clear();
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady)
                {
                    TreeNode node = new TreeNode(drive.Name) { Tag = drive.RootDirectory.FullName };
                    node.Nodes.Add("...");
                    treeDrives.Nodes.Add(node);
                }
            }
        }

        private void TreeDrives_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            if (e.Node.Nodes.Count == 1 && e.Node.Nodes[0].Text == "...")
            {
                e.Node.Nodes.Clear();
                string path = (string)e.Node.Tag;
                try
                {
                    foreach (var dir in Directory.GetDirectories(path))
                    {
                        var info = new DirectoryInfo(dir);
                        if ((info.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden) continue;
                        TreeNode node = new TreeNode(Path.GetFileName(dir)) { Tag = dir };
                        node.Nodes.Add("...");
                        e.Node.Nodes.Add(node);
                    }
                }
                catch {}
            }
        }

        private void TreeDrives_AfterSelect(object sender, TreeViewEventArgs e)
        {
            string path = (string)e.Node.Tag;
            if (_currentDirectory != path)
                LoadFiles(path);
        }

        private void LoadFiles(string dirPath)
        {
            if (!Directory.Exists(dirPath)) return;
            
            _currentDirectory = dirPath; 
            txtPathBar.Text = dirPath;
            
            gridMain.Rows.Clear();
            gridDetails.Rows.Clear();
            _currentFiles.Clear();
            picCover.Image = null;

            try
            {
                var files = Directory.GetFiles(dirPath, "*.mp3");
                lblFileCount.Text = "Files: " + files.Length;
                
                foreach (var file in files)
                {
                    var meta = TagEngine.ReadTag(file);
                    _currentFiles.Add(meta);

                    gridMain.Rows.Add(meta.FilePath, Path.GetFileName(file), meta.Title, meta.Artist, meta.Album, meta.Year, meta.Genre);
                    gridDetails.Rows.Add(meta.Bitrate, meta.Duration, meta.SampleRate, meta.Mode);
                }
                Logger.Info("Loaded " + files.Length + " files from: " + dirPath);
            }
            catch (Exception ex) { Logger.Error("Load error: " + ex.Message); }
        }

        private void GridMain_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            string filePath = gridMain.Rows[e.RowIndex].Cells["FilePath"].Value.ToString();
            string colName = gridMain.Columns[e.ColumnIndex].Name;
            var valObj = gridMain.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
            string newValue = valObj != null ? valObj.ToString() : "";
            
            var meta = _currentFiles.FirstOrDefault(m => m.FilePath == filePath);
            if (meta == null) return;
            
            string oldValue = "";
            switch(colName) {
                case "Title": oldValue = meta.Title; meta.Title = newValue; break;
                case "Artist": oldValue = meta.Artist; meta.Artist = newValue; break;
                case "Album": oldValue = meta.Album; meta.Album = newValue; break;
                case "Year": oldValue = meta.Year.ToString(); meta.Year = uint.Parse(newValue); break;
                case "Genre": oldValue = meta.Genre; meta.Genre = newValue; break;
            }
            
            if (oldValue != newValue) {
                TagEngine.WriteSingleTag(filePath, colName, newValue);
                HistoryStore.RecordChange(filePath, colName, oldValue, newValue);
                RefreshHistoryList();
            }
        }

        private void GridMain_SelectionChanged(object sender, EventArgs e)
        {
            if (gridMain.SelectedRows.Count == 0) return;
            
            var row = gridMain.SelectedRows[0];
            int idx = row.Index;
            
            if (gridDetails.Rows.Count > idx && idx >= 0) {
                gridDetails.ClearSelection();
                gridDetails.Rows[idx].Selected = true;
            }

            var cellPath = row.Cells["FilePath"];
            if (cellPath == null || cellPath.Value == null) return;
            string path = cellPath.Value.ToString();

            try {
                var img = TagEngine.GetCover(path);
                if (picCover.Image != null) { var old = picCover.Image; picCover.Image = null; old.Dispose(); }
                picCover.Image = img;
            } catch { picCover.Image = null; }
        }

        private void ChangeCover()
        {
            if (gridMain.SelectedRows.Count == 0) return;
            string path = gridMain.SelectedRows[0].Cells["FilePath"].Value.ToString();
            using (var ofd = new OpenFileDialog { Filter = "Images|*.jpg;*.png" }) {
                if (ofd.ShowDialog() == DialogResult.OK) {
                    TagEngine.SetCover(path, ofd.FileName);
                    GridMain_SelectionChanged(null, null);
                }
            }
        }

        private void RefreshHistoryList()
        {
            listHistory.Items.Clear();
            var history = HistoryStore.Load();
            foreach(var entry in history) listHistory.Items.Add(entry);
            if (listHistory.Items.Count > 0) listHistory.TopIndex = listHistory.Items.Count - 1;
        }

        private void ExecuteUndo()
        {
            if (listHistory.SelectedItems.Count == 0) return;
            if (MessageBox.Show("Undo changes?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                var entries = new List<HistoryEntry>();
                foreach (var item in listHistory.SelectedItems) {
                    var entry = item as HistoryEntry;
                    if(entry != null) entries.Add(entry);
                }
                HistoryStore.UndoEntries(entries);
                RefreshHistoryList();
                if (Directory.Exists(_currentDirectory)) LoadFiles(_currentDirectory);
            }
        }

        private void ListHistory_KeyDown(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Delete) ExecuteUndo(); }
        private void ListHistory_DoubleClick(object sender, EventArgs e) { ExecuteUndo(); }
    }
}