using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework.Content;
using WinFormsContentLoading;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using PartStacker.Geometry;

namespace PartStacker
{
    partial class MainForm : Form
    {
        ModelViewerControl Display3D;
        ListView PartsList;
        Button Import, Delete, Change, Reload, Start, Preview, Export, CopyMirror, GenerateBox;
        NumericUpDown PartQuantity, MinHole, MinimumClearance;
        CheckBox RotateMinBox, EnableSinterbox;
        ProgressBar Progress;
        Label InfoLabel;
        RadioButton None, Cubic, Arbitrary;
        NumericUpDown Clearance, Spacing, Thickness, BWidth;
        NumericUpDown xMin, xMax, yMin, yMax, zMin, zMax;

        Thread StackerThread;
        bool StackerThreadRunning = false;

        STLBody result;

        Rotation[][] RotationSets = new Rotation[3][];

        ToolStripMenuItem ImportMenu, ExportMenu;

        public MainForm()
            : base()
        {
            // Set initial size
            Width = 1030;
            Height = 654;

            // Fix size
            MinimumSize = Size;
            MaximumSize = Size;

            // Title text
            Text = "PartStacker 1.0 - Tom van der Zanden";

            // Abort stacking when program is closed
            this.FormClosing += (o, e) => {
                if (StackerThreadRunning)
                {
                    StackerThreadRunning = false;
                    StackerThread.Join();
                }
            };

            // Set up the array containing rotations
            LoadRotations();

            // Menustrip for saving etc.
            MenuStrip menu = new MenuStrip();

            ToolStripMenuItem fileMenu = new ToolStripMenuItem("File");
            ToolStripMenuItem item = new ToolStripMenuItem("New");
            item.Click += NewHandler;
            fileMenu.DropDownItems.Add(item);
            item = new ToolStripMenuItem("Open");
            item.Click += OpenHandler;
            fileMenu.DropDownItems.Add(item);
            item = new ToolStripMenuItem("Save");
            item.Click += SaveHandler;
            fileMenu.DropDownItems.Add(item);
            item = new ToolStripMenuItem("Close");
            item.Click += CloseHandler;
            fileMenu.DropDownItems.Add(item);

            ToolStripMenuItem partMenu = new ToolStripMenuItem("Import/Export");
            ImportMenu = new ToolStripMenuItem("Import parts");
            ImportMenu.Click += ImportHandler;
            partMenu.DropDownItems.Add(ImportMenu);
            ExportMenu = new ToolStripMenuItem("Export result as STL");
            ExportMenu.Enabled = false;
            ExportMenu.Click += ExportHandler;
            partMenu.DropDownItems.Add(ExportMenu);

            ToolStripMenuItem about = new ToolStripMenuItem("Help");
            item = new ToolStripMenuItem("About");
            item.Click += (o, e) => { MessageBox.Show("PartStacker is (c)opyright Tom van der Zanden 2011-2013. It is licensed under the GNU General Public License.", "PartStacker", MessageBoxButtons.OK, MessageBoxIcon.Information); };
            about.DropDownItems.Add(item);
            item = new ToolStripMenuItem("Visit website");
            item.Click += (o, e) => { System.Diagnostics.Process.Start("http://www.tomvanderzanden.nl/partstacker"); };
            about.DropDownItems.Add(item);

            menu.Items.Add(fileMenu);
            menu.Items.Add(partMenu);
            menu.Items.Add(about);
            Controls.Add(menu);

            // Panel for drawing the 3D preview
            Display3D = new ModelViewerControl()
            {
                Location = new Point(0, 24),
                Size = new Size(ClientSize.Height - menu.Height, ClientSize.Height - menu.Height),
                BackColor = Color.FromArgb(40, 50, 120)
            };
            Controls.Add(Display3D);

            // ListView showing the base STL files
            PartsList = new ListView()
            {
                Location = new Point(ClientSize.Width - 400, 20 + menu.Height),
                Size = new Size(380, 240),
                View = View.Details,
                AllowColumnReorder = true,
            };
            PartsList.SelectedIndexChanged += PartSelectHandler;
            PartsList.Columns.Add("Name", 105);
            PartsList.Columns.Add("Quantity", 60);
            PartsList.Columns.Add("Volume", 60);
            PartsList.Columns.Add("Triangles", 60);
            PartsList.Columns.Add("Comment", 90);
            Controls.Add(PartsList);

            // Buttons for interacting with the list view
            Import = new Button()
            {
                Location = new Point(ClientSize.Width - 400, 270 + menu.Height),
                Size = new Size(88, 25),
                Text = "Import"
            };
            Delete = new Button()
            {
                Location = new Point(ClientSize.Width - 302, 270 + menu.Height),
                Size = new Size(88, 25),
                Text = "Delete",
                Enabled = false
            };
            Change = new Button()
            {
                Location = new Point(ClientSize.Width - 204, 270 + menu.Height),
                Size = new Size(88, 25),
                Text = "Change",
                Enabled = false
            };
            Reload = new Button()
            {
                Location = new Point(ClientSize.Width - 106, 270 + menu.Height),
                Size = new Size(88, 25),
                Text = "Reload"
            };
            Import.Click += ImportHandler;
            Delete.Click += DeleteHandler;
            Change.Click += ChangeHandler;
            Reload.Click += ReloadHandler;
            Controls.Add(Import);
            Controls.Add(Delete);
            Controls.Add(Change);
            Controls.Add(Reload);

            // Label with statistics about all the parts
            InfoLabel = new Label()
            {
                Location = new Point(ClientSize.Width - 400, 300 + menu.Height),
                Width = ClientSize.Width - ClientSize.Height - 40
            };
            Controls.Add(InfoLabel);
            SetText();

            // Progressbar for giving information about the progress of the stacking
            Progress = new ProgressBar()
            {
                Location = new Point(ClientSize.Width - 400, ClientSize.Height - 40),
                Size = new Size(279, 25)
            };
            Controls.Add(Progress);

            Start = new Button()
            {
                Location = new Point(ClientSize.Width - 106, ClientSize.Height - 40),
                Size = new Size(88, 25),
                Text = "Start"
            };
            Start.Click += StartHandler;
            Controls.Add(Start);

            // Checkbox to allow section viewing
            CheckBox section = new CheckBox()
            {
                Location = new Point(ClientSize.Width - 400, ClientSize.Height - 75),
                Text = "Section view:",
                CheckAlign = ContentAlignment.MiddleRight,
                Width = 126
            };
            section.CheckedChanged += (o, e) => { Display3D.Section = section.Checked; };
            Controls.Add(section);

            // Export button
            Export = new Button()
            {
                Size = new Size(170, 30),
                Location = new Point(ClientSize.Width - 195, ClientSize.Height - 80),
                Text = "Export result as STL",
                Enabled = false
            };
            Export.Click += ExportHandler;
            Controls.Add(Export);

            // Minimal clearance size control
            Label caption = new Label()
            {
                Text = "Minimum clearance:",
                Location = Location = new Point(ClientSize.Width - 400, ClientSize.Height - 100),
                Width = 110
            };
            Controls.Add(caption);
            MinimumClearance = new NumericUpDown()
            {
                Minimum = 0.5M,
                Maximum = 2,
                Increment = 0.05M,
                Location = new Point(ClientSize.Width - 288, ClientSize.Height - 102),
                Width = 50,
                Value = 1,
                Enabled = true,
                DecimalPlaces = 2
            };
            Controls.Add(MinimumClearance);

            // Group box that contains controls for editing the parts
            TabControl Tabs = new TabControl()
            {
                Location = new Point(ClientSize.Width - 400, 325 + menu.Height),
                Size = new Size(380, 140),
            };

            Controls.Add(Tabs);

            // Part quantity updown
            Tabs.TabPages.Add("Part Settings");
            TabPage PartsTab = Tabs.TabPages[0];
            caption = new Label()
            {
                Text = "Quantity: ",
                Location = new Point(10, 25) - new Size(5, 15)
            };
            PartsTab.Controls.Add(caption);
            PartQuantity = new NumericUpDown()
            {
                Minimum = 0,
                Maximum = 200,
                Location = new Point(120, 22) - new Size(5, 15),
                Width = 50,
                Value = 1,
                Enabled = false
            };
            PartQuantity.ValueChanged += PartQuantityHandler;
            PartsTab.Controls.Add(PartQuantity);

            // Minimum hole size updown
            caption = new Label()
            {
                Text = "Minimum hole: ",
                Location = new Point(10, 50) - new Size(5, 15)
            };
            PartsTab.Controls.Add(caption);
            MinHole = new NumericUpDown()
            {
                Minimum = 0,
                Maximum = 100,
                Location = new Point(120, 47) - new Size(5, 15),
                Width = 50,
                Value = 1,
                Enabled = false
            };
            MinHole.ValueChanged += MinHoleHandler;
            PartsTab.Controls.Add(MinHole);

            // Checkbox for minimizing the bounding box
            caption = new Label()
            {
                Text = "Minimize box: ",
                Location = new Point(10, 75) - new Size(5, 15)
            };
            PartsTab.Controls.Add(caption);
            RotateMinBox = new CheckBox()
            {
                Location = new Point(120, 71) - new Size(5, 15),
                Checked = false,
                Enabled = false,
                Width = 20
            };
            RotateMinBox.CheckedChanged += RotateMinBoxHandler;
            PartsTab.Controls.Add(RotateMinBox);

            // Preview button that shows the voxelation
            Preview = new Button()
            {
                Location = new Point(10, 98) - new Size(5, 15),
                Size = new Size(88, 25),
                Text = "Preview",
                Enabled = false
            };
            Preview.Click += PreviewHandler;
            PartsTab.Controls.Add(Preview);

            // Preview button that shows the voxelation
            CopyMirror = new Button()
            {
                Location = new Point(110, 98) - new Size(5, 15),
                Size = new Size(88, 25),
                Text = "Mirrored copy",
                Enabled = false
            };
            CopyMirror.Click += CopyHandler;
            PartsTab.Controls.Add(CopyMirror);

            GroupBox rotations = new GroupBox()
            {
                Location = new Point(180, 20) - new Size(5, 15),
                Text = "Part rotations",
                Size = new Size(190, 61)
            };
            PartsTab.Controls.Add(rotations);

            None = new RadioButton()
            {
                Location = new Point(10, 15),
                Text = "None",
                Enabled = false,
                Width = 80
            };
            None.Click += RotationHandler;
            rotations.Controls.Add(None);
            Cubic = new RadioButton()
            {
                Location = new Point(10, 35),
                Text = "Cubic",
                Checked = true,
                Enabled = false
            };
            Cubic.Click += RotationHandler;
            rotations.Controls.Add(Cubic);
            Arbitrary = new RadioButton()
            {
                Location = new Point(100, 15),
                Text = "Arbitrary",
                Enabled = false,
                Width = 80
            };
            Arbitrary.Click += RotationHandler;
            rotations.Controls.Add(Arbitrary);
            
            Tabs.TabPages.Add("Sinterbox");
            TabPage SinterboxTab = Tabs.TabPages[1];
            caption = new Label()
            {
                Text = "Clearance: ",
                Location = new Point(10, 25) - new Size(5, 15)
            };
            SinterboxTab.Controls.Add(caption);
            Clearance = new NumericUpDown()
            {
                Minimum = (decimal)0.1,
                Maximum = 4,
                Location = new Point(120, 22) - new Size(5, 15),
                Width = 50,
                Value = (decimal)0.8,
                Increment = (decimal)0.1,
                DecimalPlaces = 1
            };
            SinterboxTab.Controls.Add(Clearance);
            caption = new Label()
            {
                Text = "Spacing: ",
                Location = new Point(10, 50) - new Size(5, 15)
            };
            SinterboxTab.Controls.Add(caption);
            Spacing = new NumericUpDown()
            {
                Minimum = 1,
                Maximum = 20,
                Location = new Point(120, 47) - new Size(5, 15),
                Width = 50,
                Value = 6,
                Increment = (decimal)0.5,
                DecimalPlaces = 1
            };
            SinterboxTab.Controls.Add(Spacing);
            caption = new Label()
            {
                Text = "Thickness: ",
                Location = new Point(10, 75) - new Size(5, 15)
            };
            SinterboxTab.Controls.Add(caption);
            Thickness = new NumericUpDown()
            {
                Minimum = (decimal)0.1,
                Maximum = 4,
                Location = new Point(120, 72) - new Size(5, 15),
                Width = 50,
                Value = (decimal)0.8,
                Increment = (decimal)0.1,
                DecimalPlaces = 1
            };
            SinterboxTab.Controls.Add(Thickness);
            caption = new Label()
            {
                Text = "Width: ",
                Location = new Point(10, 100) - new Size(5, 15)
            };
            SinterboxTab.Controls.Add(caption);
            BWidth = new NumericUpDown()
            {
                Minimum = (decimal)0.1,
                Maximum = 4,
                Location = new Point(120, 97) - new Size(5, 15),
                Width = 50,
                Value = (decimal)1.1,
                Increment = (decimal)0.1,
                DecimalPlaces = 1
            };
            SinterboxTab.Controls.Add(BWidth);
            caption = new Label()
            {
                Text = "Generate sinterbox: ",
                Location = new Point(180, 22) - new Size(5, 15)
            };
            SinterboxTab.Controls.Add(caption);
            EnableSinterbox = new CheckBox()
            {
                Location = new Point(300, 18) - new Size(5, 15),
                Checked = true
            };
            SinterboxTab.Controls.Add(EnableSinterbox);
            GenerateBox = new Button()
            {
                Location = new Point(180, 47) - new Size(5, 15),
                Text = "Box+Export current part",
                Width = 135,
                Enabled = false
            };
            GenerateBox.Click += GenerateBoxHandler;
            SinterboxTab.Controls.Add(GenerateBox);
            
            Tabs.TabPages.Add("Bounding Box");
            TabPage BBTab = Tabs.TabPages[2];
            caption = new Label()
            {
                Text = "Initial X: ",
                Location = new Point(10, 25) - new Size(5, 15)
            };
            BBTab.Controls.Add(caption);
            xMin = new NumericUpDown()
            {
                Minimum = 10,
                Maximum = 250,
                Location = new Point(120, 22) - new Size(5, 15),
                Width = 50,
                Value = 150,
                Increment = 1,
                DecimalPlaces = 0
            };
            BBTab.Controls.Add(xMin);
            caption = new Label()
            {
                Text = "Initial Y: ",
                Location = new Point(10, 50) - new Size(5, 15)
            };
            BBTab.Controls.Add(caption);
            yMin = new NumericUpDown()
            {
                Minimum = 10,
                Maximum = 250,
                Location = new Point(120, 47) - new Size(5, 15),
                Width = 50,
                Value = 150,
                Increment = 1,
                DecimalPlaces = 0
            };
            BBTab.Controls.Add(yMin);
            caption = new Label()
            {
                Text = "Initial Z: ",
                Location = new Point(10, 75) - new Size(5, 15)
            };
            BBTab.Controls.Add(caption);
            zMin = new NumericUpDown()
            {
                Minimum = 10,
                Maximum = 250,
                Location = new Point(120, 72) - new Size(5, 15),
                Width = 50,
                Value = 30,
                Increment = 1,
                DecimalPlaces = 0
            };
            BBTab.Controls.Add(zMin);
            //
            //
            caption = new Label()
            {
                Text = "Maximum X: ",
                Location = new Point(10, 25) - new Size(5, 15) + new Size(170, 0)
            };
            BBTab.Controls.Add(caption);
            xMax = new NumericUpDown()
            {
                Minimum = 10,
                Maximum = 250,
                Location = new Point(120, 22) - new Size(5, 15) + new Size(170, 0),
                Width = 50,
                Value = 156,
                Increment = 1,
                DecimalPlaces = 0
            };
            BBTab.Controls.Add(xMax);
            caption = new Label()
            {
                Text = "Maximum Y: ",
                Location = new Point(10, 50) - new Size(5, 15) + new Size(170, 0)
            };
            BBTab.Controls.Add(caption);
            yMax = new NumericUpDown()
            {
                Minimum = 10,
                Maximum = 250,
                Location = new Point(120, 47) - new Size(5, 15) + new Size(170, 0),
                Width = 50,
                Value = 156,
                Increment = 1,
                DecimalPlaces = 0
            };
            BBTab.Controls.Add(yMax);
            caption = new Label()
            {
                Text = "Maximum Z: ",
                Location = new Point(10, 75) - new Size(5, 15) + new Size(170, 0)
            };
            BBTab.Controls.Add(caption);
            zMax = new NumericUpDown()
            {
                Minimum = 10,
                Maximum = 250,
                Location = new Point(120, 72) - new Size(5, 15) + new Size(170, 0),
                Width = 50,
                Value = 90,
                Increment = 1,
                DecimalPlaces = 0
            };
            BBTab.Controls.Add(zMax);
            
            ComboBox PresetBox = new ComboBox()
            {
                Location = new Point(10, 85),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            BBTab.Controls.Add(PresetBox);
            PresetBox.Items.Add("Load Preset");
            PresetBox.Items.Add("P1 130x105");
            PresetBox.Items.Add("P1 65x105");
            PresetBox.Items.Add("P1 86x105");
            PresetBox.Items.Add("P2 165x165");
            PresetBox.Items.Add("P2 82x165");
            PresetBox.Items.Add("P2 82x82");
            PresetBox.Items.Add("P2 110x110");
            PresetBox.SelectedIndex = 0;
            PresetBox.SelectedIndexChanged += loadBBPreset;
        }

        public void CopyHandler(object o, EventArgs ea)
        {
            if (((Part)PartsList.SelectedItems[0]).BasePart == null)
                return;

            Part copy = new Part(((Part)PartsList.SelectedItems[0]).FileName, (STLBody)((Part)PartsList.SelectedItems[0]).BasePart.Clone());

            copy.Quantity = ((Part)PartsList.SelectedItems[0]).Quantity;
            copy.RotateMinBox = ((Part)PartsList.SelectedItems[0]).RotateMinBox;
            copy.RotationIndex = ((Part)PartsList.SelectedItems[0]).RotationIndex;
            copy.MinHole = ((Part)PartsList.SelectedItems[0]).MinHole;
            copy.Mirrored = !((Part)PartsList.SelectedItems[0]).Mirrored;

            copy.SetItems();
            copy.ReloadFile();
            PartsList.Items.Add(copy);
        }

        public void NewHandler(object o, EventArgs ea)
        {
            PartsList.Items.Clear();
        }

        public void OpenHandler(object o, EventArgs ea)
        {
            OpenFileDialog select = new OpenFileDialog()
            {
                Title = "Load stacking settings from file",
                Filter = "Stacking settings (*.stk)|*.stk"
            };
            DialogResult dr = select.ShowDialog();

            if (dr != DialogResult.OK)
                return;

            try
            {
                Stream stream = File.Open(select.FileName, FileMode.Open);
#pragma warning disable SYSLIB0011
                BinaryFormatter bformatter = new BinaryFormatter();
#pragma warning restore SYSLIB0011

                List<Part> items = (List<Part>)bformatter.Deserialize(stream);
                stream.Close();

                PartsList.Items.Clear();

                foreach (Part item in items)
                    PartsList.Items.Add(item);

                SetText();
            }
            catch
            {
                MessageBox.Show("Error reading file " + select.FileName + "!", "File error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void SaveHandler(object o, EventArgs ea)
        {
            SaveFileDialog select = new SaveFileDialog()
            {
                Title = "Select file to save stacking settings",
                Filter = "Stacking settings (*.stk)|*.stk"
            };
            DialogResult dr = select.ShowDialog();

            if (dr != DialogResult.OK)
                return;

            try
            {
                Stream stream = File.Open(select.FileName, FileMode.Create);
#pragma warning disable SYSLIB0011
                BinaryFormatter bformatter = new BinaryFormatter();
#pragma warning restore SYSLIB0011

                List<Part> temp = new List<Part>(PartsList.Items.Count);
                foreach (Part p in PartsList.Items)
                    temp.Add(p);

                bformatter.Serialize(stream, temp);
                stream.Close();
            }
            catch
            {
                MessageBox.Show("Error writing to file " + select.FileName + "!", "File error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void CloseHandler(object o, EventArgs ea)
        {
            this.Close();
        }

        public void ExportHandler(object o, EventArgs ea)
        {
            SaveFileDialog select = new SaveFileDialog()
            {
                Title = "Select file to save result to",
                Filter = "STL files (*.stl)|*.stl"
            };
            DialogResult dr = select.ShowDialog();

            if (dr != DialogResult.OK)
                return;

            try
            {
                result.ExportSTL(select.FileName);
            }
            catch
            {
                MessageBox.Show("Error writing to file " + select.FileName + "!", "File error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void LoadRotations()
        {
            // No rotation
            RotationSets[0] = new Rotation[] { (STLBody b) => { } };

            // Cubic rotations
            RotationSets[1] = new Rotation[] { (STLBody b) => { }, (STLBody b) => { b.Rotate(new Point3(1, 0, 0), 90.0f); }, (STLBody b) => { b.Rotate(new Point3(1, 0, 0), 180.0f); }, (STLBody b) => { b.Rotate(new Point3(1, 0, 0), 270.0f); }, (STLBody b) => { b.Rotate(new Point3(0, 1, 0), 90.0f); }, (STLBody b) => { b.Rotate(new Point3(0, 1, 0), 180.0f); }, (STLBody b) => { b.Rotate(new Point3(0, 1, 0), 270.0f); }, (STLBody b) => { b.Rotate(new Point3(0, 0, 1), 90.0f); }, (STLBody b) => { b.Rotate(new Point3(0, 0, 1), 180.0f); }, (STLBody b) => { b.Rotate(new Point3(0, 0, 1), 270.0f); }, (STLBody b) => { b.Rotate(new Point3(1, 1, 0), 180.0f); }, (STLBody b) => { b.Rotate(new Point3(1, -1, 0), 180.0f); }, (STLBody b) => { b.Rotate(new Point3(0, 1, 1), 180.0f); }, (STLBody b) => { b.Rotate(new Point3(0, -1, 1), 180.0f); }, (STLBody b) => { b.Rotate(new Point3(1, 0, 1), 180.0f); }, (STLBody b) => { b.Rotate(new Point3(1, 0, -1), 180.0f); }, (STLBody b) => { b.Rotate(new Point3(1, 1, 1), 120.0f); }, (STLBody b) => { b.Rotate(new Point3(1, 1, 1), 240.0f); }, (STLBody b) => { b.Rotate(new Point3(-1, 1, 1), 120.0f); }, (STLBody b) => { b.Rotate(new Point3(-1, 1, 1), 240.0f); }, (STLBody b) => { b.Rotate(new Point3(1, -1, 1), 120.0f); }, (STLBody b) => { b.Rotate(new Point3(1, -1, 1), 240.0f); }, (STLBody b) => { b.Rotate(new Point3(1, 1, -1), 120.0f); }, (STLBody b) => { b.Rotate(new Point3(1, 1, -1), 240.0f); } };
            
            //TODO: arbitrary rotations
            RotationSets[2] = new Rotation[32];

            RotationSets[2][0] = (STLBody b) => { };
            RotationSets[2][1] = (STLBody b) => { b.Rotate(new Point3(1, 1, 1), 120.0f); };
            RotationSets[2][2] = (STLBody b) => { b.Rotate(new Point3(1, 1, 1), 240.0f); };
            RotationSets[2][3] = (STLBody b) => { b.Rotate(new Point3(1, 0, 0), 180.0f); };
            RotationSets[2][4] = (STLBody b) => { b.Rotate(new Point3(0, 1, 0), 180.0f); };
            RotationSets[2][5] = (STLBody b) => { b.Rotate(new Point3(0, 0, 1), 180.0f); };

            Random r = new Random();
            for (int i = 6; i < 32; i++)
            {
                float rx = (float)(r.NextDouble() * 360.0);
                float ry = (float)(r.NextDouble() * 360.0);
                float rz = (float)(r.NextDouble() * 360.0);
                RotationSets[2][i] = (STLBody b) => { b.Rotate(new Point3(1, 0, 0), rx); b.Rotate(new Point3(0, 1, 0), ry); b.Rotate(new Point3(0, 0, 1), rz); };
            }
        }

        public void RotationHandler(object o, EventArgs ea)
        {
            if (!None.Enabled || !Cubic.Enabled || !Arbitrary.Enabled)
                return;

            if (None.Checked)
                ((Part)PartsList.SelectedItems[0]).RotationIndex = 0;
            else if(Cubic.Checked)
                ((Part)PartsList.SelectedItems[0]).RotationIndex = 1;
            else
                ((Part)PartsList.SelectedItems[0]).RotationIndex = 2;
        }

        public void PartQuantityHandler(object o, EventArgs ea)
        {
            if (!PartQuantity.Enabled)
                return;

            ((Part)PartsList.SelectedItems[0]).Quantity = (int)PartQuantity.Value;
            ((Part)PartsList.SelectedItems[0]).SetItems();

            SetText();
        }

        public void MinHoleHandler(object o, EventArgs ea)
        {
            if (!MinHole.Enabled)
                return;

            ((Part)PartsList.SelectedItems[0]).MinHole = (int)MinHole.Value;
        }

        public void RotateMinBoxHandler(object o, EventArgs ea)
        {
            if (!RotateMinBox.Enabled)
                return;

            ((Part)PartsList.SelectedItems[0]).RotateMinBox = RotateMinBox.Checked;
        }

        public void PreviewHandler(object o, EventArgs ea)
        {
            STLBody body = ((Part)PartsList.SelectedItems[0]).BasePart;
            int[,,] voxels_temp = new int[body.box.Item1, body.box.Item2, body.box.Item3];
            int volume = body.Voxelize(voxels_temp, 1, (int)MinHole.Value);
            Display3D.SetMeshWithVoxels(body, voxels_temp, volume);
        }

        public void GenerateBoxHandler(object o, EventArgs ea)
        {
            STLBody body = (STLBody) ((Part)PartsList.SelectedItems[0]).BasePart.Clone();
            body.SinterBox((float)Clearance.Value, (float)Thickness.Value, (float)BWidth.Value, ((float)Spacing.Value) + 0.00013759f);

            SaveFileDialog select = new SaveFileDialog()
            {
                Title = "Select file to save result to",
                Filter = "STL files (*.stl)|*.stl"
            };
            DialogResult dr = select.ShowDialog();

            if (dr != DialogResult.OK)
                return;

            try
            {
                result.ExportSTL(select.FileName);
            }
            catch
            {
                MessageBox.Show("Error writing to file " + select.FileName + "!", "File error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void loadBBPreset(object o, EventArgs ea)
        {
            ComboBox box = (ComboBox) o;
            if (box.SelectedIndex != 0)
            {
                string[] strArray = box.GetItemText(box.SelectedItem).Split(new char[] { ' ', 'x' });
                int num = int.Parse(strArray[1]);
                int num2 = int.Parse(strArray[2]);
                xMin.Value = num - 7;
                yMin.Value = num2 - 7;
                xMax.Value = num - 4;
                yMax.Value = num2 - 4;
                float num3 = 0f;
                int minimum = (int) zMin.Minimum;
                foreach (Part part in PartsList.Items)
                {
                    num3 += part.Quantity * part.Volume;
                    Tuple<int, int, int> tuple = part.BasePart.CalcBox();
                    minimum = Math.Max(minimum, 1 + Math.Min(tuple.Item1, Math.Min(tuple.Item2, tuple.Item3)));
                }
                if (num3 == 0f)
                {
                    num3 = 1000f;
                }
                zMin.Value = Math.Min(zMin.Maximum, Math.Max(minimum, (decimal) ((((double) num3) / 0.135) / ((double) (num * num2)))));
                zMax.Value = Math.Max(zMax.Minimum, Math.Min(zMax.Maximum, 2M * ((decimal) ((((double) num3) / 0.15) / ((double) (num * num2))))));
            }
        }

        public void ImportHandler(object o, EventArgs ea)
        {
            FileDialog select = new OpenFileDialog()
            {
                Title = "Select model files to import",
                Filter = "STL files (*.stl)|*.stl|All files (*.*)|*.*",
                Multiselect = true
            };

            DialogResult result = select.ShowDialog();
            if (result != DialogResult.OK)
                return;

            for(int i = 0; i < select.FileNames.Length; i++)
            {
                try
                {
                    Part p = new Part(select.FileNames[i], STLBody.FromSTL(select.FileNames[i]));
                    
                    PartsList.Items.Add(p);
                    if (select.FileNames.Length == 1)
                    {
                        PartsList.SelectedItems.Clear();
                        p.Selected = true;
                        PartsList.Select();
                    }
                }
                catch
                {
                    MessageBox.Show("Error reading file " + select.FileNames[i] + "!", "File error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            SetText();
        }
        public void DeleteHandler(object o, EventArgs ea)
        {
            DialogResult confirm = MessageBox.Show("Are you sure you want to remove these " + PartsList.SelectedIndices.Count + " parts?", "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
                return;

            int[] toRemove = new int[PartsList.SelectedIndices.Count];
            int i = 0;
            foreach (int index in PartsList.SelectedIndices)
                toRemove[i++] = index;
            Array.Sort(toRemove);
            for (i = toRemove.Length - 1; i >= 0; i--)
                PartsList.Items.RemoveAt(toRemove[i]);

            SetText();
        }
        public void ChangeHandler(object o, EventArgs ea)
        {
            ((Part)PartsList.SelectedItems[0]).ChangeFile();
            PartSelectHandler(null, null);

            SetText();
        }
        public void ReloadHandler(object o, EventArgs ea)
        {
            if (PartsList.SelectedItems.Count > 0)
                foreach (Part p in PartsList.SelectedItems)
                    p.ReloadFile();
            else
                foreach (Part p in PartsList.Items)
                    p.ReloadFile();

            SetText();
        }

        private void DisableButtons()
        {
            Start.Text = "Stop";
            PartsList.SelectedItems.Clear();
            PartsList.Select();
            this.Import.Enabled = false;
            this.ImportMenu.Enabled = false;
            this.Export.Enabled = false;
            this.ExportMenu.Enabled = false;
            this.Reload.Enabled = false;
            this.PartsList.Enabled = false;
            this.MinimumClearance.Enabled = false;
            this.GenerateBox.Enabled = false;
        }
        private void EnableButtons()
        {
            Start.Text = "Start";
            this.Import.Enabled = true;
            this.ImportMenu.Enabled = true;
            this.Reload.Enabled = true;
            this.PartsList.Enabled = true;
            this.MinimumClearance.Enabled = true;
        }

        public void StartHandler(object o, EventArgs ea)
        {
            if (!StackerThreadRunning)
            {
                int modelTriangles = 0;
                foreach(Part p in PartsList.Items)
                    modelTriangles += p.Quantity * p.Triangles;

                if (modelTriangles == 0)
                    return;
                else if (modelTriangles > 1000000)
                {
                    DialogResult confirm = MessageBox.Show("The finished model will exceed 1.000.000 triangles. Are you sure you want to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (confirm != DialogResult.Yes)
                        return;
                }

                DisableButtons();

                result = new STLBody(modelTriangles + 2);

                baseParts = new Part[PartsList.Items.Count];
                for (int i = 0; i < baseParts.Length; i++)
                {
                    baseParts[i] = (Part)(PartsList.Items[i]);
                    baseParts[i].Remaining = baseParts[i].Quantity;
                }

                StackerThread = new Thread(Stacker);
                StackerThreadRunning = true;
                StackerThread.Start();
            }
            else
            {
                DialogResult confirm = MessageBox.Show("Are you sure you want to abort stacking? Any progress will be lost.", "Stop stacking", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                StackerThreadRunning = false;
                StackerThread.Join();

                EnableButtons();
                Progress.Value = 0;
            }
        }
        public void FinishStacking(bool succeeded)
        {
            if (succeeded)
            {
                if (EnableSinterbox.Checked)
                    result.SinterBox((float)Clearance.Value, (float)Thickness.Value, (float)BWidth.Value, ((float)Spacing.Value) + 0.00013759f);
                result.CalcBox();
                Display3D.SetMesh(result);
                this.Export.Enabled = true;
                this.ExportMenu.Enabled = true;
                DialogResult dl = MessageBox.Show("Done stacking! Final bounding box: " + Math.Round(result.size.X, 1) + "x" + Math.Round(result.size.Y, 1) + "x" + Math.Round(result.size.Z, 1) + "mm (" + Math.Round(100 * result.Volume() / (result.size.X * result.size.Y * result.size.Z), 1) + "% density).\n\nWould you like to save the result now?", "Stacking complete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (dl == DialogResult.Yes)
                    ExportHandler(null, null);
            }
            else
            {
                MessageBox.Show("Did not manage to stack parts within maximum bounding box", "Stacking failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            EnableButtons();
            Progress.Value = 0;
        }

        public void PartSelectHandler(object o, EventArgs ea)
        {
            Change.Enabled = false;
            Delete.Enabled = false;
            MinHole.Enabled = false;
            PartQuantity.Enabled = false;
            RotateMinBox.Enabled = false;
            Preview.Enabled = false;
            CopyMirror.Enabled = false;
            GenerateBox.Enabled = false;

            None.Enabled = false;
            Cubic.Enabled = false;
            Arbitrary.Enabled = false;

            if (PartsList.SelectedIndices.Count == 1)
            {
                Change.Enabled = true;
                if(((Part)PartsList.SelectedItems[0]).BasePart != null)
                    Display3D.SetMesh(((Part)PartsList.SelectedItems[0]).BasePart);

                MinHole.Value = ((Part)PartsList.SelectedItems[0]).MinHole;
                MinHole.Enabled = true;

                PartQuantity.Value = ((Part)PartsList.SelectedItems[0]).Quantity;
                PartQuantity.Enabled = true;

                RotateMinBox.Checked = ((Part)PartsList.SelectedItems[0]).RotateMinBox;
                RotateMinBox.Enabled = true;

                CopyMirror.Enabled = true;
                GenerateBox.Enabled = true;

                if(((Part)PartsList.SelectedItems[0]).BasePart != null)
                    Preview.Enabled = true;

                if (((Part)PartsList.SelectedItems[0]).RotationIndex == 0)
                    None.Checked = true;
                else if (((Part)PartsList.SelectedItems[0]).RotationIndex == 1)
                    Cubic.Checked = true;
                else
                    Arbitrary.Checked = true;

                None.Enabled = true;
                Cubic.Enabled = true;
                Arbitrary.Enabled = true;
            }
            if (PartsList.SelectedIndices.Count > 0)
                Delete.Enabled = true;
        }

        public void SetText()
        {
            int parts = 0;
            int triangles = 0;
            float volume = 0;

            foreach (Part p in PartsList.Items)
            {
                parts += p.Quantity;
                triangles += p.Quantity * p.Triangles;
                volume += p.Quantity * p.Volume;
            }

            InfoLabel.Text = "Parts: " + parts + " - Volume: " + Math.Round(volume / 1000, 1) + " - Triangles: " + triangles;
        }
    }
}
