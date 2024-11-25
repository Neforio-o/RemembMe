using Microsoft.Win32;
using RemembMe.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;


namespace RemembMe
{
    public partial class Form3 : Form
    {
        RemembMe remembMe;
        private string selectedLanguage = "eng+rus";
        public Form3(RemembMe r)
        {
            this.Icon = Resources.Untitled4;
            InitializeComponent();
            remembMe = r;
            
        }

        private void Close_btn_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Min_btn_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void label5_MouseDown(object sender, MouseEventArgs e)
        {
            label5.Capture = false;
            Message m = Message.Create(base.Handle, 0xa1, new IntPtr(2), IntPtr.Zero);
            WndProc(ref m);
        }

        private void label7_MouseDown(object sender, MouseEventArgs e)
        {
            label7.Capture = false;
            Message m = Message.Create(base.Handle, 0xa1, new IntPtr(2), IntPtr.Zero);
            WndProc(ref m);
        }

        private void Min_btn_MouseEnter(object sender, EventArgs e)
        {
            Min_btn.BackColor = Color.FromArgb(118, 118, 118);
        }

        private void Min_btn_MouseLeave(object sender, EventArgs e)
        {
            Min_btn.BackColor = Color.FromArgb(20, 20, 20);
        }

        private void Close_btn_MouseEnter(object sender, EventArgs e)
        {
            Close_btn.BackColor = Color.FromArgb(239, 74, 83);
        }

        private void Close_btn_MouseLeave(object sender, EventArgs e)
        {
            Close_btn.BackColor = Color.FromArgb(20, 20, 20);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            GraphicsPath path = new GraphicsPath();
            Rectangle bounds = new Rectangle(0, 0, Width, Height);
            int arcRadius = 15; // для изменения закгруленности
            path.AddArc(bounds.X, bounds.Y, arcRadius, arcRadius, 180, 90);
            path.AddArc(bounds.X + bounds.Width - arcRadius, bounds.Y, arcRadius, arcRadius, 270, 90);
            path.AddArc(bounds.X + bounds.Width - arcRadius, bounds.Y + bounds.Height - arcRadius, arcRadius, arcRadius, 0, 90);
            path.AddArc(bounds.X, bounds.Y + bounds.Height - arcRadius, arcRadius, arcRadius, 90, 90);
            path.CloseFigure();
            Region = new Region(path);
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            Settings.Default.OpenorNot = true;
            Close_btn.Select();
            int default_Index = (int)Settings.Default.SelectedLanguageIndex;
            if (default_Index != -1)
            {
                comboBox2.SelectedIndex = default_Index;
            }
            else
            {
                comboBox2.SelectedIndex = 0;
            }
            if (Settings.Default.btnText == 1)
            {
                toggleButton1.Checked = true;
            }
            else
            {
                toggleButton1.Checked = false;
            }

            if (Settings.Default.AutoStart == 1)
            {
                toggleButton2.Checked = true;
            }
            else
            {
                toggleButton2.Checked = false;
            }

            if (Settings.Default.SaveImage == 1)
            {
                toggleButton3.Checked = true;
            }
            else
            {
                toggleButton3.Checked = false;
            }

            LinkToSave = Settings.Default.LinkToSave;

            if (!string.IsNullOrEmpty(LinkToSave))
            {
                label10.Text = LinkToSave;
            }
            else
            {
                LoadSavedPath();
            }

            HotkeyTextBox.Text = Settings.Default.Hotkey;
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            System.Windows.Forms.ComboBox comboBox = sender as System.Windows.Forms.ComboBox;
            if (comboBox != null)
            {
                string selectedOption = comboBox.SelectedItem.ToString();

                switch (selectedOption)
                {
                    case "Английский (eng)":
                        selectedLanguage = "eng";
                        break;
                    case "Русский (rus)":
                        selectedLanguage = "rus";
                        break;
                    case "Китайский (chi_sim)":
                        selectedLanguage = "chi_sim";
                        break;
                    case "Японский (jpn)":
                        selectedLanguage = "jpn";
                        break;
                    case "Корейский (kor)":
                        selectedLanguage = "kor";
                        break;
                    case "Общий (all)":
                        selectedLanguage = "eng+rus+chi_sim+jpn+kor+deu+fra+ita";
                        break;
                    case "Стандартный (eng + rus)":
                        selectedLanguage = "eng+rus";
                        break;
                    case "Немецкий (deu)":
                        selectedLanguage = "deu";
                        break;
                    case "Французский (fra)":
                        selectedLanguage = "fra";
                        break;
                    case "Итальянский (ita)":
                        selectedLanguage = "ita";
                        break;
                    default:
                        selectedLanguage = "eng+rus";
                        break;
                }
                // Сохраняем выбор для дальнейшей обработки
                Settings.Default.SelectedLanguage = selectedLanguage;
                Settings.Default.SelectedLanguageIndex= comboBox2.SelectedIndex;
                Settings.Default.Save();
            }
            if (comboBox2.SelectedIndex == 9)
            {
                label4.Visible = true;
            }
            else
            {
                label4.Visible = false;
            }
        }

        private void toggleButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (toggleButton1.Checked == false)
            {
                Settings.Default.btnText = 0;
            }
            else
            {
                Settings.Default.btnText = 1;

            }
            Settings.Default.Save();
        }

        private void HotkeyTextBox_KeyDown(object sender, KeyEventArgs e)
        {

            // Если нажаты только модификаторы (Ctrl, Shift), игнорируем их
            if (e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.ShiftKey)
            {
                e.SuppressKeyPress = true; // Блокируем ввод
                return;
            }
            if (e.KeyCode == Keys.Back)
            {
                HotkeyTextBox.Text = string.Empty;
                return;
            }
            var modifiers = new List<string>();
            if ((e.Modifiers & Keys.Control) != 0 && (modifiers.Count == 0))
                modifiers.Add("Ctrl");
            if ((e.Modifiers & Keys.Shift) != 0 && (modifiers.Count == 0))
                modifiers.Add("Shift");

            // Основная клавиша
            string key = e.KeyCode.ToString();

            // Формируем строку комбинации
            string hotkey = string.Join("+", modifiers) + (modifiers.Count > 0 ? "+" : "") + key;

            HotkeyTextBox.Text = hotkey;

            // Блокируем ввод в поле
            e.SuppressKeyPress = true;
        }

        private void HotkeyTextBox_TextChanged(object sender, EventArgs e)
        {
            Settings.Default.Hotkey = HotkeyTextBox.Text;
            Settings.Default.Save();
            remembMe.ChangeHotKey(HotkeyTextBox.Text);
            if (HotkeyTextBox.Text == "")
            {
                label6.Visible = true;
            }
            else
            {
                label6.Visible = false;
            }
            
        }

        private void Form3_FormClosed(object sender, FormClosedEventArgs e)
        {
            Settings.Default.OpenorNot = false;
            if (HotkeyTextBox.Text == "")
            {
                MessageBox.Show("Установлены стандартные горячие клавиши: Ctrl+Q");
            }
        }


        bool chkStartUp = false;

        private void SetStartup()
        {
            // Открытие ключа реестра
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (rk == null)
            {
                MessageBox.Show("Не удалось получить доступ к разделу реестра.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                if (chkStartUp)
                {
                    rk.SetValue(Application.ProductName, Application.ExecutablePath);
                }
                else
                {
                    if (rk.GetValue(Application.ProductName) != null)
                    {
                        rk.DeleteValue(Application.ProductName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при работе с реестром: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Закрытие ключа реестра
                rk.Close();
            }
        }

        private void toggleButton2_CheckedChanged(object sender, EventArgs e)
        {
            chkStartUp = toggleButton2.Checked;
            SetStartup();
            if (toggleButton2.Checked == false)
            {
                Settings.Default.AutoStart = 0;
            }
            else
            {
                Settings.Default.AutoStart = 1;
            }
            Settings.Default.Save();
        }

        private void toggleButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (toggleButton3.Checked == false)
            {
                Settings.Default.SaveImage = 0;
                label10.Visible = false;
                linkLabel1.Visible = false;
            }
            else
            {
                Settings.Default.SaveImage = 1;
                label10.Visible = true;
                linkLabel1.Visible = true;
            }
            Settings.Default.Save();
        }

        private string LinkToSave;
        private void LoadSavedPath()
        {
            LinkToSave = Settings.Default.LinkToSave;

            if (string.IsNullOrEmpty(LinkToSave))
            {
                // Если пути нет, создаём стандартный
                string defaultPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Screenshots"
                );

                if (!Directory.Exists(defaultPath))
                {
                    Directory.CreateDirectory(defaultPath);
                }

                LinkToSave = defaultPath;

                Settings.Default.LinkToSave = LinkToSave;
                Settings.Default.Save();
            }

            label10.Text = LinkToSave;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Выберите папку для сохранения скриншотов";
                folderDialog.ShowNewFolderButton = true;
                folderDialog.SelectedPath = LinkToSave;

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    LinkToSave = folderDialog.SelectedPath;
                    label10.Text = LinkToSave;

                    Settings.Default.LinkToSave = LinkToSave;
                    Settings.Default.Save();
                }
            }
        }
    }
}
