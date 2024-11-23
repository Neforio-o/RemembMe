using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RemembMe
{
    internal static class Program
    {
        private const string AppMutexName = "CoolMutexforRememnMe";
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool isNewInstance;

            using (Mutex mutex = new Mutex(true, AppMutexName, out isNewInstance))
            {
                if (!isNewInstance) //запущенно ли приложение
                {
                    MessageBox.Show("Приложение уже запущено.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new RemembMe()); // Запускаем основное окно
            }
        }
    }
}
