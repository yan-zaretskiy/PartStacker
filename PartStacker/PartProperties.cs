using PartStacker.Geometry;

namespace PartStacker
{
    public class PartProperties
    {
        public required Mesh BaseMesh;
        public required double Volume;
        public required int TriangleCount;

        public required int Quantity;

        public required int MinHole;
        public required bool RotateMinBox;
        public required int RotationIndex;
    }
}
