using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;
using RemembMe.Properties;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace RemembMe
{

    public partial class RemembMe : Form
    {
        public RemembMe()
        {
            InitializeComponent();
            _instance = this;
            notifyIcon1.Icon = Resources.Untitled3;
            LoadHotkeyFromSettings();
        }

        public void ChangeHotKey(string HotKey)
        {
            this.textBox1.Text = HotKey;
        }

        Form2 newForm = new Form2();

        private static RemembMe _instance;

        Bitmap ScreenShot = null;
        Point LocPress;
        Point LocUp;
        Point LocNow;
        bool RectangleFlag = false;

        //Всё для хука
        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr hInstance, uint threadId);

        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, int wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);


        private bool isSelectingRectangle = false; // условие выделения области для скрина
        private bool isScreenStart = false; //условие начала процеса скрина

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        const int WH_KEYBOARD_LL = 13; // Номер глобального LowLevel-хука на клавиатуру
        const int WM_KEYDOWN = 0x100; // Сообщения нажатия клавиши

        private LowLevelKeyboardProc _proc = hookProc;

        private static IntPtr hhook = IntPtr.Zero;

        static bool ctrlPressed = false;
        static bool ShiftPressed = false;

        public void SetHook()
        {
            IntPtr hInstance = LoadLibrary("User32");
            hhook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, hInstance, 0);
        }
        public static void UnHook()
        {
            UnhookWindowsHookEx(hhook);
        }

        public class Hotkey
        {
            public bool Ctrl { get; set; }
            public bool Shift { get; set; }
            public Keys Key { get; set; }

            public Hotkey(bool ctrl, bool shift,  Keys key)
            {
                Ctrl = ctrl;
                Shift = shift;
                Key = key;
            }

            public override string ToString()
            {
                var parts = new List<string>();
                if (Ctrl) parts.Add("Ctrl");
                if (Shift) parts.Add("Shift");
                if (Key != Keys.None) parts.Add(Key.ToString());

                return string.Join(" + ", parts);
            }
        }
        public static Hotkey ParseHotkey(string hotkeyString)
        {
            if (string.IsNullOrWhiteSpace(hotkeyString))
                throw new ArgumentException("Горячие клавиши не найдены");

            var parts = hotkeyString.Split('+');
            bool ctrl = false, shift = false;
            Keys key = Keys.None;

            foreach (var part in parts)
            {
                string trimmedPart = part.Trim(); // Убираем пробелы
                if (string.Equals(trimmedPart, "Ctrl", StringComparison.OrdinalIgnoreCase))
                {
                    ctrl = true;
                }
                else if (string.Equals(trimmedPart, "Shift", StringComparison.OrdinalIgnoreCase))
                {
                    shift = true;
                }
                else if (Enum.TryParse(trimmedPart, true, out Keys parsedKey))
                {
                    key = parsedKey;
                }
            }

            if (key == Keys.None)
            {
                throw new ArgumentException($"Не доступная комбинация клавиш: {hotkeyString}");
            }

            return new Hotkey(ctrl, shift, key);
        }

        private static Hotkey screenshotHotkey;

        public static void LoadHotkeyFromSettings()
        {
            string hotkeySetting = Settings.Default.Hotkey;

            if (string.IsNullOrWhiteSpace(hotkeySetting))
            {
                hotkeySetting = "Ctrl+Q";
                Settings.Default.Hotkey = "Ctrl+Q";
                Settings.Default.Save();
            }

            screenshotHotkey = ParseHotkey(hotkeySetting);
        }
        public static IntPtr hookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code >= 0 && wParam == (IntPtr)WM_KEYDOWN )
            {

                int hotkey = Marshal.ReadInt32(lParam);
                // Если нажата клавиша Esc
                if (hotkey == (int)Keys.Escape)
                {
                    if (!_instance.isSelectingRectangle) // Esc активен только если выделение ещё не начато
                    {
                        _instance.isScreenStart = false;
                        _instance.ResetApplicationState();
                        return (IntPtr)1;
                    }
                    else
                    {
                        return (IntPtr)1;
                    }
                }
                // Проверяем модификаторы
                bool ctrlPressed = (Control.ModifierKeys & Keys.Control) == Keys.Control;
                bool shiftPressed = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;

                // Проверяем, совпадает ли комбинация с настройкой
                if (ctrlPressed == screenshotHotkey.Ctrl &&
                    shiftPressed == screenshotHotkey.Shift &&
                    (Keys)hotkey == screenshotHotkey.Key &&
                    !_instance.isScreenStart)
                {
                    _instance.Opacity = 0;
                    _instance.CloseFreezeScreen();
                    Bitmap screenshot = ScreenCapture();
                    _instance.isScreenStart = true;
                    _instance.FreezeScreen(screenshot);
                    _instance.Focus();
                    _instance.Opacity = 0.4;
                    if (_instance.isScreenStart) // Esc активен только если выделение ещё не начато
                    {
                        return (IntPtr)0;
                    }
                    else
                    {
                        return (IntPtr)1;
                    }
                }
            }
            return CallNextHookEx(hhook, code, (int)wParam, lParam);
        }


        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadHotkeyFromSettings();
        }
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            LoadHotkeyFromSettings();
        }


        private void RemembMe_Load(object sender, EventArgs e)
        {
            Settings.Default.OpenorNot = false;
            SetHook();
        }

        private void RemembMe_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnHook();
        }

        public static Bitmap ScreenCapture()
        {
            int screenLeft = int.MaxValue;
            int screenTop = int.MaxValue;
            int screenRight = int.MinValue;
            int screenBottom = int.MinValue;

            Bitmap bmp = null;

            int deviceIndex = 0;
            while (true)
            {
                NativeUtilities.DisplayDevice deviceData = new NativeUtilities.DisplayDevice { cb = Marshal.SizeOf(typeof(NativeUtilities.DisplayDevice)) };
                if (NativeUtilities.EnumDisplayDevices(null, deviceIndex, ref deviceData, 0) != 0)
                {
                    NativeUtilities.DEVMODE devMode = new NativeUtilities.DEVMODE();
                    if (NativeUtilities.EnumDisplaySettings(deviceData.DeviceName, NativeUtilities.ENUM_CURRENT_SETTINGS, ref devMode))
                    {
                        screenLeft = Math.Min(screenLeft, devMode.dmPositionX);
                        screenTop = Math.Min(screenTop, devMode.dmPositionY);
                        screenRight = Math.Max(screenRight, devMode.dmPositionX + devMode.dmPelsWidth);
                        screenBottom = Math.Max(screenBottom, devMode.dmPositionY + devMode.dmPelsHeight);
                    }
                    deviceIndex++;
                }
                else
                    break;
            }

            // Создание изображения с размером экрана
            bmp = new Bitmap(screenRight - screenLeft, screenBottom - screenTop);

            // Используем конструкцию using для автоматического освобождения ресурсов Graphics
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;

                // Заполнять bitmap здесь не нужно — следующая команда CopyFromScreen полностью обновляет его.
                g.CopyFromScreen(screenLeft, screenTop, 0, 0, bmp.Size);
            }

            return bmp;
        }
        private void RemembMe_MouseDown(object sender, MouseEventArgs e)
        {
            LocPress = e.Location;

            try
            {

                newForm.Show();
                newForm.Location = LocPress;
                newForm.Size = new Size(0, 0);
                newForm.Opacity = 0.2;
                newForm.TopMost = true;
                RectangleFlag = true;
                isSelectingRectangle = true; // Устанавливаем флаг процесса выделения
            }
            catch
            {

            }
        }

        private void RemembMe_MouseMove(object sender, MouseEventArgs e)
        {
            LocNow = e.Location;

            if (RectangleFlag)
            {
                newForm.Location = new Point((int)Math.Min(LocPress.X, LocNow.X), (int)Math.Min(LocPress.Y, LocNow.Y));
                newForm.Size = new Size((int)Math.Abs(LocNow.X - LocPress.X), (int)Math.Abs(LocNow.Y - LocPress.Y));
                Graphics gra = newForm.CreateGraphics();

                gra.Clear(newForm.BackColor);

                Pen skyPen = new Pen(ChangeColorBrightness(newForm.BackColor, 0.5f));
                skyPen.Width = 4.0F;
                skyPen.LineJoin = LineJoin.Bevel;
                gra.DrawRectangle(skyPen, new Rectangle(0, 0, (int)(LocNow.X - LocPress.X), (int)(LocNow.Y - LocPress.Y)));

                skyPen.Dispose();
            }
        }
        public static Color ChangeColorBrightness(Color color, float correctionFactor)
        {
            float red = (float)color.R;
            float green = (float)color.G;
            float blue = (float)color.B;
            if (correctionFactor < 0)
            {
                correctionFactor = 1 + correctionFactor;
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else
            {
                red = (255 - red) * correctionFactor + red;
                green = (255 - green) * correctionFactor + green;
                blue = (255 - blue) * correctionFactor + blue;
            }
            return Color.FromArgb(color.A, (int)red, (int)green, (int)blue);
        }

        private Form freezeForm;
        public void FreezeScreen(Bitmap screenshot)
        {
            // Если уже есть открытая форма заморозки, закрываем её
            if (freezeForm != null && !freezeForm.IsDisposed)
            {
                freezeForm.Close();
                freezeForm.Dispose();
            }

            // Создаём полноэкранное окно
            freezeForm = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                WindowState = FormWindowState.Maximized,
                TopMost = true,
                BackColor = Color.Black,
                Opacity = 1,
                ShowIcon = false,
                ShowInTaskbar = false
            };

            // Отображаем скриншот как фон формы
            freezeForm.BackgroundImage = screenshot;
            freezeForm.BackgroundImageLayout = ImageLayout.Stretch;

            freezeForm.Show();
        }
        public void CloseFreezeScreen()
        {
            if (freezeForm != null && !freezeForm.IsDisposed)
            {
                freezeForm.Close();
                freezeForm.Dispose();
            }
        }

        private void RemembMe_MouseUp(object sender, MouseEventArgs e) //самое главное
        {
            Bitmap screenshot = null;
            Bitmap cloneBitmap = null;
            if (Settings.Default.btnText == 0)
            {
                LocUp = e.Location;

                double r = 1;

                newForm.Opacity = 0;
                this.Opacity = 0;
                screenshot = ScreenCapture();
                
                Rectangle bounds = Screen.GetBounds(Point.Empty);
                r = (double)screenshot.Width / (double)bounds.Width;

                

                // Координаты выделенной области
                int x = Math.Min((int)(LocPress.X * r), (int)(LocUp.X * r));
                int y = Math.Min((int)(LocPress.Y * r), (int)(LocUp.Y * r));
                int width = Math.Abs((int)(LocUp.X * r - LocPress.X * r));
                int height = Math.Abs((int)(LocUp.Y * r - LocPress.Y * r));

                Rectangle cloneRect = new Rectangle(x, y, width, height);
                PixelFormat format = screenshot.PixelFormat;
                CloseFreezeScreen();
                try
                {
                    cloneBitmap = screenshot.Clone(cloneRect, format);
                    if (Settings.Default.SaveImage == 1)
                    {
                        try
                        {
                            // Получение пути из настроек
                            string savePath = Settings.Default.LinkToSave;

                            // Убедимся, что директория существует
                            if (!Directory.Exists(savePath))
                            {
                                Directory.CreateDirectory(savePath);
                            }

                            // Генерация уникального имени файла (например, с использованием времени)
                            string fileName = $"Скриншотик {DateTime.Now:yyyy.MM.dd HHmmss}.png";
                            string fullPath = Path.Combine(savePath, fileName);

                            // Сохранение изображения
                            cloneBitmap.Save(fullPath);
                        }
                        catch (Exception ex)
                        {
                            // Обработка ошибок сохранения
                            MessageBox.Show($"Не удалось сохранить изображение: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    Clipboard.SetImage(cloneBitmap);
                }
                catch
                {

                }
                finally
                {
                    // Освобождаем память
                    DisposeResources(screenshot, cloneBitmap);
                    RectangleFlag = false;
                    isSelectingRectangle = false; // Сбрасываем флаг процесса выделения
                    _instance.isScreenStart = false;
                    newForm.Hide();
                    this.Opacity = 0;
                    this.ActiveControl = null;
                }
            }
            if (Settings.Default.btnText == 1)
            {

                try
                {
                    LocUp = e.Location;
                    double r = 1;

                    newForm.Hide();
                    this.Opacity = 0;

                    // Получаем скриншот
                    screenshot = ScreenCapture();
                    
                    Rectangle bounds = Screen.GetBounds(Point.Empty);
                    r = (double)screenshot.Width / (double)bounds.Width;

                    // Координаты выделенной области
                    int x = Math.Min((int)(LocPress.X * r), (int)(LocUp.X * r));
                    int y = Math.Min((int)(LocPress.Y * r), (int)(LocUp.Y * r));
                    int width = Math.Abs((int)(LocUp.X * r - LocPress.X * r));
                    int height = Math.Abs((int)(LocUp.Y * r - LocPress.Y * r));
                    Rectangle cloneRect = new Rectangle(x, y, width, height);
                    
                    try
                    {
                        cloneBitmap = screenshot.Clone(cloneRect, screenshot.PixelFormat);
                        if (Settings.Default.SaveImage == 1)
                        {
                            try
                            {
                                string savePath = Settings.Default.LinkToSave;

                                // Убедимся, что директория существует
                                if (!Directory.Exists(savePath))
                                {
                                    Directory.CreateDirectory(savePath);
                                }

                                // Генерация уникального имени файла
                                string fileName = $"Screenshot_{DateTime.Now:yyyy.MM.dd HHmmss}.png";
                                string fullPath = Path.Combine(savePath, fileName);

                                cloneBitmap.Save(fullPath);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Не удалось сохранить изображение: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        string recognizedText = RecognizeText(cloneBitmap, GetSelectedLanguage());
                        
                        Clipboard.SetText(recognizedText); 
                        CloseFreezeScreen();
                    }
                    catch (Exception ex)
                    {
                        Clipboard.SetText($"Ошибка:{ex.Message}");
                        _instance.ResetApplicationState();
                    }
                    

                    
                }
                catch
                {

                }
                finally
                {
                    // Освобождаем память
                    DisposeResources(screenshot, cloneBitmap);
                    RectangleFlag = false;
                    isSelectingRectangle = false; // Сбрасываем флаг процесса выделения
                    _instance.isScreenStart = false;
                    newForm.Hide();
                    this.Opacity = 0;
                    this.ActiveControl = null;
                }
            }
            
        }

        private void настройкиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!Settings.Default.OpenorNot)
            {
                Form3 form3 = new Form3(this);
                form3.Show();
                Form2 f = new Form2();
            }
        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void оRemembMeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("RemembMe" + Environment.NewLine + "Version: 1.1" + Environment.NewLine + "https://github.com/Neforio-o");
        }
        private void ResetApplicationState()
        {
            CloseFreezeScreen();

            // Скрыть форму выделения
            if (newForm != null && !newForm.IsDisposed)
            {
                newForm.Hide();
            }

            // Сбросить флаги
            RectangleFlag = false;
            ctrlPressed = false;

            // Очистить выделение
            LocPress = Point.Empty;
            LocUp = Point.Empty;
            LocNow = Point.Empty;

            this.Opacity = 0;
        }

        public static string GetAvailableLanguages()
        {
            // Путь к папке tessdata (для удобства туда закинул)
            string tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");

            try
            {
                // Получаем все файлы с расширением .traineddata
                var languageFiles = Directory.GetFiles(tessDataPath, "*.traineddata");

                // Извлекаем языковые коды из имён файлов
                var languages = languageFiles.Select(file =>
                {
                    return Path.GetFileNameWithoutExtension(file); // Убираем расширение
                });

                // Объединяем языки через '+'
                return string.Join("+", languages);
            }
            catch
            {
                return "eng+rus"; // Возврат по умолчанию
            }
        }

        public static string GetSelectedLanguage()
        {
            // Возвращаем язык из настроек (Form3) приложения или устанавливаем язык по умолчанию
            return Settings.Default.SelectedLanguage ?? "eng+rus";
        }
        public static string RecognizeText(Bitmap image, string selectedLanguage)
        {

            //Путь к папке tessdata
            string tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");

            try
            {
                using (var engine = new TesseractEngine(tessDataPath, selectedLanguage, EngineMode.Default))
                {

                    using (var page = engine.Process(image))
                    {

                        if (page.GetText() == "")
                        {
                            return "Текст не распознан";
                        }
                        else
                        {
                            return page.GetText(); // Возвращает распознанный текст
                        }
                    }
                }
            }

            catch (Exception ex) // обрабатывал ошибки для устронения багов
            {
                return "";
            }
        }

        private void DisposeResources(Bitmap screenshot = null, Bitmap cloneBitmap = null, Graphics graphics = null)
        {
            screenshot?.Dispose();
            cloneBitmap?.Dispose();
            graphics?.Dispose();
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            if (!Settings.Default.OpenorNot && e.Button == MouseButtons.Left)
            {
                Form3 form3 = new Form3(this);
                form3.Show();
                Form2 f = new Form2();
            }
        }

        private void CreateScreenshoot_Click(object sender, EventArgs e)
        {
            _instance.Opacity = 0;
            _instance.CloseFreezeScreen();
            Bitmap screenshot = ScreenCapture();
            _instance.isScreenStart = true;
            _instance.FreezeScreen(screenshot);
            _instance.Focus();
            _instance.Opacity = 0.4;
        }
    }

    class NativeUtilities
    {
        [Flags()]
        public enum DisplayDeviceStateFlags : int
        {
            // The device is part of the desktop.
            AttachedToDesktop = 0x1,
            MultiDriver = 0x2,
            // This is the primary display.
            PrimaryDevice = 0x4,
            // Represents a pseudo device used to mirror application drawing for remoting or other purposes.
            MirroringDriver = 0x8,
            // The device is VGA compatible.
            VGACompatible = 0x16,
            // The device is removable; it cannot be the primary display
            Removable = 0x20,
            // The device has more display modes than its output devices support.
            ModesPruned = 0x8000000,
            Remote = 0x4000000,
            Disconnect = 0x2000000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct DisplayDevice
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            [MarshalAs(UnmanagedType.U4)]
            public DisplayDeviceStateFlags StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DEVMODE
        {
            private const int CCHDEVICENAME = 0x20;
            private const int CCHFORMNAME = 0x20;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public ScreenOrientation dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [DllImport("user32.dll")]
        public static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        public const int ENUM_CURRENT_SETTINGS = -1;
        const int ENUM_REGISTRY_SETTINGS = -2;

        [DllImport("User32.dll")]
        public static extern int EnumDisplayDevices(string lpDevice, int iDevNum, ref DisplayDevice lpDisplayDevice, int dwFlags);
    }

}



