using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.IO;
using PartStacker.MeshFile;
using PartStacker.Geometry;

namespace PartStacker
{
    delegate void Rotation(Mesh original);

    [Serializable]
    public class Part : ListViewItem, ISerializable
    {
        public Mesh BasePart;
        public string FileName;
        public double Volume;
        public int Quantity;
        public int Remaining;
        public int Triangles;
        public int MinHole;
        public bool RotateMinBox;
        public int RotationIndex;
        public bool Mirrored;

        public Part(string FileName, Mesh BasePart)
            : base()
        {
            this.BasePart = BasePart;
            BasePart.CalcBox();
            this.FileName = FileName;
            this.Volume = BasePart.Volume();
            this.Triangles = BasePart.Triangles.Count;
            this.Quantity = 1;
            this.MinHole = 1;
            this.RotationIndex = 1;
            this.RotateMinBox = false;

            SetItems();
            ExtractCount();
        }

        public void SetItems()
        {
            string[] fp = FileName.Split(new char[] { '/', '\\' });

            SubItems.Clear();
            Text = fp[fp.Length - 1];
            if(Text.EndsWith(".stl", true, System.Globalization.CultureInfo.CurrentCulture))
                Text = Text.Substring(0, Text.Length - 4);

            SubItems.Add(Quantity.ToString());
            SubItems.Add(Math.Round(Volume / 1000, 2).ToString());
            SubItems.Add(Triangles.ToString());

            if (BasePart == null)
                SubItems.Add("FILE UNREADABLE");
            else if (Mirrored)
                SubItems.Add("Mirrored");
            else
                SubItems.Add("");
        }

        private void ExtractCount()
        {
            string[] ct = this.Text.Split(new char[] { '(', ')', '.' });
            try
            {
                this.Quantity = int.Parse(ct[ct.Length - 1]);
                SetItems();
            }
            catch
            {
                try
                {
                    this.Quantity = int.Parse(ct[ct.Length - 2]);
                    SetItems();
                }
                catch
                {

                }
            }
        }

        public void ChangeFile()
        {
            FileDialog select = new OpenFileDialog()
            {
                Title = "Select model file to replace " + Text,
                Filter = "STL files (*.stl)|*.stl|All files (*.*)|*.*",
                Multiselect = false
            };

            DialogResult result = select.ShowDialog();
            if (result != DialogResult.OK)
                return;

            try
            {
                BasePart = STL.From(select.FileName);
                Mirror();
                BasePart.CalcBox();
                this.Volume = BasePart.Volume();
                this.Triangles = BasePart.Triangles.Count;
                this.FileName = select.FileName;
            }
            catch
            {
                MessageBox.Show("Error reading file " + select.FileName + "!", "File error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            SetItems();
        }

        public void Mirror()
        {
            if (!Mirrored) return;
            BasePart.Mirror();
        }

        public void ReloadFile()
        {
            if (!File.Exists(FileName) && File.Exists(Path.GetFileName(FileName))) FileName = Path.GetFullPath(Path.GetFileName(FileName)); // If parts cannot be found in original location, maybe they're in the working directory?
            
            try
            {
                BasePart = STL.From(FileName);
                Mirror();
                BasePart.CalcBox();
                this.Volume = BasePart.Volume();
                this.Triangles = BasePart.Triangles.Count;
            }
            catch
            {
                BasePart = null;
                MessageBox.Show("Error reading file " + FileName + "! Perhaps it has been moved or renamed?", "File error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            SetItems();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("FileName", FileName);
            info.AddValue("Quantity", Quantity);
            info.AddValue("MinHole", MinHole);
            info.AddValue("RotateMinBox", RotateMinBox);
            info.AddValue("RotationIndex", RotationIndex);
            info.AddValue("Mirrored", Mirrored);
        }

        public Part(SerializationInfo info, StreamingContext context)
            : base()
        {
            if (info == null)
                throw new ArgumentNullException("info");

            FileName = info.GetString("FileName");
            Quantity = info.GetInt32("Quantity");
            MinHole = info.GetInt32("MinHole");
            RotateMinBox = info.GetBoolean("RotateMinBox");
            RotationIndex = info.GetInt32("RotationIndex");
            Mirrored = info.GetBoolean("Mirrored");

            ReloadFile();
        }
    }
}