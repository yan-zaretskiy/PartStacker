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
using PartStacker.FormComponents;
using PartStacker.Geometry;
using PartStacker.MeshFile;

namespace PartStacker
{
    partial class MainForm : Form
    {
        ModelViewerControl Display3D;
        PartsList PartsList;
        Button Import, Delete, Change, Reload, Start, Preview, Export, CopyMirror;
        NumericUpDown PartQuantity, MinHole, MinimumClearance;
        CheckBox RotateMinBox, EnableSinterbox;
        ProgressBar Progress;
        Label InfoLabel;
        RadioButton None, Cubic, Arbitrary;
        NumericUpDown Clearance, Spacing, Thickness, BWidth;
        NumericUpDown xMin, xMax, yMin, yMax, zMin, zMax;

        PartStacker? Stacker;
        Mesh? LastResult;

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
            this.FormClosing += (o, e) => { Stacker?.Stop(); };

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
            PartsList = new(this, ClientSize.Width, menu.Height);
            PartsList.SelectedIndexChanged += PartSelectHandler; // Todo, make List not visible

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
            PartsList.MirrorCopySelectedItem();
        }

        public void NewHandler(object o, EventArgs ea)
        {
            PartsList.RemoveAll();
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

                List<PartsListItem> items = (List<PartsListItem>)bformatter.Deserialize(stream);
                stream.Close();

                PartsList.RemoveAll();

                foreach (PartsListItem item in items)
                    PartsList.Add(item);

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

                List<PartsListItem> temp = PartsList.AllParts();
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
                STL.To(LastResult, select.FileName);
            }
            catch
            {
                MessageBox.Show("Error writing to file " + select.FileName + "!", "File error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void RotationHandler(object o, EventArgs ea)
        {
            if (!None.Enabled || !Cubic.Enabled || !Arbitrary.Enabled)
                return;

            ref int index = ref PartsList.SelectedItem.Properties.RotationIndex;
            if (None.Checked)
                index = 0;
            else if (Cubic.Checked)
                index = 1;
            else
                index = 2;
        }

        public void PartQuantityHandler(object o, EventArgs ea)
        {
            if (!PartQuantity.Enabled)
                return;

            PartsList.SelectedItem.Properties.Quantity = (int)PartQuantity.Value;
            PartsList.SelectedItem.SetItems();

            SetText();
        }

        public void MinHoleHandler(object o, EventArgs ea)
        {
            if (!MinHole.Enabled)
                return;

            PartsList.SelectedItem.Properties.MinHole = (int)MinHole.Value;
        }

        public void RotateMinBoxHandler(object o, EventArgs ea)
        {
            if (!RotateMinBox.Enabled)
                return;

            PartsList.SelectedItem.Properties.RotateMinBox = RotateMinBox.Checked;
        }

        public void PreviewHandler(object o, EventArgs ea)
        {
            Mesh mesh = PartsList.SelectedItem.Properties.BaseMesh;
            int[,,] voxels_temp = new int[mesh.box.Item1, mesh.box.Item2, mesh.box.Item3];
            int volume = mesh.Voxelize(voxels_temp, 1, (int)MinHole.Value);
            Display3D.SetMeshWithVoxels(mesh, voxels_temp, volume);
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
                int minimum = (int) zMin.Minimum;
                PartsList.ForEachItem(part =>
                {
                    Tuple<int, int, int> tuple = part.Properties.BaseMesh.CalcBox();
                    minimum = Math.Max(minimum, 1 + Math.Min(tuple.Item1, Math.Min(tuple.Item2, tuple.Item3)));
                });
                var (_, _, volume) = PartsList.Totals();
                if (volume == 0)
                {
                    volume = 1000;
                }
                zMin.Value = Math.Min(zMin.Maximum, Math.Max(minimum, (decimal) ((((double) volume) / 0.135) / ((double) (num * num2)))));
                zMax.Value = Math.Max(zMax.Minimum, Math.Min(zMax.Maximum, 2M * ((decimal) ((((double) volume) / 0.15) / ((double) (num * num2))))));
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
                    PartsListItem p = new PartsListItem(select.FileNames[i], STL.From(select.FileNames[i]));
                    
                    PartsList.Add(p);
                    if (select.FileNames.Length == 1)
                    {
                        PartsList.ClearSelection();
                        p.Selected = true;
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
            int selectedCount = PartsList.SelectedItemCount;
            string messageFragment = selectedCount == 1 ? "this part" : $"these {selectedCount} parts";
            DialogResult confirm = MessageBox.Show($"Are you sure you want to remove {messageFragment}?", "Confirm delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes)
                return;

            PartsList.RemoveSelectedItems();

            SetText();
        }
        public void ChangeHandler(object o, EventArgs ea)
        {
            PartsList.SelectedItem.ChangeFile();
            PartSelectHandler(null, null);

            SetText();
        }
        public void ReloadHandler(object o, EventArgs ea)
        {
            PartsList.ReloadSelectedItems();
            SetText();
        }

        private void DisableButtons()
        {
            Start.Text = "Stop";
            PartsList.ClearSelection();
            this.Import.Enabled = false;
            this.ImportMenu.Enabled = false;
            this.Export.Enabled = false;
            this.ExportMenu.Enabled = false;
            this.Reload.Enabled = false;
            this.PartsList.Enable(false);
            this.MinimumClearance.Enabled = false;
        }
        private void EnableButtons()
        {
            Start.Text = "Start";
            this.Import.Enabled = true;
            this.ImportMenu.Enabled = true;
            this.Reload.Enabled = true;
            this.PartsList.Enable(true);
            this.MinimumClearance.Enabled = true;
        }

        public void SetProgress(double progress, double total)
        {
            var func = () => Progress.Value = (int)(100 * progress / total);
            if (Progress.InvokeRequired)
                Invoke(func);
            else
                func();
        }
        public void StartHandler(object o, EventArgs ea)
        {
            bool running = Stacker?.Running ?? false;
            if (!running)
            {
                var (_, modelTriangles, _) = PartsList.Totals();

                if (modelTriangles == 0)
                    return;
                else if (modelTriangles > 1000000)
                {
                    DialogResult confirm = MessageBox.Show("The finished model will exceed 1.000.000 triangles. Are you sure you want to continue?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (confirm != DialogResult.Yes)
                        return;
                }

                DisableButtons();

                PartStacker.Parameters parameters = new()
                {
                    InitialTriangles = modelTriangles + 2,
                    Parts = PartsList.AllParts().Select(part => part.Properties).ToArray(),

                    SetProgress = SetProgress,
                    FinishStacking = (bool b, Mesh m) => this.Invoke(() => FinishStacking(b, m)),
                    DisplayMesh = (Mesh m, int x, int y, int z) => this.Invoke(() => { Display3D.SetMesh(m); Display3D.BB = new Microsoft.Xna.Framework.Vector3(x, y, z); }),
                    Resolution = (double)MinimumClearance.Value,

                    xMin = (double)xMin.Value, xMax = (double)xMax.Value,
                    yMin = (double)yMin.Value, yMax = (double)yMax.Value,
                    zMin = (double)zMin.Value, zMax = (double)zMax.Value,
                };

                Stacker = new(parameters);
            }
            else
            {
                DialogResult confirm = MessageBox.Show("Are you sure you want to abort stacking? Any progress will be lost.", "Stop stacking", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (confirm != DialogResult.Yes)
                    return;

                Stacker?.Stop();
                Stacker = null;

                EnableButtons();
                Progress.Value = 0;
            }
        }
        public void FinishStacking(bool succeeded, Mesh result)
        {
            if (succeeded)
            {
                LastResult = result;
                result.CalcBox();
                if (EnableSinterbox.Checked)
                {
                    Sinterbox.Parameters parameters = new()
                    {
                        Clearance = (double)Clearance.Value,
                        Thickness = (double)Thickness.Value,
                        Width = (double)BWidth.Value,
                        Spacing = ((double)Spacing.Value) + 0.00013759,
                    };
                    result.AddSinterbox(parameters);
                    result.CalcBox();
                }
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

            None.Enabled = false;
            Cubic.Enabled = false;
            Arbitrary.Enabled = false;

            if (PartsList.SelectedItemCount == 1)
            {
                PartsListItem part = PartsList.SelectedItem;

                Change.Enabled = true;
                if(part.Properties.BaseMesh != null)
                    Display3D.SetMesh(part.Properties.BaseMesh);

                MinHole.Value = part.Properties.MinHole;
                MinHole.Enabled = true;

                PartQuantity.Value = part.Properties.Quantity;
                PartQuantity.Enabled = true;

                RotateMinBox.Checked = part.Properties.RotateMinBox;
                RotateMinBox.Enabled = true;

                CopyMirror.Enabled = true;

                if(part.Properties.BaseMesh != null)
                    Preview.Enabled = true;

                if (part.Properties.RotationIndex == 0)
                    None.Checked = true;
                else if (part.Properties.RotationIndex == 1)
                    Cubic.Checked = true;
                else
                    Arbitrary.Checked = true;

                None.Enabled = true;
                Cubic.Enabled = true;
                Arbitrary.Enabled = true;
            }
            if (PartsList.SelectedItemCount > 0)
                Delete.Enabled = true;
        }

        public void SetText()
        {
            var (parts, triangles, volume) = PartsList.Totals();
            InfoLabel.Text = "Parts: " + parts + " - Volume: " + Math.Round(volume / 1000, 1) + " - Triangles: " + triangles;
        }
    }
}
