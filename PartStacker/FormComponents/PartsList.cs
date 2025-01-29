using System.Drawing;
using System.Windows.Forms;

namespace PartStacker.FormComponents
{
    public class PartsList
    {
        private ListView List;

        public PartsList(Control parent, int clientWidth, int menuHeight)
        {
            List = new ListView()
            {
                Location = new Point(clientWidth - 400, 20 + menuHeight),
                Size = new Size(380, 240),
                View = View.Details,
                AllowColumnReorder = true,
            };
            List.Columns.Add("Name", 105);
            List.Columns.Add("Quantity", 60);
            List.Columns.Add("Volume", 60);
            List.Columns.Add("Triangles", 60);
            List.Columns.Add("Comment", 90);
            parent.Controls.Add(List);
        }

        public int ItemCount => List.Items.Count;
        public int SelectedItemCount => List.SelectedItems.Count; // make sure this works, not needing SelectedIndices
        public Part SelectedItem => (Part)List.SelectedItems[0];

        public event EventHandler? SelectedIndexChanged
        {
            add => List.SelectedIndexChanged += value;
            remove => throw new NotSupportedException();
        }

        public void Enable(bool enable)
        {
            List.Enabled = enable;
        }
        
        public List<Part> AllParts()
        {
            List<Part> parts = new();
            foreach (Part part in List.Items)
            {
                parts.Add(part);
            }
            return parts;
        }

        public (int Parts, int Triangles, double Volume) Totals()
        {
            int parts = 0;
            int triangles = 0;
            double volume = 0;
            foreach (Part p in List.Items)
            {
                parts += p.Quantity;
                triangles += p.Quantity * p.Triangles;
                volume += p.Quantity * p.Volume;
            }
            return (parts, triangles, volume);
        }

        public void Add(Part part) => List.Items.Add(part);
        public void RemoveAll() => List.Clear();

        public void ForEachItem(Action<Part> action)
        {
            foreach (Part part in List.Items)
            {
                action(part);
            }
        }

        public void ClearSelection()
        {
            List.SelectedItems.Clear();
        }

        public void MirrorCopySelectedItem()
        {
            Part part = SelectedItem;
            if (part.BasePart == null)
                return;

            Part copy = new Part(part.FileName, part.BasePart.Clone());

            copy.Quantity = part.Quantity;
            copy.RotateMinBox = part.RotateMinBox;
            copy.RotationIndex = part.RotationIndex;
            copy.MinHole = part.MinHole;
            copy.Mirrored = !part.Mirrored;

            copy.SetItems();
            copy.ReloadFile();
            List.Items.Add(copy);
        }

        public void ReloadSelectedItems()
        {
            if (SelectedItemCount > 0)
            {
                foreach (Part p in List.SelectedItems)
                    p.ReloadFile();
            }
            else
            {
                foreach (Part p in List.Items)
                    p.ReloadFile();
            }
        }

        public void RemoveSelectedItems()
        {
            foreach (Part selected in List.SelectedItems)
            {
                List.Items.Remove(selected);
            }
        }
    }
}
