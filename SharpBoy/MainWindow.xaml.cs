using Microsoft.Win32;
using SharpBoy.GameBoySystem;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SharpBoy
{
    public partial class MainWindow : Window
    {
        // Used for loading roms and loading save states
        private delegate void LoadMethod(string filename);

        private readonly Int32Rect renderRectangle;

        private IEmulator emulator;
        private Thread emulatorThread;
        private WriteableBitmap renderFrame;

        public MainWindow()
        {
            InitializeComponent();

            emulator = new GameBoyEmulator();
            emulator.RenderHandler += emulator_Render;
            DataContext = emulator;

            renderFrame = new WriteableBitmap(emulator.RenderWidth, emulator.RenderHeight,
                96, 96, PixelFormats.Gray8, null);
            renderedImage.Source = renderFrame;
            // To be used for writing a new frame to renderFrame
            renderRectangle = new Int32Rect(0, 0, emulator.RenderWidth, emulator.RenderHeight);
        }

        private void StopEmulation()
        {
            if (emulatorThread != null && emulatorThread.IsAlive)
            {
                emulator.Stop = true;
                emulatorThread.Join(1000);
            }
        }

        private void StartEmulation()
        {
            emulatorThread = new Thread(new ThreadStart(emulator.Run));
            emulatorThread.Start();
        }

        private void Render(byte[] screenData)
        {
            renderFrame.WritePixels(renderRectangle, screenData, emulator.RenderWidth, 0);
        }

        private void LoadFile(LoadMethod loadMethod, string filter = "Game Boy Roms|*.gb")
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = filter;
            bool? fileChosen = dialog.ShowDialog();

            if (fileChosen == true)
            {
                try
                {
                    StopEmulation();
                    loadMethod(dialog.FileName);
                    StartEmulation();
                }
                catch (SystemException ex)
                {
                    MessageBox.Show(ex.Message, "Failed to Load File",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void emulator_Render(byte[] screenData)
        {
            // The emulator thread cannot access the render frame, hence the
            // call to Dispatcher.BeginInvoke(), instead of rendering directly.
            App.Current.Dispatcher.BeginInvoke((Action<byte[]>)Render, screenData);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            emulator.KeyDown(e.Key);
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            emulator.KeyUp(e.Key);
        }

        private void FileExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void LoadRom_Click(object sender, RoutedEventArgs e)
        {
            LoadFile(emulator.LoadRom);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // We stop emulation to prevent the emulator thread from making
            // render event calls on this window after it has been destroyed,
            // which would result in a NullReferenceException.
            StopEmulation();
        }

        private void menu_save_Click(object sender, RoutedEventArgs e)
        {
            if (emulatorThread != null && emulatorThread.IsAlive)
            {
                emulator.SaveState();
            }
        }

        private void menu_loadState_Click(object sender, RoutedEventArgs e)
        {
            LoadFile(emulator.LoadState, "Save States|*.sav*");
        }
    }
}
