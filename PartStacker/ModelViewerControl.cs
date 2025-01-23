#region File Description
//-----------------------------------------------------------------------------
// ModelViewerControl.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using WinFormsContentLoading;
using PartStacker.Geometry;
#endregion

namespace PartStacker
{
    /// <summary>
    /// Example control inherits from GraphicsDeviceControl, and displays
    /// a spinning 3D model. The main form class is responsible for loading
    /// the model: this control just displays it.
    /// </summary>
    
    public class ModelViewerControl : GraphicsDeviceControl
    {
        public int TriangleCount;
        public Vector3 BB;
        bool section = false;

        float zoom = 100;
        Quaternion modelRotation = Quaternion.Identity;
        System.Drawing.Point oldPos;

        BasicEffect effect;

        public VertexPositionColorNormal[] triangles;

        protected override void Initialize()
        {
            effect = new BasicEffect(GraphicsDevice);
            SetupEffect();

            this.MouseMove += MoveHandler;
            this.MouseWheel += ScrollHandler;
            this.MouseEnter += (o, e) => { this.Focus(); };
        }

        private void MoveHandler(object o, MouseEventArgs mea)
        {
            if (mea.Button == MouseButtons.Left && TriangleCount > 0)
            {
                float dx = mea.Location.X - oldPos.X;
                float dy = mea.Location.Y - oldPos.Y;

                modelRotation = Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), dy / 150) * modelRotation;
                modelRotation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), dx / 150) * modelRotation;

                Invalidate();
            }
            oldPos = mea.Location;
        }

        private void ScrollHandler(object o, MouseEventArgs mea)
        {
            if (mea.Delta != 0)
            {
                zoom *= (float)Math.Pow(1.0017, mea.Delta);
                zoom = (float)Math.Max(Math.Min(zoom, 275), 7);

                Invalidate();
            }
        }

        protected override void Draw()
        {
            Color backColor = new Color(BackColor.R, BackColor.G, BackColor.B);
            GraphicsDevice.Clear(backColor);
            effect.World = Matrix.CreateTranslation(-0.5f * BB) * Matrix.CreateFromQuaternion(modelRotation);
            effect.View = Matrix.CreateLookAt(new Vector3(0, 0, zoom), new Vector3(0, 0, 0), new Vector3(0, 1, 0));

            if (TriangleCount > 0)
            {
                for (int i = 0; i <= TriangleCount / 65535; i++)
                    foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, triangles, 3 * 65535 * i, Math.Min(65535, TriangleCount - 65535 * i), VertexPositionColorNormal.VertexDeclaration);
                        //GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 3 * 65535 * i, Math.Min(65535, TriangleCount - 65535 * i));
                    }
            }
        }

        protected void SetupEffect()
        {
            effect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1.0f, 11.0f, 300.0f);
            effect.VertexColorEnabled = true;
            GraphicsDevice.RasterizerState = new RasterizerState() { CullMode = CullMode.None };
            //GraphicsDevice.RasterizerState = new RasterizerState() { CullMode = CullMode.None, FillMode = FillMode.WireFrame };

            GraphicsDevice.BlendState = BlendState.AlphaBlend;

            effect.LightingEnabled = true;
            effect.DirectionalLight0.Enabled = true;
            effect.DirectionalLight0.DiffuseColor = new Vector3(0.6f);
            effect.DirectionalLight0.SpecularColor = new Vector3(0.01f);
            effect.AmbientLightColor = new Vector3(0.2f);
            effect.DirectionalLight0.Direction = new Vector3(0, 0, -1);
        }

        public bool Section
        {
            get
            {
                return section;
            }
            set
            {
                section = value;
                if(section)
                    effect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1.0f, 37.0f, 450.0f);
                else
                    effect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 1.0f, 12.0f, 450.0f);

                Invalidate();
            }
        }

        private static Vector3 ToVector3(Vector vec) => new Vector3(vec.X, vec.Y, vec.Z);
        private static Vector3 ToVector3(Point3 point) => new Vector3(point.X, point.Y, point.Z);

        public void SetMesh(STLBody mesh)
        {
            TriangleCount = 0;
            triangles = null;
            GC.AddMemoryPressure(mesh.Triangles.Count * 3 * 40 + 1);
            GC.Collect();

            VertexPositionColorNormal[] buffer = new VertexPositionColorNormal[mesh.Triangles.Count * 3];

            for (int i = 0; i < mesh.Triangles.Count; i++)
            {
                buffer[3 * i + 0] = new VertexPositionColorNormal(ToVector3(mesh.Triangles[i].v1), Microsoft.Xna.Framework.Color.White, ToVector3(mesh.Triangles[i].Normal));
                buffer[3 * i + 1] = new VertexPositionColorNormal(ToVector3(mesh.Triangles[i].v2), Microsoft.Xna.Framework.Color.White, ToVector3(mesh.Triangles[i].Normal));
                buffer[3 * i + 2] = new VertexPositionColorNormal(ToVector3(mesh.Triangles[i].v3), Microsoft.Xna.Framework.Color.White, ToVector3(mesh.Triangles[i].Normal));
            }

            triangles = buffer;
            TriangleCount = mesh.Triangles.Count;
            BB = ToVector3(mesh.size);
            Invalidate();
        }

        public void SetMeshWithVoxels(STLBody mesh, int[,,] voxels, int volume)
        {
            TriangleCount = 0;
            triangles = null;
            GC.AddMemoryPressure(40 * (mesh.Triangles.Count * 3 + volume * 36));
            GC.Collect();

            VertexPositionColorNormal[] buffer = new VertexPositionColorNormal[mesh.Triangles.Count * 3 + volume * 36];

            for (int i = 0; i < mesh.Triangles.Count; i++)
            {
                buffer[3 * i + 0] = new VertexPositionColorNormal(ToVector3(mesh.Triangles[i].v1), Microsoft.Xna.Framework.Color.White, ToVector3(mesh.Triangles[i].Normal));
                buffer[3 * i + 1] = new VertexPositionColorNormal(ToVector3(mesh.Triangles[i].v2), Microsoft.Xna.Framework.Color.White, ToVector3(mesh.Triangles[i].Normal));
                buffer[3 * i + 2] = new VertexPositionColorNormal(ToVector3(mesh.Triangles[i].v3), Microsoft.Xna.Framework.Color.White, ToVector3(mesh.Triangles[i].Normal));
            }

            int pos = mesh.Triangles.Count * 3;
            Color c = Color.Aqua;
            c.A = (byte)(256 * 0.8f);

            for (int x = 0; x < voxels.GetLength(0); x++)
                for (int y = 0; y < voxels.GetLength(1); y++)
                    for (int z = 0; z < voxels.GetLength(2); z++)
                        if (voxels[x, y, z] == 1)
                        {
                            float xt = (float)x - 1.0f;
                            float yt = (float)y - 1.0f;
                            float zt = (float)z - 1.0f;

                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 0, zt + 0), c, new Vector3(-1, 0, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 1, zt + 0), c, new Vector3(-1, 0, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 1, zt + 1), c, new Vector3(-1, 0, 0));

                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 0, zt + 0), c, new Vector3(-1, 0, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 0, zt + 1), c, new Vector3(-1, 0, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 1, zt + 1), c, new Vector3(-1, 0, 0));

                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 0, zt + 0), c, new Vector3(1, 0, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 1, zt + 0), c, new Vector3(1, 0, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 1, zt + 1), c, new Vector3(1, 0, 0));

                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 0, zt + 0), c, new Vector3(1, 0, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 0, zt + 1), c, new Vector3(1, 0, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 1, zt + 1), c, new Vector3(1, 0, 0));


                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 0, zt + 0), c, new Vector3(0, -1, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 0, zt + 0), c, new Vector3(0, -1, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 0, zt + 1), c, new Vector3(0, -1, 0));

                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 0, zt + 0), c, new Vector3(0, -1, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 0, zt + 1), c, new Vector3(0, -1, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 0, zt + 1), c, new Vector3(0, -1, 0));

                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 1, zt + 0), c, new Vector3(0, 1, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 1, zt + 0), c, new Vector3(0, 1, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 1, zt + 1), c, new Vector3(0, 1, 0));

                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 1, zt + 0), c, new Vector3(0, 1, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 1, zt + 1), c, new Vector3(0, 1, 0));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 1, zt + 1), c, new Vector3(0, 1, 0));


                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 0, zt + 0), c, new Vector3(0, 0, -1));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 0, zt + 0), c, new Vector3(0, 0, -1));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 1, zt + 0), c, new Vector3(0, 0, -1));

                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 0, zt + 0), c, new Vector3(0, 0, -1));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 1, zt + 0), c, new Vector3(0, 0, -1));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 1, zt + 0), c, new Vector3(0, 0, -1));

                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 0, zt + 1), c, new Vector3(0, 0, 1));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 0, zt + 1), c, new Vector3(0, 0, 1));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 1, zt + 1), c, new Vector3(0, 0, 1));

                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 0, zt + 1), c, new Vector3(0, 0, 1));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 0, yt + 1, zt + 1), c, new Vector3(0, 0, 1));
                            buffer[pos++] = new VertexPositionColorNormal(new Vector3(xt + 1, yt + 1, zt + 1), c, new Vector3(0, 0, 1));
                        }


            TriangleCount = buffer.Length / 3;
            triangles = buffer;
            BB = ToVector3(mesh.size);
            Invalidate();
        }
    }
}
