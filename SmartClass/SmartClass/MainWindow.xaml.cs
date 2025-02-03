using Microsoft.Win32;
using Newtonsoft.Json;
using SmartClass.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SmartClass
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        HttpClient httpClient = new HttpClient();
        public MainWindow()
        {
            InitializeComponent();

            var code = Settings.Default.Code;
            var isActive = Settings.Default.IsActive;

            if (!isActive)
                AutoBtn.Content = "Убрать из автозагрузки";

            if (code != "null")
            {
                Code.IsEnabled = false;
                Code.Text = code;
                SaveBtn.Content = "Изменить";
            }

            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Interval = TimeSpan.FromSeconds(3);
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Start();
        }

        class Computer
        {
            public int Id { get; set; }
            public string Uuid { get; set; }
            public string Name { get; set; }
            public int Work { get; set; }
            public bool Is_active { get; set; }
            public bool Signal { get; set; }
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            Refresh();
        }

        static List<Computer> ParseJsonToComputers(string json)
        {
            var computers = new List<Computer>();

            // Убираем квадратные скобки массива
            json = json.Trim(new char[] { '[', ']' });
            string[] objects = json.Split(new[] { "},{" }, StringSplitOptions.None);

            foreach (var obj in objects)
            {
                string cleanedObj = obj.Trim(new char[] { '{', '}' });
                var computer = new Computer();

                // Разбиваем объект на пары ключ-значение
                string[] pairs = cleanedObj.Split(',');
                foreach (var pair in pairs)
                {
                    string[] keyValue = pair.Split(':');
                    string key = keyValue[0].Trim(new char[] { '"', ' ' });
                    string value = keyValue[1].Trim(new char[] { '"', ' ' });

                    // Заполняем свойства объекта Computer
                    switch (key)
                    {
                        case "id":
                            computer.Id = int.Parse(value);
                            break;
                        case "uuid":
                            computer.Uuid = value;
                            break;
                        case "name":
                            computer.Name = value;
                            break;
                        case "work":
                            computer.Work = int.Parse(value);
                            break;
                        case "is_active":
                            computer.Is_active = bool.Parse(value);
                            break;
                        case "signal":
                            computer.Signal = bool.Parse(value);
                            break;
                    }
                }

                computers.Add(computer);
            }

            return computers;
        }

        private async void Refresh()
        {
            try
            {
                var response = await httpClient.GetAsync("https://smartclass.pythonanywhere.com/api/computers/");
                var content = await response.Content.ReadAsStringAsync();
                List<Computer> computers = ParseJsonToComputers(content);

                var code = Settings.Default.Code;
                var computer = computers.FirstOrDefault(x => x.Uuid == code);

                if (computer == null)
                    return;

                if (computer.Signal)
                    SystemSounds.Beep.Play();

                if (!computer.Is_active)
                    Process.Start("shutdown", "/s /t 0");
            }
            catch
            {
                return;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (Code.IsEnabled == false)
            {
                Code.IsEnabled = true;
                SaveBtn.Content = "Активировать";
                return;
            }

            Settings.Default.Code = Code.Text;
            Settings.Default.Save();

            Code.IsEnabled = false;
            SaveBtn.Content = "Изменить";
        }

        private void HiddenBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void AutoBtn_Click(object sender, RoutedEventArgs e)
        {
            var isActive = Settings.Default.IsActive;
            var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (isActive)
            {
                registryKey.SetValue("SmartClass", exePath);
                AutoBtn.Content = "Убрать из автозагрузки";
                Settings.Default.IsActive = false;
            }
            else
            {
                registryKey.DeleteValue("SmartClass", false);
                AutoBtn.Content = "Добавить в автозагрузки";
                Settings.Default.IsActive = true;
            }
            Settings.Default.Save();
        }
    }
}
