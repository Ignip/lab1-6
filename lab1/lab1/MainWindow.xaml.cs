using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;


namespace lab1
{
    // Класс для хранения информации об иконке
    public class EnemyIcon
    {
        public string Name { get; set; }
        public string ImagePath { get; set; }
    }

    // Класс шаблона противника
    public class CEnemyTemplate
    {
        [JsonInclude]
        private string name;

        [JsonInclude]
        private string iconName;

        [JsonInclude]
        private int baseLife;

        [JsonInclude]
        private double lifeModifier;

        [JsonInclude]
        private int baseGold;

        [JsonInclude]
        private double goldModifier;

        [JsonInclude]
        private double spawnChance;

        public CEnemyTemplate(string name, string iconName, int baseLife,
                             double lifeModifier, int baseGold, double goldModifier,
                             double spawnChance)
        {
            this.name = name;
            this.iconName = iconName;
            this.baseLife = baseLife;
            this.lifeModifier = lifeModifier;
            this.baseGold = baseGold;
            this.goldModifier = goldModifier;
            this.spawnChance = spawnChance;
        }

        public string GetName() { return name; }
        public string GetIconName() { return iconName; }
        public int GetBaseLife() { return baseLife; }
        public double GetLifeModifier() { return lifeModifier; }
        public int GetBaseGold() { return baseGold; }
        public double GetGoldModifier() { return goldModifier; }
        public double GetSpawnChance() { return spawnChance; }
    }

    // Класс для управления списком противников
    public class CEnemyTemplateList
    {
        private List<CEnemyTemplate> enemies;

        public CEnemyTemplateList()
        {
            enemies = new List<CEnemyTemplate>();
        }

        public void AddEnemy(string name, string iconName, int baseLife,
                            double lifeModifier, int baseGold, double goldModifier,
                            double spawnChance)
        {
            enemies.Add(new CEnemyTemplate(name, iconName, baseLife,
                                          lifeModifier, baseGold, goldModifier,
                                          spawnChance));
        }

        public CEnemyTemplate GetEnemyByName(string name)
        {
            return enemies.Find(e => e.GetName() == name);
        }

        public CEnemyTemplate GetEnemyByIndex(int index)
        {
            if (index >= 0 && index < enemies.Count)
                return enemies[index];
            return null;
        }

        public void DeleteEnemyByName(string name)
        {
            var enemy = GetEnemyByName(name);
            if (enemy != null)
                enemies.Remove(enemy);
        }

        public void DeleteEnemyByIndex(int index)
        {
            if (index >= 0 && index < enemies.Count)
                enemies.RemoveAt(index);
        }

        public List<string> GetListOfEnemyNames()
        {
            List<string> names = new List<string>();
            foreach (var enemy in enemies)
            {
                names.Add(enemy.GetName());
            }
            return names;
        }

        public List<CEnemyTemplate> GetAllEnemies()
        {
            return enemies;
        }

        public void SaveToJson(string path)
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(enemies, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(path, jsonString);
                MessageBox.Show("Список противников успешно сохранен!", "Успех",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LoadFromJson(string path)
        {
            try
            {
                string jsonFromFile = File.ReadAllText(path);
                JsonDocument doc = JsonDocument.Parse(jsonFromFile);

                enemies.Clear();

                foreach (JsonElement element in doc.RootElement.EnumerateArray())
                {
                    string name = element.GetProperty("name").GetString();
                    string iconName = element.GetProperty("iconName").GetString();
                    int baseLife = element.GetProperty("baseLife").GetInt32();
                    double lifeModifier = element.GetProperty("lifeModifier").GetDouble();
                    int baseGold = element.GetProperty("baseGold").GetInt32();
                    double goldModifier = element.GetProperty("goldModifier").GetDouble();
                    double spawnChance = element.GetProperty("spawnChance").GetDouble();

                    enemies.Add(new CEnemyTemplate(name, iconName, baseLife,
                                                   lifeModifier, baseGold,
                                                   goldModifier, spawnChance));
                }

                MessageBox.Show("Список противников успешно загружен!", "Успех",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Основное окно приложения
    public partial class MainWindow : Window
    {
        private CEnemyTemplateList enemyList;
        private List<EnemyIcon> enemyIcons;
        private string currentSelectedIcon = "";
        private string lastFolderPath = "";

        public MainWindow()
        {
            InitializeComponent();
            enemyList = new CEnemyTemplateList();
            enemyIcons = new List<EnemyIcon>();
        }

        // Выбор папки с иконками
        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            // Создаем простой диалог для ввода пути
            var inputDialog = new Window
            {
                Title = "Выберите папку с иконками",
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var stackPanel = new StackPanel { Margin = new Thickness(10) };

            var label = new TextBlock
            {
                Text = "Введите путь к папке с иконками:",
                Margin = new Thickness(0, 0, 0, 10)
            };

            var textBox = new TextBox { Margin = new Thickness(0, 0, 0, 10) };

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center };

            var okButton = new Button { Content = "OK", Width = 60, Margin = new Thickness(5) };
            var cancelButton = new Button { Content = "Отмена", Width = 60, Margin = new Thickness(5) };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(label);
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(buttonPanel);

            inputDialog.Content = stackPanel;

            okButton.Click += (s, args) =>
            {
                if (Directory.Exists(textBox.Text))
                {
                    lastFolderPath = textBox.Text;
                    LoadIconsFromFolder(textBox.Text);
                    inputDialog.Close();
                }
                else
                {
                    MessageBox.Show("Указанная папка не существует!", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            cancelButton.Click += (s, args) => inputDialog.Close();

            inputDialog.ShowDialog();
        }

        // Загрузка иконок из папки
        private void LoadIconsFromFolder(string path)
        {
            try
            {
                enemyIcons.Clear();
                IconsListBox.Items.Clear();

                string filter = "*.png";
                string[] files = Directory.GetFiles(path, filter);

                foreach (string file in files)
                {
                    enemyIcons.Add(new EnemyIcon
                    {
                        Name = System.IO.Path.GetFileName(file),
                        ImagePath = file
                    });
                }

                // Заполнение ListBox иконками
                foreach (EnemyIcon icon in enemyIcons)
                {
                    Image image = new Image()
                    {
                        Source = new BitmapImage(new Uri(icon.ImagePath)),
                        Height = 64,
                        Width = 64,
                        Margin = new Thickness(5),
                        Stretch = Stretch.Uniform
                    };

                    StackPanel panel = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        Margin = new Thickness(5)
                    };

                    TextBlock textBlock = new TextBlock
                    {
                        Text = icon.Name,
                        FontSize = 10,
                        TextAlignment = TextAlignment.Center
                    };

                    panel.Children.Add(image);
                    panel.Children.Add(textBlock);

                    IconsListBox.Items.Add(panel);
                }

                if (enemyIcons.Count == 0)
                {
                    MessageBox.Show("В выбранной папке не найдено PNG изображений!",
                                   "Предупреждение", MessageBoxButton.OK,
                                   MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке иконок: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработка выбора иконки
        private void IconsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IconsListBox.SelectedItem != null)
            {
                int index = IconsListBox.SelectedIndex;
                if (index >= 0 && index < enemyIcons.Count)
                {
                    currentSelectedIcon = enemyIcons[index].Name;
                    SelectedIconTextBox.Text = currentSelectedIcon;
                }
            }
        }

        // Добавление противника
        private void AddEnemy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверка ввода
                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                {
                    MessageBox.Show("Введите название противника!", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(currentSelectedIcon))
                {
                    MessageBox.Show("Выберите иконку противника!", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(BaseLifeTextBox.Text) ||
                    string.IsNullOrWhiteSpace(LifeModifierTextBox.Text) ||
                    string.IsNullOrWhiteSpace(BaseGoldTextBox.Text) ||
                    string.IsNullOrWhiteSpace(GoldModifierTextBox.Text) ||
                    string.IsNullOrWhiteSpace(SpawnChanceTextBox.Text))
                {
                    MessageBox.Show("Заполните все поля!", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string name = NameTextBox.Text;
                int baseLife = int.Parse(BaseLifeTextBox.Text);
                double lifeModifier = double.Parse(LifeModifierTextBox.Text);
                int baseGold = int.Parse(BaseGoldTextBox.Text);
                double goldModifier = double.Parse(GoldModifierTextBox.Text);
                double spawnChance = double.Parse(SpawnChanceTextBox.Text);

                // Проверка шанса появления
                if (spawnChance < 0 || spawnChance > 1)
                {
                    MessageBox.Show("Шанс появления должен быть в диапазоне от 0 до 1!",
                                   "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Проверка на дубликат имени
                if (enemyList.GetEnemyByName(name) != null)
                {
                    MessageBox.Show("Противник с таким именем уже существует!", "Ошибка",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                enemyList.AddEnemy(name, currentSelectedIcon, baseLife, lifeModifier,
                                  baseGold, goldModifier, spawnChance);

                UpdateEnemiesList();
                ClearForm();

                MessageBox.Show("Противник успешно добавлен!", "Успех",
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (FormatException)
            {
                MessageBox.Show("Проверьте правильность ввода числовых значений!",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Удаление выбранного противника
        private void DeleteEnemy_Click(object sender, RoutedEventArgs e)
        {
            if (EnemiesListBox.SelectedItem != null)
            {
                string selectedName = EnemiesListBox.SelectedItem.ToString();

                MessageBoxResult result = MessageBox.Show($"Удалить противника '{selectedName}'?",
                                                          "Подтверждение",
                                                          MessageBoxButton.YesNo,
                                                          MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    enemyList.DeleteEnemyByName(selectedName);
                    UpdateEnemiesList();
                    ClearForm();
                }
            }
            else
            {
                MessageBox.Show("Выберите противника для удаления!", "Ошибка",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Обновление списка противников в интерфейсе
        private void UpdateEnemiesList()
        {
            EnemiesListBox.Items.Clear();
            List<string> enemyNames = enemyList.GetListOfEnemyNames();
            foreach (string name in enemyNames)
            {
                EnemiesListBox.Items.Add(name);
            }
        }

        // Очистка формы
        private void ClearForm()
        {
            NameTextBox.Clear();
            SelectedIconTextBox.Clear();
            BaseLifeTextBox.Clear();
            LifeModifierTextBox.Clear();
            BaseGoldTextBox.Clear();
            GoldModifierTextBox.Clear();
            SpawnChanceTextBox.Clear();
            currentSelectedIcon = "";
            IconsListBox.SelectedItem = null;
        }

        // Выбор противника из списка для редактирования
        private void EnemiesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EnemiesListBox.SelectedItem != null)
            {
                string selectedName = EnemiesListBox.SelectedItem.ToString();
                CEnemyTemplate enemy = enemyList.GetEnemyByName(selectedName);

                if (enemy != null)
                {
                    NameTextBox.Text = enemy.GetName();
                    currentSelectedIcon = enemy.GetIconName();
                    SelectedIconTextBox.Text = currentSelectedIcon;
                    BaseLifeTextBox.Text = enemy.GetBaseLife().ToString();
                    LifeModifierTextBox.Text = enemy.GetLifeModifier().ToString();
                    BaseGoldTextBox.Text = enemy.GetBaseGold().ToString();
                    GoldModifierTextBox.Text = enemy.GetGoldModifier().ToString();
                    SpawnChanceTextBox.Text = enemy.GetSpawnChance().ToString();

                    // Подсветка выбранной иконки
                    for (int i = 0; i < enemyIcons.Count; i++)
                    {
                        if (enemyIcons[i].Name == currentSelectedIcon)
                        {
                            IconsListBox.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
        }

        // Сохранение в JSON
        private void SaveToJson_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "JSON files (*.json)|*.json";
            saveDialog.DefaultExt = "json";
            saveDialog.FileName = "enemies.json";

            if (saveDialog.ShowDialog() == true)
            {
                enemyList.SaveToJson(saveDialog.FileName);
            }
        }

        // Загрузка из JSON
        private void LoadFromJson_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "JSON files (*.json)|*.json";
            openDialog.DefaultExt = "json";

            if (openDialog.ShowDialog() == true)
            {
                enemyList.LoadFromJson(openDialog.FileName);
                UpdateEnemiesList();
                ClearForm();
            }
        }

        // Очистка всех данных
        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Вы уверены, что хотите удалить всех противников?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                enemyList = new CEnemyTemplateList();
                UpdateEnemiesList();
                ClearForm();
            }
        }
    }
}