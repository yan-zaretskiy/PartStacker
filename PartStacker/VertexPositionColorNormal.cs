using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PartStacker
{
    public struct VertexPositionColorNormal
    {
        public Vector3 Position;
        public Color Color;
        public Vector3 Normal;

        public VertexPositionColorNormal(Vector3 position, Color color, Vector3 normal)
        {
            Position = position;
            Color = color;
            Normal = normal;
        }

        private readonly static VertexElement[] VertexElements =
        [
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(sizeof(float) * 3, VertexElementFormat.Color, VertexElementUsage.Color, 0),
            new VertexElement(sizeof(float) * 3 + 4, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
        ];
        public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration(VertexElements);
    }
}
