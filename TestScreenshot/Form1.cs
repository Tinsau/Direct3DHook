﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using EasyHook;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting;
using System.Runtime.InteropServices;
using System.IO;
using Capture.Interface;
using Capture.Hook;
using Capture;

namespace TestScreenshot
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void btnInject_Click(object sender, EventArgs e)
        {
            if (_captureProcess == null)
            {
                btnInject.Enabled = false;

                if (cbAutoGAC.Checked)
                {
                    // NOTE: On some 64-bit setups this doesn't work so well.
                    //       Sometimes if using a 32-bit target, it will not find the GAC assembly
                    //       without a machine restart, so requires manual insertion into the GAC
                    // Alternatively if the required assemblies are in the target applications
                    // search path they will load correctly.

                    // Must be running as Administrator to allow dynamic registration in GAC
                    Config.Register("Capture",
                        "Capture.dll");
                }
                
                IntPtr destHandle = NativeMethods.StartSearchWindowDlg(this.Handle);
                int processID = 0;
                int threadID = NativeMethods.GetWindowThreadProcessId((IntPtr)destHandle, out processID);
                AttachProcessEx(processID);
            }
            else
            {
                HookManager.RemoveHookedProcess(_captureProcess.Process.Id);
                _captureProcess.CaptureInterface.Disconnect();
                _captureProcess = null;
            }

            if (_captureProcess != null)
            {
                btnInject.Text = "Detach";
                btnInject.Enabled = true;
            }
            else
            {
                btnInject.Text = "Inject";
                btnInject.Enabled = true;
            }
        }

        int processId = 0;
        Process _process;
        CaptureProcess _captureProcess;
        private void AttachProcess()
        {
            string exeName = Path.GetFileNameWithoutExtension(textBox1.Text);
            
            Process[] processes = Process.GetProcessesByName(exeName);
            foreach (Process process in processes)
            {
                // Simply attach to the first one found.

                // If the process doesn't have a mainwindowhandle yet, skip it (we need to be able to get the hwnd to set foreground etc)
                if (process.MainWindowHandle == IntPtr.Zero)
                {
                    continue;
                }

                // Skip if the process is already hooked (and we want to hook multiple applications)
                if (HookManager.IsHooked(process.Id))
                {
                    continue;
                }

                Direct3DVersion direct3DVersion = Direct3DVersion.Direct3D10;

                if (rbDirect3D11.Checked)
                {
                    direct3DVersion = Direct3DVersion.Direct3D11;
                }
                else if (rbDirect3D10_1.Checked)
                {
                    direct3DVersion = Direct3DVersion.Direct3D10_1;
                }
                else if (rbDirect3D10.Checked)
                {
                    direct3DVersion = Direct3DVersion.Direct3D10;
                }
                else if (rbDirect3D9.Checked)
                {
                    direct3DVersion = Direct3DVersion.Direct3D9;
                }
                else if (rbAutodetect.Checked)
                {
                    direct3DVersion = Direct3DVersion.AutoDetect;
                }

                CaptureConfig cc = new CaptureConfig()
                {
                    Direct3DVersion = direct3DVersion,
                    ShowOverlay = cbDrawOverlay.Checked
                };

                processId = process.Id;
                _process = process;

                var captureInterface = new CaptureInterface();
                captureInterface.RemoteMessage += new MessageReceivedEvent(CaptureInterface_RemoteMessage);
                _captureProcess = new CaptureProcess(process, cc, captureInterface);
                
                break;
            }
            Thread.Sleep(10);

            if (_captureProcess == null)
            {
                MessageBox.Show("No executable found matching: '" + exeName + "'");
            }
            else
            {
                btnLoadTest.Enabled = true;
                btnCapture.Enabled = true;
            }
        }

        private void AttachProcessEx(int processID)
        {
            string exeName = Path.GetFileNameWithoutExtension(textBox1.Text);

            Process process = Process.GetProcessById(processID);
            if (process != null)
            {
                textBox1.Text = process.ProcessName;
            }
            do
            {
                // Simply attach to the first one found.

                // If the process doesn't have a mainwindowhandle yet, skip it (we need to be able to get the hwnd to set foreground etc)
                if (process.MainWindowHandle == IntPtr.Zero)
                {
                    break;
                }

                // Skip if the process is already hooked (and we want to hook multiple applications)
                if (HookManager.IsHooked(process.Id))
                {
                    break;
                }

                Direct3DVersion direct3DVersion = Direct3DVersion.Direct3D10;

                if (rbDirect3D11.Checked)
                {
                    direct3DVersion = Direct3DVersion.Direct3D11;
                }
                else if (rbDirect3D10_1.Checked)
                {
                    direct3DVersion = Direct3DVersion.Direct3D10_1;
                }
                else if (rbDirect3D10.Checked)
                {
                    direct3DVersion = Direct3DVersion.Direct3D10;
                }
                else if (rbDirect3D9.Checked)
                {
                    direct3DVersion = Direct3DVersion.Direct3D9;
                }
                else if (rbAutodetect.Checked)
                {
                    direct3DVersion = Direct3DVersion.AutoDetect;
                }

                CaptureConfig cc = new CaptureConfig()
                {
                    Direct3DVersion = direct3DVersion,
                    ShowOverlay = cbDrawOverlay.Checked
                };

                processId = process.Id;
                _process = process;

                var captureInterface = new CaptureInterface();
                captureInterface.RemoteMessage += new MessageReceivedEvent(CaptureInterface_RemoteMessage);
                _captureProcess = new CaptureProcess(process, cc, captureInterface);

                break;
            } while (false);
            Thread.Sleep(10);

            if (_captureProcess == null)
            {
                MessageBox.Show("No executable found matching: '" + exeName + "'");
            }
            else
            {
                btnLoadTest.Enabled = true;
                btnCapture.Enabled = true;
            }
        }

        /// <summary>
        /// Display messages from the target process
        /// </summary>
        /// <param name="message"></param>
        void CaptureInterface_RemoteMessage(MessageReceivedEventArgs message)
        {
            txtDebugLog.Invoke(new MethodInvoker(delegate()
                {
                    txtDebugLog.Text = String.Format("{0}\r\n{1}", message, txtDebugLog.Text);
                })
            );
        }

        /// <summary>
        /// Display debug messages from the target process
        /// </summary>
        /// <param name="clientPID"></param>
        /// <param name="message"></param>
        void ScreenshotManager_OnScreenshotDebugMessage(int clientPID, string message)
        {
            txtDebugLog.Invoke(new MethodInvoker(delegate()
                {
                    txtDebugLog.Text = String.Format("{0}:{1}\r\n{2}", clientPID, message, txtDebugLog.Text);
                })
            );
        }

        DateTime start;
        DateTime end;

        private void btnCapture_Click(object sender, EventArgs e)
        {
            start = DateTime.Now;
            progressBar1.Maximum = 1;
            progressBar1.Step = 1;
            progressBar1.Value = 0;

            DoRequest();
        }

        private void btnLoadTest_Click(object sender, EventArgs e)
        {
            // Note: we bring the target application into the foreground because
            //       windowed Direct3D applications have a lower framerate 
            //       if not the currently focused window
            _captureProcess.BringProcessWindowToFront();
            start = DateTime.Now;
            progressBar1.Maximum = Convert.ToInt32(txtNumber.Text);
            progressBar1.Minimum = 0;
            progressBar1.Step = 1;
            progressBar1.Value = 0;
            DoRequest();
        }

        /// <summary>
        /// Create the screen shot request
        /// </summary>
        void DoRequest()
        {
            progressBar1.Invoke(new MethodInvoker(delegate()
                {
                    if (progressBar1.Value < progressBar1.Maximum)
                    {
                        progressBar1.PerformStep();

                        _captureProcess.BringProcessWindowToFront();
                        // Initiate the screenshot of the CaptureInterface, the appropriate event handler within the target process will take care of the rest
                        Size? resize = null;
                        if (!String.IsNullOrEmpty(txtResizeHeight.Text) && !String.IsNullOrEmpty(txtResizeWidth.Text))
                            resize = new System.Drawing.Size(int.Parse(txtResizeWidth.Text), int.Parse(txtResizeHeight.Text));
                        _captureProcess.CaptureInterface.BeginGetScreenshot(new Rectangle(int.Parse(txtCaptureX.Text), int.Parse(txtCaptureY.Text), int.Parse(txtCaptureWidth.Text), int.Parse(txtCaptureHeight.Text)), new TimeSpan(0, 0, 2), Callback, resize, (ImageFormat)Enum.Parse(typeof(ImageFormat), cmbFormat.Text));
                    }
                    else
                    {
                        end = DateTime.Now;
                        txtDebugLog.Text = String.Format("Debug: {0}\r\n{1}", "Total Time: " + (end-start).ToString(), txtDebugLog.Text);
                    }
                })
            );
        }

        /// <summary>
        /// The callback for when the screenshot has been taken
        /// </summary>
        /// <param name="clientPID"></param>
        /// <param name="status"></param>
        /// <param name="screenshotResponse"></param>
        void Callback(IAsyncResult result)
        {
            using (Screenshot screenshot = _captureProcess.CaptureInterface.EndGetScreenshot(result))
            try
            {
                _captureProcess.CaptureInterface.DisplayInGameText("Screenshot captured...");
                if (screenshot != null && screenshot.Data != null)
                {
                    pictureBox1.Invoke(new MethodInvoker(delegate()
                    {
                        if (pictureBox1.Image != null)
                        {
                            pictureBox1.Image.Dispose();
                        }
                        pictureBox1.Image = screenshot.ToBitmap();
                    })
                    );
                }

                Thread t = new Thread(new ThreadStart(DoRequest));
                t.Start();
            }
            catch
            {
            }
        }

        private void btnDisplayOverlay_Click(object sender, EventArgs e)
        {
            _captureProcess.CaptureInterface.DrawOverlayInGame(new Capture.Hook.Common.Overlay
            {
                Elements = new List<Capture.Hook.Common.IOverlayElement>
                {
                    new Capture.Hook.Common.FramesPerSecond(new System.Drawing.Font("Arial", 16, FontStyle.Bold)) {
                            Location = new Point(25, 25),
                            Color = Color.Red,
                            AntiAliased = true,
                            Text = "{0:N0} fps"
                        },
                },
                Hidden = !cbDrawOverlay.Checked
            });
        }
    }
}
