using uBasicForm.Properties;
using uBasicLibrary;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using TracerLibrary;

namespace uBasicForm
{
    public partial class uBasicUI : Form
    {
        #region Fields

        // Prepare Basic
        static IDefaultIO _displayIO = null;
        IInterpreter _basic = null;
        int _pos = 0;
        bool _stopped = true;
        bool _paused = false;


        // Declare a delegate used to communicate with the UI thread
        public delegate void UpdateTextDelegate();
        private UpdateTextDelegate updateTextDelegate = null;

        // Declare our worker thread
        private Thread _workerThread = null;

        // Manage the inputs
        string value = "";

        // Most recently used
        protected MruStripMenu _mruMenu;

        #endregion
        #region Constructor

        public uBasicUI(string path, string name)
        {
            Debug.WriteLine("In ConsoleForm()");

            InitializeComponent();

            _displayIO = new TextBoxIO();
            _displayIO.TextReceived += new EventHandler<TextEventArgs>(OnMessageReceived);

            // Initialise the delegate
            this.updateTextDelegate = new UpdateTextDelegate(this.UpdateText);

            // Add most recent used
            _mruMenu = new MruStripMenuInline(fileMenuItem, recentFileToolStripMenuItem, new MruStripMenu.ClickedHandler(OnMruFile), 4);
            LoadFileList();

            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            this.Text = "uBasic " + version;

            if ((path.Length > 0) && (name.Length > 0))
            {
                consoleTextBox.Text = "";
                this.Text = "uBasic " + version + " - " + name;

                string filenamePath = "";
                filenamePath = path + Path.DirectorySeparatorChar + name + ".bas";
                byte[] program;
                byte[] memory;

                Thread.Sleep(100);
                try
                {
                    using (StreamReader sr = new StreamReader(filenamePath))
                    {
                        string text = sr.ReadToEnd();
                        program = System.Text.Encoding.ASCII.GetBytes(text);
                    }

                    _mruMenu.AddFile(filenamePath);
                    UInt32 size = 4096;
                    memory = new byte[size];
                    _basic = new uBasic(memory, size, _displayIO);
                    _basic.Init();
                    _basic.Load(program);

                    _stopped = false;
                    this._workerThread = new Thread(new ThreadStart(this.Run));
                    _displayIO.Reset();
                    this._workerThread.Start();
                    consoleTextBox.Visible = true;
                    consoleTextBox.Enabled = true;
                }
                catch (Exception e1)
                {
                    TraceInternal.TraceError(e1.ToString());
                }
            }
            Debug.WriteLine("Out ConsoleForm()");
        }

        #endregion
        #region Private

        private void OnMruFile(int number, String filenamePath)
        {
            string path = "";
            string filename = "";

            consoleTextBox.Enabled = false;
            consoleTextBox.Visible = false;
            if (_stopped == false)
            {
                _basic.Stop();
                if (_workerThread != null && _workerThread.IsAlive)
                {
                    _workerThread.Join(1000);
                }
            }

            if (File.Exists(filenamePath) == true)
            {
                _mruMenu.SetFirstFile(number);
                _pos = filenamePath.LastIndexOf('\\');
                if (_pos > 0)
                {
                    path = filenamePath.Substring(0, _pos);
                    filename = filenamePath.Substring(_pos + 1, filenamePath.Length - _pos - 1);
                }
                else
                {
                    path = filenamePath;
                }
                TraceInternal.TraceInformation("Use Name=" + filename);
                TraceInternal.TraceInformation("Use Path=" + path);

                consoleTextBox.Text = "";
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                this.Text = "uBasic " + version + " - " + filename;

                filenamePath = path + Path.DirectorySeparatorChar + filename;
                byte[] program;
                byte[] memory;

                Thread.Sleep(100);
                try
                {
                    using (StreamReader sr = new StreamReader(filenamePath))
                    {
                        string text = sr.ReadToEnd();
                        program = System.Text.Encoding.ASCII.GetBytes(text);
                    }

                    // Define some memory

                    UInt32 size = 4096;
                    memory = new byte[size];
                    _basic = new uBasic(memory, size, _displayIO);
                    _basic.Init();
                    _basic.Load(program);

                    _stopped = false;
                    this._workerThread = new Thread(new ThreadStart(this.Run));
                    _displayIO.Reset();
                    this._workerThread.Start();
                    consoleTextBox.Visible = true;
                    consoleTextBox.Enabled = true;
                }
                catch (Exception e1)
                {
                    TraceInternal.TraceError(e1.ToString());
                }
            }
            else
            {
                _mruMenu.RemoveFile(number);
            }
        }

        private void UpdateText()
        {
            string output = _displayIO.Output;
            if (output.Length > 0)
            {
                this.consoleTextBox.AppendText(output);
                //int position = consoleTextBox.SelectionLength;
                double position = consoleTextBox.TextLength;
                double width = consoleTextBox.Width / 8;   // assume a fix width font
                double top = Math.Floor(position / width);
                _displayIO.CursorTop = (int)top;
                double left = position - width * Math.Floor(position / width);
                _displayIO.CursorLeft = (int)left;
            }
        }

        // Define the event handlers.
        private void OnMessageReceived(object source, TextEventArgs e)
        {
            if (e.Text.Length > 0)
            {
                this.Invoke(this.updateTextDelegate);
            }
        }

        private void Run()
        {
            try
            {
                do
                {
                    if (!_stopped && !_paused)
                    {
                        _basic.Run();
                    }
                } while (!_basic.IsFinished() && !_stopped);
                _stopped = true;
            }
            catch (Exception e)
            {
                TraceInternal.TraceVerbose(e.ToString());
            }
        }

        /// <summary>
        /// Intercept the key press events and manage the content
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConsoleTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            char chr = e.KeyChar;
            if (chr == '\r')
            {
                value += chr;
                _displayIO.Input = value;
                this.consoleTextBox.AppendText("\r" + "\n");
                value = "";
            }
            else if (chr == '\b')
            {
                if (value.Length > 0)
                {
                    this.consoleTextBox.Text = this.consoleTextBox.Text.Substring(0, this.consoleTextBox.Text.Length - 1);
                    this.consoleTextBox.SelectionStart = consoleTextBox.Text.Length;
                    this.consoleTextBox.ScrollToCaret();
                    value = value.Substring(0, value.Length - 1);
                }
            }
            else
            {
                value += chr;
                this.consoleTextBox.AppendText(Convert.ToString(chr));
            }
        }

        private void FileOpenMenuItem_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("In FileOpenMenuItem_Click()");

            string path = "";
            string filename = "";

            consoleTextBox.Enabled = false;
            consoleTextBox.Visible = false;
            if (_stopped == false)
            {
                _basic.Stop();
                if (_workerThread != null && _workerThread.IsAlive)
                {
                    _workerThread.Join(1000);
                }
            }

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "uBasic (*.bas)|*.bas",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filenamePath = openFileDialog.FileName;
                _pos = filenamePath.LastIndexOf('\\');
                if (_pos > 0)
                {
                    path = filenamePath.Substring(0, _pos);
                    filename = filenamePath.Substring(_pos + 1, filenamePath.Length - _pos - 1);
                }
                else
                {
                    filename = filenamePath;
                }
                TraceInternal.TraceInformation("Use Name=" + filename);
                TraceInternal.TraceInformation("Use Path=" + path);

                consoleTextBox.Text = "";
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                this.Text = "uBasic " + version + " - " + filename;

                filenamePath = path + Path.DirectorySeparatorChar + filename;
                byte[] program;
                byte[] memory;

                Thread.Sleep(100);
                try
                {
                    using (StreamReader sr = new StreamReader(filenamePath))
                    {
                        string text = sr.ReadToEnd();
                        program = System.Text.Encoding.ASCII.GetBytes(text);
                    }
                    _mruMenu.AddFile(filenamePath);
                    UInt32 size = 4096;
                    memory = new byte[size];
                    _basic = new uBasic(memory, size, _displayIO);
                    _basic.Init();
                    _basic.Load(program);

                    _stopped = false;
                    this._workerThread = new Thread(new ThreadStart(this.Run));
                    _displayIO.Reset();
                    this._workerThread.Start();
                    consoleTextBox.Visible = true;
                    consoleTextBox.Enabled = true;
                }
                catch (Exception e1)
                {
                    TraceInternal.TraceError(e1.ToString());
                }
            }
            Debug.WriteLine("Out FileOpenMenuItem_Click()");
        }

        private void FormatFontMenuItem_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("In FormatFontMenuItem_Click()");
            FontDialog fontDialog = new FontDialog
            {
                Font = consoleTextBox.Font,
                ShowColor = true,
                Color = consoleTextBox.ForeColor
            };

            if (fontDialog.ShowDialog() == DialogResult.OK)
            {
                Font font = fontDialog.Font;
                Color color = fontDialog.Color;
                consoleTextBox.Font = font;
                consoleTextBox.ForeColor = color;
                Properties.Settings.Default.ConsoleFont = font;
                Properties.Settings.Default.ConsoleFontColor = color;
                // Save settings
                Settings.Default.Save();
            }
            Debug.WriteLine("Out FormatFontMenuItem_Click()");
        }

        private void ConsoleForm_Load(object sender, EventArgs e)
        {
            Debug.WriteLine("In ConsoleForm_Load()");

            Settings.Default.Upgrade();

            // Set window location

            // fix errors with location being negative or off the main display

            this.Location = Settings.Default.ConsoleLocation;
            if ((this.Location.X < 0) || (this.Location.Y < 0))
            {
                this.Location = new Point(0, 0);
            }

            this.WindowState = FormWindowState.Normal;

            // Fixed windows size

            this.Width = _displayIO.Width;

            // Set window size

            this.Size = Settings.Default.ConsoleSize;
            

            // Set Console font
            if (Settings.Default.ConsoleFont != null)
            {
                this.consoleTextBox.Font = Settings.Default.ConsoleFont;
            }

            // Set Console font colour
            this.consoleTextBox.ForeColor = Settings.Default.ConsoleFontColor;
            

            // Set Console colour
            this.consoleTextBox.BackColor = Settings.Default.ConsoleColor;

            Debug.WriteLine("Out ConsoleForm_Load()");

        }

        private void ConsoleForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Debug.WriteLine("In ConsoleForm_FormClosing()");

            if (_stopped == false)
            {
                _displayIO.Input = "\r\n";
                _basic.Stop();
                if (_workerThread != null && _workerThread.IsAlive)
                {
                    _workerThread.Join(1000);
                }
            }

            // Copy window location to app settings
            Settings.Default.ConsoleLocation = this.Location;

            // Copy window size to app settings
            if (this.WindowState == FormWindowState.Normal)
            {
                Settings.Default.ConsoleSize = this.Size;
            }
            else
            {
                Settings.Default.ConsoleSize = this.RestoreBounds.Size;
            }

            // Copy console font type to app settings
            Settings.Default.ConsoleFont = this.consoleTextBox.Font;

            // Copy console font colour to app settings
            Settings.Default.ConsoleFontColor = this.consoleTextBox.ForeColor;

            // Copy console colour to app settings
            Settings.Default.ConsoleColor = this.consoleTextBox.BackColor;

            // Safe Mru
            SaveFileList();

            // Save settings
            Settings.Default.Save();

            // Upgrade settings
            Settings.Default.Reload();

            Debug.WriteLine("Out ConsoleForm_FormClosing()");
        }

        private void FileExitMenuItem_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("Out FileExitMenuItem_Click()");
            this.Close();
        }

        private void FormatColorMenuItem_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("In FileExitMenuItem_Click()");
            ColorDialog colorDialog = new ColorDialog
            {
                Color = consoleTextBox.BackColor
            };

            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                Color color = colorDialog.Color;
                consoleTextBox.BackColor = color;
                Properties.Settings.Default.ConsoleColor = color;
            }
            Debug.WriteLine("Out FileExitMenuItem_Click()");
        }

        private void LoadFileList()
        {
            Debug.WriteLine("In LoadFileList()");
            TraceInternal.TraceVerbose("Files " + Properties.Settings.Default.FileCount);
            for (int i = 0; i < 4; i++)
            {
                string property = "File" + (i + 1);
                string file = (string)Properties.Settings.Default[property];
                if (file != "")
                {
                    _mruMenu.AddFile(file);
                    TraceInternal.TraceVerbose("Load " + file);
                }
            }
            Debug.WriteLine("Out LoadFiles()");
        }

        private void SaveFileList()
        {
            Debug.WriteLine("In SaveFileList");
            string[] files = _mruMenu.GetFiles();
            Properties.Settings.Default["FileCount"] = files.Length;
            TraceInternal.TraceVerbose("Files=" + files.Length);
            for (int i = 0; i < 4; i++)
            {
                string property = "File" + (i + 1);
                if (i < files.Length)
                {
                    Properties.Settings.Default[property] = files[i];
                    TraceInternal.TraceVerbose("Save " + property + "=" + files[i]);
                }
                else
                {
                    Properties.Settings.Default[property] = "";
                    TraceInternal.TraceVerbose("Save " + property + "=");
                }
            }
            Debug.WriteLine("Out SaveFiles");
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("In saveToolStripMenuItem_Click()");

            try
            {
                // Find next available snapshot number
                string snapshotDir = Path.Combine(Application.StartupPath, "snapshots");
                if (!Directory.Exists(snapshotDir))
                {
                    Directory.CreateDirectory(snapshotDir);
                }

                int snapshotNum = 1;
                string snapshotPath;
                do
                {
                    snapshotPath = Path.Combine(snapshotDir, $"snapshot_{snapshotNum:D3}.bin");
                    snapshotNum++;
                } while (File.Exists(snapshotPath));

                // SaveMemory handles pause/resume internally
                SaveMemory(snapshotPath);
                TraceInternal.TraceInformation($"Memory snapshot saved to {snapshotPath}");

                // Brief status update (non-blocking)
                string[] parts = this.Text.Split(new string[] { " - " }, 2, StringSplitOptions.None);
                this.Text = parts[0] + " - " + Path.GetFileName(snapshotPath);
            }
            catch (Exception ex)
            {
                TraceInternal.TraceError($"Save memory error: {ex.Message}");
                MessageBox.Show($"Error saving memory: {ex.Message}", 
                              "Save Error", 
                              MessageBoxButtons.OK, 
                              MessageBoxIcon.Error);
            }

            Debug.WriteLine("Out saveToolStripMenuItem_Click()");
        }

        private void loadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("In loadToolStripMenuItem_Click()");

            try
            {
                // Load from most recent snapshot
                string snapshotDir = Path.Combine(Application.StartupPath, "snapshots");
                if (!Directory.Exists(snapshotDir))
                {
                    MessageBox.Show("No snapshots found. Create a snapshot first using File > Save.",
                                  "No Snapshots",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Information);
                    return;
                }

                // Find most recent snapshot
                string[] snapshots = Directory.GetFiles(snapshotDir, "snapshot_*.bin");
                if (snapshots.Length == 0)
                {
                    MessageBox.Show("No snapshots found. Create a snapshot first using File > Save.",
                                  "No Snapshots",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Information);
                    return;
                }

                Array.Sort(snapshots);
                string latestSnapshot = snapshots[snapshots.Length - 1];

                LoadMemory(latestSnapshot);

                // Clear display after loading new state
                consoleTextBox.Clear();

                TraceInternal.TraceInformation($"Memory loaded from {latestSnapshot}");

                // Update title to show loaded snapshot
                string[] parts = this.Text.Split(new string[] { " - " }, 2, StringSplitOptions.None);
                this.Text = parts[0] + " - Loaded: " + Path.GetFileName(latestSnapshot);
            }
            catch (Exception ex)
            {
                TraceInternal.TraceError($"Load memory error: {ex.Message}");
                MessageBox.Show($"Error loading memory: {ex.Message}",
                              "Load Error",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
            }

            Debug.WriteLine("Out loadToolStripMenuItem_Click()");
        }

        /// <summary>
        /// Load a previously saved memory state from a binary file.
        /// This restores both the memory contents and reinitializes the interpreter's internal state.
        /// </summary>
        /// <param name="filePath">Path to the memory dump file</param>
        public void LoadMemory(string filePath)
        {
            Debug.WriteLine($"In uBasicUI.LoadMemory({filePath})");
            _paused = true;
            try
            {
                // Give current Run() call time to finish
                Thread.Sleep(500);

                byte[] loadedMemory = File.ReadAllBytes(filePath);
                if (loadedMemory.Length != _basic.Size)
                {
                    throw new InvalidOperationException(
                        $"Memory size mismatch: file={loadedMemory.Length}, expected={_basic.Size}");
                }

                _basic.Memory = loadedMemory;

                TraceInternal.TraceInformation($"Loaded {_basic.Size} bytes from {filePath} and reinitialized interpreter");
            }
            catch (Exception ex)
            {
                TraceInternal.TraceError($"LoadMemory error: {ex.Message}");
                throw;
            }
            finally
            {
                _paused = false;
            }
            Debug.WriteLine("Out uBasicUI.LoadMemory()");
        }

        /// <summary>
        /// Save the entire interpreter memory state to a binary file
        /// </summary>
        /// <param name="filePath">Path to save the memory dump</param>
        public void SaveMemory(string filePath)
        {
            Debug.WriteLine($"In uBasic.SaveMemory({filePath})");
            _paused = true;
            try
            {
                // Give current Run() call time to finish
                Thread.Sleep(500);

                File.WriteAllBytes(filePath, _basic.Memory);
                TraceInternal.TraceInformation($"Saved {_basic.Size} bytes to {filePath}");
            }
            catch (Exception ex)
            {
                TraceInternal.TraceError($"SaveMemory error: {ex.Message}");
                throw;
            }
            finally
            {
                _paused = false;
            }
            Debug.WriteLine("Out uBasic.SaveMemory()");
        }

        #endregion

    }
}