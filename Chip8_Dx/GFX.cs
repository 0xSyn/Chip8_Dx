using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D3D11 = SharpDX.Direct3D11;
using System.Windows.Forms;

namespace Chip8_Dx {
    class GFX : IDisposable {
        private RenderForm renderForm;

        private const int Width = 1280;
        private const int Height = 720;
        SharpDX.Color[] pixColor = new SharpDX.Color[] { SharpDX.Color.Black, SharpDX.Color.Red };
        private D3D11.Device d3dDevice;
        private D3D11.DeviceContext d3dDeviceContext;
        private SwapChain swapChain;
        private D3D11.RenderTargetView renderTargetView;
        private Viewport viewport;
        Random rand = new Random();
        // Shaders
        private D3D11.VertexShader vertexShader;

        private D3D11.PixelShader pixelShader;
        private ShaderSignature inputSignature;
        private D3D11.InputLayout inputLayout;
        public static byte[] gfxOut = new byte[64 * 32];//2048 pix
        
        
        //private D3D11.InputElement[] inputElements = new D3D11.InputElement[]
        //{
        //    new D3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0)
        //};
        private D3D11.InputElement[] inputElements = new D3D11.InputElement[]{
            new D3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, D3D11.InputClassification.PerVertexData, 0),
            new D3D11.InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0, D3D11.InputClassification.PerVertexData, 0)
        };


        public struct VertexPositionColor {
            public readonly Vector3 Position;
            public readonly Color4 Color;

            public VertexPositionColor(Vector3 position, Color4 color) {
                Position = position;
                Color = color;
            }
        }


        // Triangle vertices
        private RawVector3[] vertices = new RawVector3[] {
            new RawVector3(-0.5f,  0.5f, 0.0f),
            new RawVector3( 0.5f,  0.5f, 0.0f),
            new RawVector3( 0.0f, -1.0f, 0.0f)
        };
        
        private RawVector3[] rectVerts = new RawVector3[] {
            new RawVector3(-0.7f,  -0.5f, 0.0f),
            new RawVector3( -0.5f,  -0.5f, 0.0f),
            new RawVector3(-0.7f, -0.7f, 0.0f),
            new RawVector3( -0.5f, -0.7f, 0.0f),

            new RawVector3(-0.5f,  0.5f, 0.0f),
            new RawVector3( 0.5f,  0.5f, 0.0f),
            new RawVector3(-0.5f, -0.5f, 0.0f),
            new RawVector3( 0.5f, -0.5f, 0.0f)
        };

        private RawVector3[] verts = new RawVector3[32*64*4];

        private VertexPositionColor[] pix = new VertexPositionColor[32 * 64 * 4];

        private D3D11.Buffer triangleVertexBuffer;
        private D3D11.Buffer rectVertexBuffer;
        private D3D11.Buffer screenVertexBuffer;

        public GFX() {// Set window properties

            renderForm = new RenderForm("Chipper 8");
            renderForm.ClientSize = new Size(Width, Height);
            renderForm.AllowUserResizing = false;

            InitializeDeviceResources();
            InitializeShaders();
            InitBuffers();

            RefreshMemDisplay();
        }

        /// <summary>
        /// Start the game.
        /// </summary>
        public void Run() {
            if (CPU.drawFlag) {
                RenderLoop.Run(renderForm, RenderCallback);
            }
        }

        private void RenderCallback() {
            Draw();
            
        }

        private void InitializeDeviceResources() {
            ModeDescription backBufferDesc = new ModeDescription(Width, Height, new Rational(60, 1), Format.R8G8B8A8_UNorm);

            // Descriptor for the swap chain
            SwapChainDescription swapChainDesc = new SwapChainDescription() {
                ModeDescription = backBufferDesc,
                SampleDescription = new SampleDescription(1, 0),
                Usage = Usage.RenderTargetOutput,
                BufferCount = 2,
                OutputHandle = renderForm.Handle,
                IsWindowed = true
            };

            // Create device and swap chain
            D3D11.Device.CreateWithSwapChain(DriverType.Hardware, D3D11.DeviceCreationFlags.None, swapChainDesc, out d3dDevice, out swapChain);
            d3dDeviceContext = d3dDevice.ImmediateContext;

            viewport = new Viewport(0, 0, Width, Height);
            d3dDeviceContext.Rasterizer.SetViewport(viewport);

            // Create render target view for back buffer
            using (D3D11.Texture2D backBuffer = swapChain.GetBackBuffer<D3D11.Texture2D>(0)) {
                renderTargetView = new D3D11.RenderTargetView(d3dDevice, backBuffer);
            }
        }

        private void InitializeShaders() {// Compile the vertex/pixel shaders
            using (var vertexShaderByteCode = ShaderBytecode.CompileFromFile("vertexShader.hlsl", "main", "vs_4_0", ShaderFlags.Debug)) {
                inputSignature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
                vertexShader = new D3D11.VertexShader(d3dDevice, vertexShaderByteCode);
            }

            using (var pixelShaderByteCode = ShaderBytecode.CompileFromFile("pixelShader.hlsl", "main", "ps_4_0", ShaderFlags.Debug)) {
                pixelShader = new D3D11.PixelShader(d3dDevice, pixelShaderByteCode);
            }


            // Set as current vertex and pixel shaders
            d3dDeviceContext.VertexShader.Set(vertexShader);
            d3dDeviceContext.PixelShader.Set(pixelShader);
            d3dDeviceContext.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            
            inputLayout = new D3D11.InputLayout(d3dDevice, inputSignature, inputElements);// Create the input layout from the input signature and the input elements 
            d3dDeviceContext.InputAssembler.InputLayout = inputLayout;// Set input layout to use
        }
//____________________________________________________________________________________________________________________________________________________________________________________________
        private void InitBuffers() {// Create a vertex buffer, and use our array with vertices as data
            
            for (int i = 0; i < 64 * 32; i++) {
                gfxOut[i] = (byte)rand.Next(0, 2);
            }
            


            float Xoff = -.6f;
            float Yoff = -.9f;
            float X_scale = .01f * (Width/Height);
            float Y_scale = .02f ;
            for (int x = 0; x < 64; x++) {
                for (int y = 0; y < 32; y++) {
                    pix[(x * 128) + (y * 4)    ] = new VertexPositionColor(new Vector3((x * X_scale) + Xoff,       (y * Y_scale) + Yoff,               0), pixColor[gfxOut[x * y]]);
                    pix[(x * 128) + (y * 4) + 1] = new VertexPositionColor(new Vector3((x * X_scale) + Xoff,       (y * Y_scale) + Y_scale + Yoff,     0), pixColor[gfxOut[x * y]]);
                    pix[(x * 128) + (y * 4) + 2] = new VertexPositionColor(new Vector3((x * X_scale) + X_scale + Xoff, (y * Y_scale) + Yoff,           0), pixColor[gfxOut[x * y]]);
                    pix[(x * 128) + (y * 4) + 3] = new VertexPositionColor(new Vector3((x * X_scale) + X_scale + Xoff, (y * Y_scale) + Y_scale + Yoff, 0), pixColor[gfxOut[x * y]]);

                }
            }
            screenVertexBuffer = D3D11.Buffer.Create<VertexPositionColor>(d3dDevice, D3D11.BindFlags.VertexBuffer, pix);
            //triangleVertexBuffer = D3D11.Buffer.Create<RawVector3>(d3dDevice, D3D11.BindFlags.VertexBuffer, vertices);
            //rectVertexBuffer = D3D11.Buffer.Create<RawVector3>(d3dDevice, D3D11.BindFlags.VertexBuffer, rectVerts);
        }

//____________________________________________________________________________________________________________________________________________________________________________________________
        private void Draw() {
            InitBuffers();
            //d3dDeviceContext.UpdateSubresource(,);
            d3dDeviceContext.OutputMerger.SetRenderTargets(renderTargetView);// Set back buffer as current render target view          
            d3dDeviceContext.ClearRenderTargetView(renderTargetView, new RawColor4(0, 0, 178, 2));// Clear the screen

            // Set vertex buffer
            
            //d3dDeviceContext.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(rectVertexBuffer, Utilities.SizeOf<RawVector3>(), 0));
            //d3dDeviceContext.Draw(rectVerts.Count(), 0);
            //d3dDeviceContext.Draw(4, 0);
            //d3dDeviceContext.Draw(4, 4);
            //d3dDeviceContext.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(triangleVertexBuffer, Utilities.SizeOf<RawVector3>(), 0));
            //d3dDeviceContext.Draw(vertices.Count(), 0);

            d3dDeviceContext.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(screenVertexBuffer, Utilities.SizeOf<VertexPositionColor>(), 0));
            for (int i = 0; i < 64 * 32*4; i += 4) {
                d3dDeviceContext.Draw(4, i);
                d3dDeviceContext.PixelShader.Set(pixelShader);
            }

            
            swapChain.Present(0, PresentFlags.None);// Swap front and back buffer
            screenVertexBuffer.Dispose();


        }
//____________________________________________________________________________________________________________________________________________________________________________________________
        public void RefreshMemDisplay() {


            ListBox lb = new ListBox {
                Size = new Size(200, Height),
                Location = new System.Drawing.Point(0, 0),
                SelectionMode = SelectionMode.MultiExtended,
                Font = SystemFonts.DialogFont,
                Visible = true
            };
            renderForm.Controls.Add(lb);
            lb.BringToFront();
            for (int i = 0; i < 4096; i++) {
                lb.Items.Add("0x" + i.ToString("X") + " == " + Memory.memory[i].ToString("X"));
            }


            TextBox tb = new TextBox();
            tb.Text = "HYAA";
            tb.Tag = "mem";
            tb.Select();
            //this.Controls.Add(tb);
            lb.Items.Add(tb);

        }

        public void Dispose() {
            inputLayout.Dispose();
            inputSignature.Dispose();
            triangleVertexBuffer.Dispose();
            rectVertexBuffer.Dispose();
            screenVertexBuffer.Dispose();
            vertexShader.Dispose();
            pixelShader.Dispose();

            renderTargetView.Dispose();
            swapChain.Dispose();
            d3dDevice.Dispose();
            d3dDeviceContext.Dispose();
            renderForm.Dispose();
        }
    }
}