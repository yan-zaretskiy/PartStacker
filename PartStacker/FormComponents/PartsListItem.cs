using System.Windows.Forms;
using PartStacker.MeshFile;
using PartStacker.Geometry;

namespace PartStacker.FormComponents
{
    public class PartsListItem : ListViewItem
    {
        public PartProperties Properties;
        public string FileName;
        private bool Mirrored;

        public PartsListItem(string fileName, Mesh baseMesh)
            : base()
        {
            FileName = fileName;
            Mirrored = false;
            Initialize(baseMesh);
        }

        private void Initialize(Mesh baseMesh)
        {
            Properties = new PartProperties()
            {
                BaseMesh = baseMesh.Clone(),
                Volume = baseMesh.Volume(),
                TriangleCount = baseMesh.Triangles.Count,
                Quantity = 1,
                MinHole = 1,
                RotationIndex = 1,
                RotateMinBox = false,
            };

            if (Mirrored)
            {
                Properties.BaseMesh.Mirror();
            }
            Properties.BaseMesh.CalcBox();

            SetItems();
        }

        private void Initialize(string fileName)
        {
            FileName = fileName;
            Mirrored = false;
            Mesh baseMesh;
            try
            {
                baseMesh = STL.From(fileName);
            }
            catch
            {
                MessageBox.Show($"Error reading file {fileName}!", "File error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            Initialize(baseMesh);
        }

        private void SetItems()
        {
            string[] fp = FileName.Split(new char[] { '/', '\\' });

            SubItems.Clear();
            Text = fp[fp.Length - 1];
            if(Text.EndsWith(".stl", true, System.Globalization.CultureInfo.CurrentCulture))
                Text = Text.Substring(0, Text.Length - 4);

            // Check the last 2 split sections to see if they are an integer
            var sections = Text.Split(['(', ')', '.']).Reverse().Take(2);
            foreach (string s in sections)
            {
                if (int.TryParse(s, out Properties.Quantity))
                {
                    break;
                }
            }
            if (Properties.Quantity == 0)
            {
                Properties.Quantity = 1;
            }

            SubItems.Add(Properties.Quantity.ToString());
            SubItems.Add(Math.Round(Properties.Volume / 1000, 2).ToString());
            SubItems.Add(Properties.TriangleCount.ToString());

            if (Mirrored)
                SubItems.Add("Mirrored");
            else
                SubItems.Add("");
        }

        public void SetQuantity(int quantity)
        {
            Properties.Quantity = quantity;
            SubItems[1].Text = quantity.ToString();
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

            Initialize(select.FileName);
        }

        public void ReloadFile()
        {
            // If parts cannot be found in original location, maybe they're in the working directory?
            if (!File.Exists(FileName) && File.Exists(Path.GetFileName(FileName)))
                FileName = Path.GetFullPath(Path.GetFileName(FileName));
            
            Initialize(FileName);
        }

        public void Mirror()
        {
            Mirrored = !Mirrored;
            Properties.BaseMesh.Mirror();
            Properties.BaseMesh.CalcBox();
        }
    }
}