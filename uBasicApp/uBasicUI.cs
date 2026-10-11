using DisplayLibrary;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using TracerLibrary;
using uBasicApp.Properties;
using uBasicLibrary;
using ColourLibrary;

namespace uBasicApp
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


        // display updating fix

        bool _updated = false;
        int _height = 32;
        int _width = 32;
        int _scale = 1;
        int _aspect = 1;


        // Declare a delegate used to communicate with the UI thread
        public delegate void UpdateTextDelegate();
        private UpdateTextDelegate updateTextDelegate = null;

        // Declare our worker thread
        private Thread _workerThread = null;

        // Manage the inputs
        string value = "";

        // Most recently used
        protected MruStripMenu _mruMenu;

        // Use monochrome display

        MonochromeTextDisplay _mtd;

        // Keyboard matrix

        KeyboardMapper _matrix;

        string _lineBuffer = "";

        #endregion
        #region Constructor

        public uBasicUI(string path, string name)
        {
            Debug.WriteLine("In ConsoleApp()");

            InitializeComponent();

            // Initialise the display

            _width = 32;
            _height = 32;
            _scale = 2;
            _aspect = 1;

            _mtd = new MonochromeTextDisplay(_width, _height, _scale, _aspect);

            ROMFont rasterFont = new ROMFont();
            string fileName = "IBM_VGA_8x8.bin";
            string fileNamePath = Path.Combine(path, fileName);
            rasterFont.Load(fileNamePath);

            _mtd.Font = rasterFont;
            _mtd.Set(0, 0);
            _mtd.ForegroundColour = new SolidColour(0, 160, 0);          // Dark Green foreground
            _mtd.BackgroundColour = new SolidColour(0, 0, 0);                // Black background
            _mtd.Clear(); // Calls generate internally

            _displayIO = new DisplayIO();
            _displayIO.TextReceived += new EventHandler<TextEventArgs>(OnMessageReceived);

            // Initialise the delegate
            this.updateTextDelegate = new UpdateTextDelegate(this.UpdateText);

            // Fit the picturebox to the form

            consolePictureBox.Select();

            // Fix the form size

            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            SetLimits();


            // Add most recent used
            _mruMenu = new MruStripMenuInline(fileMenuItem, recentFileToolStripMenuItem, new MruStripMenu.ClickedHandler(OnMruFile), 4);
            LoadFileList();

            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            this.Text = "uBasic " + version;

            if ((path.Length > 0) && (name.Length > 0))
            {
                //consoleTextBox.Text = "";
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
                    consolePictureBox.Visible = true;
                    consolePictureBox.Enabled = true;
                }
                catch (Exception e1)
                {
                    TraceInternal.TraceError(e1.ToString());
                }
            }
            Debug.WriteLine("Out ConsoleApp()");
        }

        #endregion
        #region Private

        private void OnMruFile(int number, String filenamePath)
        {
            string path = "";
            string filename = "";

            consolePictureBox.Enabled = false;
            consolePictureBox.Visible = false;
            if (_stopped == false)
            {
                _basic.Stop();  // This will cause the worker thread to exit the Run() loop and finish
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

                //consoleTextBox.Text = "";
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
                    UInt32 size = 4096;
                    memory = new byte[size];
                    _basic = new uBasic(memory, size, _displayIO);
                    _basic.Init();
                    _basic.Load(program);

                    _stopped = false;
                    _paused = false;
                    this._workerThread = new Thread(new ThreadStart(this.Run));
                    _displayIO.Reset();
                    this._workerThread.Start();
                    consolePictureBox.Visible = true;
                    consolePictureBox.Enabled = true;
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
                foreach (char c in output)
                {
                    if (c == '\n')
                    {
                        int row = _mtd.Row;
                        if (row == _mtd.Height - 1)
                        {
                            _mtd.Scroll();
                            _mtd.Set(0, row);
                            _mtd.Generate();
                        }
                        else
                        {
                            _mtd.Set(0, row + 1);
                        }

                    }
                    else if (c == '\r')
                    {
                        _mtd.Set(0, _mtd.Row);
                    }
                    else
                    {
                        _mtd.Write((byte)c);
                    }
                }
                consolePictureBox.Invalidate();
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


        private void FileOpenMenuItem_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("In FileOpenMenuItem_Click()");

            string path = "";
            string filename = "";

            consolePictureBox.Enabled = false;
            consolePictureBox.Visible = false;
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

                //_mtd.Clear(); // Calls generate internally
                Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                this.Text = "uBasic " + version + " - " + filename;

                filenamePath = path + Path.DirectorySeparatorChar + filename;
                byte[] memory;
                byte[] program;

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
                    IInterpreter basic = new uBasic(memory, size, _displayIO);
                    _basic = new uBasic(memory, size, _displayIO);
                    _basic.Init();
                    _basic.Load(program);

                    _stopped = false;
                    _paused = false;
                    this._workerThread = new Thread(new ThreadStart(this.Run));
                    _displayIO.Reset();
                    this._workerThread.Start();
                    consolePictureBox.Visible = true;
                    consolePictureBox.Enabled = true;
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
            //FontDialog fontDialog = new FontDialog
            //{
            //    Font = consoleTextBox.Font,
            //    ShowColor = true,
            //    Color = consoleTextBox.ForeColor
            //};

            //if (fontDialog.ShowDialog() == DialogResult.OK)
            //{
            //    Font font = fontDialog.Font;
            //    Color color = fontDialog.Color;
            //    consoleTextBox.Font = font;
            //    consoleTextBox.ForeColor = color;
            //    Properties.Settings.Default.ConsoleFont = font;
            //    Properties.Settings.Default.ConsoleFontColor = color;
            //    // Save settings
            //    Settings.Default.Save();
            //}
            Debug.WriteLine("Out FormatFontMenuItem_Click()");
        }

        private void ConsoleForm_Load(object sender, EventArgs e)
        {
            Debug.WriteLine("In ConsoleForm_Load()");

            Settings.Default.Upgrade();

            // Set window location

            // fIx errors with location being negative or off the main display

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
                //this.consoleTextBox.Font = Settings.Default.ConsoleFont;
            }

            // Set Console font colour
            if (Settings.Default.ConsoleFontColor != Color.Black)
            {
                //this.consoleTextBox.ForeColor = Settings.Default.ConsoleFontColor;
            }

            // Set Console colour
            if (Settings.Default.ConsoleColor != Color.White)
            {
                //this.consoleTextBox.BackColor = Settings.Default.ConsoleColor;
            }

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
            //Settings.Default.ConsoleFont = this.consoleTextBox.Font;

            // Copy console font colour to app settings
            //Settings.Default.ConsoleFontColor = this.consoleTextBox.ForeColor;

            // Copy console colour to app settings
            //Settings.Default.ConsoleColor = this.consoleTextBox.BackColor;

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
                //Color = _mtd.BackgroundColour.ToUInt32()
            };

            if (colorDialog.ShowDialog() == DialogResult.OK)
            {
                Color color = colorDialog.Color;
                _mtd.BackgroundColour = new SolidColour(0, 0, 0);                // Black background
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

        private void ConsolePictureBox_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Bitmap b = _mtd.Bitmap;
            g.DrawImageUnscaled(b, 0, 0);
        }

        private void ConsolePictureBox_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {

            byte key = _matrix.ToASCII(e.KeyValue, e.Shift, e.Control, e.Alt);

            if (key > 0)
            {
                if (key == 27)  // Escape
                {
                    _mtd.Clear();    // Calls generate internally
                    _mtd.Set(0, 0);
                    _lineBuffer = "";
                }
                else if (key == 13) // Carriage Return
                {
                    // Not sure if the scroll should trigger
                    int row = _mtd.Row;
                    if (row == _mtd.Height - 1)
                    {
                        _mtd.Scroll();
                        _mtd.Set(0, row);
                        _mtd.Generate();
                    }
                    else
                    {
                        _mtd.Set(0, row + 1);
                    }

                    // At this stage the buffer would be sent to a command processor

                    _lineBuffer = "";

                }
                else if (key == 8)  // Backspace
                {
                    int column = _mtd.Column;
                    int row = _mtd.Row;
                    if (_lineBuffer.Length > 0)
                    {
                        if (column > 0)
                        {
                            _mtd.Column = column - 1;
                            _mtd.Put(32);  // Calls Generate internally
                        }
                        else
                        {
                            // At start of line - need to move to end of previous line
                            if (row > 0)
                            {
                                _mtd.Row = row - 1;
                                _mtd.Column = _mtd.Width - 1;
                                _mtd.Put(32);  // Calls Generate internally

                            }
                        }
                        _lineBuffer = _lineBuffer.Substring(0, _lineBuffer.Length - 1);
                    }
                }
                else
                {
                    int column = _mtd.Column;
                    int row = _mtd.Row;
                    if (column >= _mtd.Width)
                    {
                        row += 1;
                        column = 0;
                        _mtd.Set(column, row);
                        if (row == _mtd.Height)
                        {
                            _mtd.Scroll();
                            row -= 1;
                            _mtd.Set(column, row);
                            _mtd.Generate();
                        }
                    }
                    _mtd.Put(key, new SolidColour(255, 0, 0), new SolidColour(0, 255, 0)); // // Red on Green Calls generate internally
                    _mtd.Set(column + 1, row);

                    _lineBuffer += (char)key;           // Append to line buffer
                }
                consolePictureBox.Invalidate();
            }
        }

        private void SetLimits()
        {
            int hbit = _mtd.Font.Horizontal;
            int vbit = _mtd.Font.Vertical;
            _mtd.Scale = _scale;
            _mtd.Aspect = _aspect;
            int height = 39 + this.MainMenuStrip.Height;    // Must be some fixed elements to the form
            int width = 16;                                 // Must have some fixed element to the form (8 pixels each side)
            if (_aspect > 1)
            {
                height = height + _scale * _height * 8;
                width = width + _scale * _width * _aspect * 8;
            }
            else
            {
                height = height + _scale * _height * 8 / _aspect;
                width = width + _scale * _width * _aspect * 8;
            }

            this.MinimumSize = new Size(width, height); // 40
            this.MaximumSize = new Size(width, height);

            this.consolePictureBox.Invalidate();
        }

        private void ConsolePiciureBox_Click(object sender, EventArgs e)
        {
            this.consolePictureBox.Invalidate();
        }

        private void ActionStopMenuItem_Click(object sender, EventArgs e)
        {
            if (_stopped == false)
            {
                _basic.Stop();
                _stopped = true;
            }
        }

        private void ActionStartMenuItem_Click(object sender, EventArgs e)
        {
            if (_stopped == true)
            {
                _basic.Reset();
                _stopped = false;
                Run();
                if (_workerThread != null && _workerThread.IsAlive)
                {
                    _workerThread.Join(1000);
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Debug.WriteLine("In saveToolStripMenuItem_Click()");

            try
            {
                // Check if interpreter is initialized
                if (_basic != null)
                {

                    // This is a simple memory dump of the entire interpreter state.
                    // It can be loaded back to restore the state of the interpreter.
                    // This is not a checkpoint its a hibernate.

                    string fileName = "memory_dump.bin";
                    _paused = true;
                    try
                    {
                        // Give current Run() call time to finish
                        Thread.Sleep(500);

                        File.WriteAllBytes(fileName, _basic.Memory);
                        TraceInternal.TraceInformation($"Saved {_basic.Size} bytes to {fileName}");
                    }
                    catch (Exception ex)
                    {
                        TraceInternal.TraceError($"SaveMemory error: {ex.Message}");
                        throw;
                    }
                    finally
                    {
                        _basic.Stop();
                    }
                }

                //SaveFileDialog saveFileDialog = new SaveFileDialog
                //{
                //    Filter = "Memory Dump (*.bin)|*.bin|All Files (*.*)|*.*",
                //    FilterIndex = 1,
                //    FileName = "memory_dump.bin",
                //    RestoreDirectory = true
                //};

                //if (saveFileDialog.ShowDialog() == DialogResult.OK)
                //{
                //    // SaveMemory handles pause/resume internally
                //    SaveMemory(saveFileDialog.FileName);
                //    TraceInternal.TraceInformation($"Memory saved to {saveFileDialog.FileName}");
                //    MessageBox.Show($"Memory saved to {saveFileDialog.FileName}",
                //                  "Save Complete",
                //                  MessageBoxButtons.OK,
                //                  MessageBoxIcon.Information);
                //}
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

            if (_stopped == false)
            {
                _basic.Stop();  // This will cause the worker thread to exit the Run() loop and finish
                if (_workerThread != null && _workerThread.IsAlive)
                {
                    _workerThread.Join(1000);
                }
            }

            try
            {
                string fileName = "memory_dump.bin";
                byte[] loadedMemory = File.ReadAllBytes(fileName);

                UInt32 size = 4096;
                _basic = new uBasic(loadedMemory, size, _displayIO);
                _basic.Init();
                _basic.Resume();

                _stopped = false;
                _paused = false;
                this._workerThread = new Thread(new ThreadStart(this.Run));
                _displayIO.Reset();
                this._workerThread.Start();
                consolePictureBox.Visible = true;
                consolePictureBox.Enabled = true;
            }
            catch (Exception e1)
            {
                TraceInternal.TraceError(e1.ToString());
            }




                //OpenFileDialog openFileDialog = new OpenFileDialog
                //{
                //    Filter = "Memory Dump (*.bin)|*.bin|All Files (*.*)|*.*",
                //    FilterIndex = 1,
                //    RestoreDirectory = true
                //};

                //if (openFileDialog.ShowDialog() == DialogResult.OK)
                //{
                //    // If no interpreter exists, create one to receive the memory dump
                //    if (_basic == null)
                //    {
                //        UInt32 size = 4096;
                //        byte[] memory = new byte[size];
                //        _basic = new uBasic(memory, size, _displayIO);
                //        _basic.Init();
                //    }
                //    LoadMemory(openFileDialog.FileName);

                //    // Clear display after loading new state
                //    _mtd.Clear();
                //    _mtd.Set(0, 0);
                //    consolePictureBox.Invalidate();

                //    TraceInternal.TraceInformation($"Memory loaded from {openFileDialog.FileName}");

                // Start worker thread if not already running
                //if (_stopped || _workerThread == null || !_workerThread.IsAlive)
                //{
                //    _stopped = false;
                //    _workerThread = new Thread(new ThreadStart(this.Run));
                //    _displayIO.Reset();
                //    _workerThread.Start();
                //    consolePictureBox.Visible = true;
                //    consolePictureBox.Enabled = true;
                //}

                //MessageBox.Show($"Memory loaded from {openFileDialog.FileName}\nExecution started automatically.",
                //              "Load Complete",
                //              MessageBoxButtons.OK,
                //              MessageBoxIcon.Information);
            //}
            //}
            //catch (Exception ex)
            //{
            //    TraceInternal.TraceError($"Load memory error: {ex.Message}");
            //    MessageBox.Show($"Error loading memory: {ex.Message}",
            //                  "Load Error",
            //                  MessageBoxButtons.OK,
            //                  MessageBoxIcon.Error);
            //}

            Debug.WriteLine("Out loadToolStripMenuItem_Click()");
        }

        private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _paused = true;
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
            _stopped = true;  // Ensure the worker thread will stop running the old state
            try
            {
                Thread.Sleep(500);

                byte[] loadedMemory = File.ReadAllBytes(filePath);
                if (loadedMemory.Length != _basic.Size)
                {
                    throw new InvalidOperationException(
                        $"Memory size mismatch: file={loadedMemory.Length}, expected={_basic.Size}");
                }

                UInt32 size = 4096;
                _basic = new uBasic(loadedMemory, size, _displayIO);
                _basic.Init();

                // Copy into the existing buffer that the C DLL is pointing to
                // DON'T do: _basic.Memory = loadedMemory;  // This won't work!
                //Array.Copy(loadedMemory, 0, _basic.Memory, 0, (int)_basic.Size);

                // Now resume from the restored state
                _basic.Resume();

                TraceInternal.TraceInformation($"Loaded {_basic.Size} bytes from {filePath} and resumed");
            }
            catch (Exception ex)
            {
                TraceInternal.TraceError($"LoadMemory error: {ex.Message}");
                throw;
            }
            finally
            {
                //_paused = false;
            }
            Debug.WriteLine("Out uBasicUI.LoadMemory()");
        }

        /// <summary>
        /// Save the entire interpreter memory state to a binary file
        /// </summary>
        /// <param name="filePath">Path to save the memory dump</param>
        private void SaveMemory(string filePath)
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

        private void resumeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _paused = false;
        }
    }
}