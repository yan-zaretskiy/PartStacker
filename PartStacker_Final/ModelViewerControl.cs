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
#endregion

namespace PartStacker_Final
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
    }
}
