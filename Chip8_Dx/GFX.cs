using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
//using SharpDX.Direct2D1;
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
using System.Windows.Input;

namespace Chip8_Dx {
    class GFX : IDisposable {
        private RenderForm renderForm;

        private const int Width = 1280;
        private const int Height = 720;
        public static byte[] keys = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        SharpDX.Color[] pixColor = new SharpDX.Color[] { SharpDX.Color.Black, SharpDX.Color.LimeGreen };
        bool step = true;
        bool DEBUG = false;
        private D3D11.Device d3dDevice;
        private D3D11.DeviceContext d3dDeviceContext;
        private SwapChain swapChain;
        private D3D11.RenderTargetView renderTargetView;
        private Viewport viewport;
        Random rand = new Random();
        private D3D11.VertexShader vertexShader;
        private D3D11.PixelShader pixelShader;
        private ShaderSignature inputSignature;
        private D3D11.InputLayout inputLayout;
        public static byte[] gfxOut = new byte[64 * 32];//2048 pix
        private D3D11.InputElement[] inputElements = new D3D11.InputElement[]{
            new D3D11.InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0, D3D11.InputClassification.PerVertexData, 0),
            new D3D11.InputElement("COLOR", 0, Format.R32G32B32A32_Float, 12, 0, D3D11.InputClassification.PerVertexData, 0)
        };

        Label lab_reg = new Label {
            Size = new Size(100, (int)(Height*.65f)),
            Location = new System.Drawing.Point(102, 0),
            Font = SystemFonts.DialogFont,
            BackColor = System.Drawing.Color.Black,
            ForeColor = System.Drawing.Color.LimeGreen,
            Visible = true,

        };
        Label lab_op = new Label {
            Size = new Size(400, (int)(Height * .35f)),
            Location = new System.Drawing.Point(102, (int)(Height * .65f)),
            Font = SystemFonts.DialogFont,
            Visible = true,
            BackColor = System.Drawing.Color.Black,
            ForeColor = System.Drawing.Color.LimeGreen

        };
        Button nStep = new Button {
            Size = new Size(90, 30),
            Location = new System.Drawing.Point(600, (int)(Height * .65f)),
            Font = SystemFonts.DialogFont,
            Visible = true,
            Text = "STEP",
            BackColor = System.Drawing.Color.Black,
            ForeColor = System.Drawing.Color.LimeGreen
        };

        ListBox lb = new ListBox {
            Size = new Size(100, Height),
            Location = new System.Drawing.Point(0, 0),
            SelectionMode = SelectionMode.MultiExtended,
            Font = SystemFonts.DialogFont,
            BackColor = System.Drawing.Color.Black,
            ForeColor = System.Drawing.Color.LimeGreen,
            Visible = true
        };


        private void nStep_Click(object sender, EventArgs e) {
            step = true;
        }
        public struct VertexPositionColor {
            public readonly Vector3 Position;
            public readonly Color4 Color;

            public VertexPositionColor(Vector3 position, Color4 color) {
                Position = position;
                Color = color;
            }
        }
        private VertexPositionColor[] pix = new VertexPositionColor[32 * 64 * 4];
        private D3D11.Buffer screenVertexBuffer;
//____________________________________________________________________________________________________________________________________________________________________________________________
        public GFX() {// Set window properties
            
            renderForm = new RenderForm("Chipper 8");
            renderForm.ClientSize = new Size(Width, Height);
            renderForm.AllowUserResizing = false;

            InitializeDeviceResources();
            InitializeShaders();
            for (int i = 0; i < 64 * 32; i++) {
                gfxOut[i] = 0;
            }
            InitBuffers();
            if (true) { CreateDebugGUI(); }

        }
//____________________________________________________________________________________________________________________________________________________________________________________________
        /// <summary>
        /// Start the game.
        /// </summary>
        public void Run() {
            
                RenderLoop.Run(renderForm, RenderCallback);
            
        }
//____________________________________________________________________________________________________________________________________________________________________________________________
        private void RenderCallback() {
            if (step) {
                Console.Write("FFFFFFFFFFFFFFFFFFFFFFFFFFFFF");
                CPU.emulateCycle();
                SYS_STATE();
                if (CPU.drawFlag) {
                    Draw();
                }
            }

            if (DEBUG) {
                step = false;
            }

        }
//____________________________________________________________________________________________________________________________________________________________________________________________
        private void InitializeDeviceResources() {
            ModeDescription backBufferDesc = new ModeDescription(Width, Height, new Rational(60, 1), Format.R8G8B8A8_UNorm);           
            SwapChainDescription swapChainDesc = new SwapChainDescription() {// Descriptor for the swap chain
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
//____________________________________________________________________________________________________________________________________________________________________________________________
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
        private void InitBuffers() {// Create a vertex buffer
            
            //for (int i = 0; i < 64 * 32; i++) {
            //    gfxOut[i] = (byte)rand.Next(0, 2);
            //}
            
            float Xoff = -.68f;
            float Yoff = -.29f;
            float X_scale = .02f * (Width/Height);
            float Y_scale = .04f ;
            for (int x = 0; x < 64; x++) {
                for (int y = 0; y < 32; y++) {
                    pix[(x * 128) + (y * 4)    ] = new VertexPositionColor(new Vector3((x * X_scale) + Xoff,       (y * Y_scale) + Yoff,               0), pixColor[gfxOut[x * y]]);
                    pix[(x * 128) + (y * 4) + 1] = new VertexPositionColor(new Vector3((x * X_scale) + Xoff,       (y * Y_scale) + Y_scale + Yoff,     0), pixColor[gfxOut[x * y]]);
                    pix[(x * 128) + (y * 4) + 2] = new VertexPositionColor(new Vector3((x * X_scale) + X_scale + Xoff, (y * Y_scale) + Yoff,           0), pixColor[gfxOut[x * y]]);
                    pix[(x * 128) + (y * 4) + 3] = new VertexPositionColor(new Vector3((x * X_scale) + X_scale + Xoff, (y * Y_scale) + Y_scale + Yoff, 0), pixColor[gfxOut[x * y]]);

                }
            }
            screenVertexBuffer = D3D11.Buffer.Create<VertexPositionColor>(d3dDevice, D3D11.BindFlags.VertexBuffer, pix);
        }

//____________________________________________________________________________________________________________________________________________________________________________________________
        private void Draw() {         
            InitBuffers();//MEMORY LEAK
            //d3dDeviceContext.UpdateSubresource(,);
            d3dDeviceContext.OutputMerger.SetRenderTargets(renderTargetView);// Set back buffer as current render target view          
            d3dDeviceContext.ClearRenderTargetView(renderTargetView, new RawColor4(0, 0, 0, 2));// Clear the screen
            d3dDeviceContext.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(screenVertexBuffer, Utilities.SizeOf<VertexPositionColor>(), 0));// Set vertex buffer
            SYS_STATE();
            for (int i = 0; i < 64 * 32*4; i += 4) {
                d3dDeviceContext.Draw(4, i);
                d3dDeviceContext.PixelShader.Set(pixelShader);
            }
            swapChain.Present(0, PresentFlags.None);// Swap front and back buffer
            screenVertexBuffer.Dispose();
            //RefreshMemDisplay();
        }
        //____________________________________________________________________________________________________________________________________________________________________________________________
        public void CreateDebugGUI() {
            nStep.MouseDown += nStep_Click;
            //nStep.Click += nStep_Click;
            renderForm.Controls.Add(lab_reg);
            renderForm.Controls.Add(lab_op);
            renderForm.Controls.Add(nStep);
            renderForm.Controls.Add(lb);
            //RefreshMemDisplay();
        }

        public void RefreshMemDisplay() {            
            lb.BringToFront();
            for (int i = 0; i < 4096; i++) {
                lb.Items.Add("0x" + i.ToString("X") + " == " + Memory.memory[i].ToString("X"));
            }
            //lb.Refresh();
        }


        protected void SYS_STATE() {
            //TextBox tb = new TextBox();
            //tb.Size = new Size(400, Height);
            //tb.Location = new System.Drawing.Point(200, 0);
            //tb.Text = "HYAA";
            //tb.Tag = "mem";
            //tb.Select();
            //renderForm.Controls.Add(tb);
            
            lab_reg.Text=(
                "___REGISTERS___" +
                "\nV[0] == " + CPU.V[0] +
                "\nV[1] == " + CPU.V[1] +
                "\nV[2] == " + CPU.V[2] +
                "\nV[3] == " + CPU.V[3] +
                "\nV[4] == " + CPU.V[4] +
                "\nV[5] == " + CPU.V[5] +
                "\nV[6] == " + CPU.V[6] +
                "\nV[7] == " + CPU.V[7] +
                "\nV[8] == " + CPU.V[8] +
                "\nV[9] == " + CPU.V[9] +
                "\nV[A] == " + CPU.V[10] +
                "\nV[B] == " + CPU.V[11] +
                "\nV[C] == " + CPU.V[12] +
                "\nV[D] == " + CPU.V[13] +
                "\nV[E] == " + CPU.V[14] +
                "\nV[F] == " + CPU.V[15] +

                "\n\n___STACK___" +
                "\nS[0] == " + CPU.stack[0] +
                "\nS[1] == " + CPU.stack[1] +
                "\nS[2] == " + CPU.stack[2] +
                "\nS[3] == " + CPU.stack[3] +
                "\nS[4] == " + CPU.stack[4] +
                "\nS[5] == " + CPU.stack[5] +
                "\nS[6] == " + CPU.stack[6] +
                "\nS[7] == " + CPU.stack[7] +
                "\nS[8] == " + CPU.stack[8] +
                "\nS[9] == " + CPU.stack[9] +
                "\nS[A] == " + CPU.stack[10] +
                "\nS[B] == " + CPU.stack[11] +
                "\nS[C] == " + CPU.stack[12] +
                "\nS[D] == " + CPU.stack[13] +
                "\nS[E] == " + CPU.stack[14] +
                "\nS[F] == " + CPU.stack[15]
                );
            lab_op.Text = (
                "___OPCODES___" +
                "\nPC == " + CPU.pc +
                "\nSP == " + CPU.sp +
                "\n\n0x" + CPU.opcode.ToString("X") +
                "\n" + CPU.opOut[0] +
                "\n" + CPU.opOut[1] +
                "\n" + CPU.opOut[2] +
                "\n" + CPU.opOut[3] +
                "\n" + CPU.opOut[4] +
                "\n" + CPU.opOut[5] +
                "\n" + CPU.opOut[6] +
                "\n" + CPU.opOut[7] +
                "\n" + CPU.opOut[8] +
                "\n" + CPU.opOut[9] +
                "\n\nDebug == " + CPU.dbgMsg 
            );
            lab_reg.Refresh();
            lab_op.Refresh();

        }


    public void Dispose() {
            inputLayout.Dispose();
            inputSignature.Dispose();
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