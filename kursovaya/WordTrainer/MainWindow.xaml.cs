using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WordTrainer
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, List<Word>> wordsByCategory;
        private ObservableCollection<string> categories;
        private Word currentWord;
        private string currentCategory;
        private List<string> currentOptions;
        private int correctAnswers;
        private int wrongAnswers;
        private string dataFilePath = "words_data.json";

        public MainWindow()
        {
            InitializeComponent();
            InitializeData();
            LoadDataFromFile();
            UpdateStatisticsDisplay();
        }

        private void InitializeData()
        {
            wordsByCategory = new Dictionary<string, List<Word>>();
            categories = new ObservableCollection<string>();

            // Добавляем примеры слов для демонстрации
            AddSampleWords();

            UpdateCategoriesList();
        }

        private void AddSampleWords()
        {
            // Животные
            AddWordToCategory("Животные", new Word("dog", "собака"));
            AddWordToCategory("Животные", new Word("cat", "кошка"));
            AddWordToCategory("Животные", new Word("bird", "птица"));
            AddWordToCategory("Животные", new Word("fish", "рыба"));

            // Фрукты
            AddWordToCategory("Фрукты", new Word("apple", "яблоко"));
            AddWordToCategory("Фрукты", new Word("banana", "банан"));
            AddWordToCategory("Фрукты", new Word("orange", "апельсин"));
            AddWordToCategory("Фрукты", new Word("grape", "виноград"));

            // Глаголы
            AddWordToCategory("Глаголы", new Word("run", "бежать"));
            AddWordToCategory("Глаголы", new Word("eat", "есть"));
            AddWordToCategory("Глаголы", new Word("sleep", "спать"));
            AddWordToCategory("Глаголы", new Word("read", "читать"));
        }

        private void AddWordToCategory(string category, Word word)
        {
            if (!wordsByCategory.ContainsKey(category))
            {
                wordsByCategory[category] = new List<Word>();
            }
            wordsByCategory[category].Add(word);
        }

        private void UpdateCategoriesList()
        {
            categories.Clear();
            foreach (var category in wordsByCategory.Keys.OrderBy(c => c))
            {
                categories.Add(category);
            }

            cmbCategories.ItemsSource = categories;
            if (categories.Count > 0)
            {
                cmbCategories.SelectedIndex = 0;
            }
        }

        private void CmbCategories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbCategories.SelectedItem != null)
            {
                currentCategory = cmbCategories.SelectedItem.ToString();
                LoadRandomWord();
                UpdateStatus($"Выбрана категория: {currentCategory}");
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadRandomWord();
            UpdateStatus("Слова обновлены");
        }

        private void LoadRandomWord()
        {
            if (string.IsNullOrEmpty(currentCategory) || !wordsByCategory.ContainsKey(currentCategory))
            {
                txtCurrentWord.Text = "Нет слов в этой категории";
                itemsOptions.ItemsSource = null;
                btnCheck.IsEnabled = false;
                btnNext.IsEnabled = false;
                return;
            }

            var words = wordsByCategory[currentCategory];
            if (words.Count == 0)
            {
                txtCurrentWord.Text = "Нет слов в этой категории";
                itemsOptions.ItemsSource = null;
                btnCheck.IsEnabled = false;
                btnNext.IsEnabled = false;
                return;
            }

            Random rand = new Random();
            currentWord = words[rand.Next(words.Count)];
            txtCurrentWord.Text = currentWord.ForeignWord;

            GenerateOptions();

            btnCheck.IsEnabled = true;
            txtResult.Text = "";
            ClearRadioButtons();
        }

        private void GenerateOptions()
        {
            var options = new List<string>();
            options.Add(currentWord.Translation);

            // Добавляем случайные переводы из других слов
            var allWords = wordsByCategory.Values.SelectMany(w => w).ToList();
            var otherTranslations = allWords
                .Where(w => w.Translation != currentWord.Translation)
                .Select(w => w.Translation)
                .Distinct()
                .Take(3)
                .ToList();

            options.AddRange(otherTranslations);

            // Если недостаточно вариантов, добавляем заглушки
            while (options.Count < 4)
            {
                options.Add($"вариант {options.Count + 1}");
            }

            // Перемешиваем варианты
            Random rand = new Random();
            currentOptions = options.OrderBy(x => rand.Next()).ToList();
            itemsOptions.ItemsSource = currentOptions;
        }

        private void ClearRadioButtons()
        {
            if (itemsOptions.ItemsSource != null)
            {
                foreach (var item in itemsOptions.Items)
                {
                    var container = itemsOptions.ItemContainerGenerator.ContainerFromItem(item);
                    if (container != null)
                    {
                        var radioButton = FindVisualChild<RadioButton>(container);
                        if (radioButton != null)
                        {
                            radioButton.IsChecked = false;
                        }
                    }
                }
            }
        }

        private T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T)
                    return (T)child;
                else
                {
                    T childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton;
            if (radioButton != null && radioButton.IsChecked == true)
            {
                btnCheck.IsEnabled = true;
            }
        }

        private void BtnCheck_Click(object sender, RoutedEventArgs e)
        {
            string selectedAnswer = null;

            foreach (var item in itemsOptions.Items)
            {
                var container = itemsOptions.ItemContainerGenerator.ContainerFromItem(item);
                if (container != null)
                {
                    var radioButton = FindVisualChild<RadioButton>(container);
                    if (radioButton != null && radioButton.IsChecked == true)
                    {
                        selectedAnswer = radioButton.Content.ToString();
                        break;
                    }
                }
            }

            if (string.IsNullOrEmpty(selectedAnswer))
            {
                MessageBox.Show("Пожалуйста, выберите вариант ответа", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (selectedAnswer == currentWord.Translation)
            {
                correctAnswers++;
                txtResult.Text = "✓ Правильно! Отличная работа!";
                txtResult.Foreground = System.Windows.Media.Brushes.Green;
                UpdateStatus("Правильный ответ!");
            }
            else
            {
                wrongAnswers++;
                txtResult.Text = $"✗ Неправильно. Правильный ответ: {currentWord.Translation}";
                txtResult.Foreground = System.Windows.Media.Brushes.Red;
                UpdateStatus("Неправильный ответ");
            }

            UpdateStatisticsDisplay();
            btnCheck.IsEnabled = false;
            btnNext.IsEnabled = true;
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            LoadRandomWord();
            btnNext.IsEnabled = false;
            btnCheck.IsEnabled = true;
        }

        private void UpdateStatisticsDisplay()
        {
            txtCorrectCount.Text = correctAnswers.ToString();
            txtWrongCount.Text = wrongAnswers.ToString();
            txtTotalCount.Text = (correctAnswers + wrongAnswers).ToString();
        }

        private void BtnAddWord_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddWordDialog(wordsByCategory.Keys.ToList());
            if (dialog.ShowDialog() == true)
            {
                string category = dialog.SelectedCategory;
                string foreignWord = dialog.ForeignWord;
                string translation = dialog.Translation;

                if (!string.IsNullOrEmpty(category) && !string.IsNullOrEmpty(foreignWord) && !string.IsNullOrEmpty(translation))
                {
                    AddWordToCategory(category, new Word(foreignWord, translation));
                    UpdateCategoriesList();

                    // Если текущая категория - та, в которую добавили, обновляем
                    if (currentCategory == category)
                    {
                        LoadRandomWord();
                    }

                    UpdateStatus($"Слово '{foreignWord}' добавлено в категорию '{category}'");
                    SaveDataToFile(); // Автоматически сохраняем после добавления
                }
            }
        }

        private void BtnSaveData_Click(object sender, RoutedEventArgs e)
        {
            SaveDataToFile();
        }

        private void BtnLoadData_Click(object sender, RoutedEventArgs e)
        {
            LoadDataFromFile();
        }

        private void SaveDataToFile()
        {
            try
            {
                var data = new SaveData
                {
                    WordsByCategory = wordsByCategory,
                    CorrectAnswers = correctAnswers,
                    WrongAnswers = wrongAnswers
                };

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                string jsonString = JsonSerializer.Serialize(data, options);
                File.WriteAllText(dataFilePath, jsonString);
                UpdateStatus($"Данные сохранены в файл: {dataFilePath}");
                MessageBox.Show("Данные успешно сохранены!", "Сохранение",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDataFromFile()
        {
            try
            {
                if (File.Exists(dataFilePath))
                {
                    string jsonString = File.ReadAllText(dataFilePath);
                    var data = JsonSerializer.Deserialize<SaveData>(jsonString);

                    if (data != null)
                    {
                        wordsByCategory = data.WordsByCategory;
                        correctAnswers = data.CorrectAnswers;
                        wrongAnswers = data.WrongAnswers;

                        UpdateCategoriesList();
                        UpdateStatisticsDisplay();
                        UpdateStatus($"Данные загружены из файла: {dataFilePath}");

                        MessageBox.Show("Данные успешно загружены!", "Загрузка",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Файл с данными не найден. Будут использованы примеры слов.",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateStatus(string message)
        {
            txtStatus.Text = message;
        }
    }

    public class Word
    {
        public string ForeignWord { get; set; }
        public string Translation { get; set; }

        public Word() { }

        public Word(string foreignWord, string translation)
        {
            ForeignWord = foreignWord;
            Translation = translation;
        }
    }

    public class SaveData
    {
        public Dictionary<string, List<Word>> WordsByCategory { get; set; }
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }
    }

    // Диалоговое окно для добавления новых слов
    public class AddWordDialog : Window
    {
        private ComboBox cmbCategories;
        private TextBox txtForeignWord;
        private TextBox txtTranslation;
        private Button btnOk;
        private Button btnCancel;

        public string SelectedCategory { get; private set; }
        public string ForeignWord { get; private set; }
        public string Translation { get; private set; }

        public AddWordDialog(List<string> existingCategories)
        {
            Title = "Добавить новое слово";
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Owner = Application.Current.MainWindow;

            var grid = new Grid();
            grid.Margin = new Thickness(10);
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Категория
            var lblCategory = new TextBlock { Text = "Категория:", Margin = new Thickness(0, 0, 0, 5) };
            grid.Children.Add(lblCategory);
            Grid.SetRow(lblCategory, 0);

            cmbCategories = new ComboBox { Margin = new Thickness(0, 0, 0, 10), Height = 30 };
            cmbCategories.ItemsSource = existingCategories;
            if (existingCategories.Count > 0)
                cmbCategories.SelectedIndex = 0;
            cmbCategories.IsEditable = true;
            grid.Children.Add(cmbCategories);
            Grid.SetRow(cmbCategories, 1);

            // Иностранное слово
            var lblForeign = new TextBlock { Text = "Слово на иностранном языке:", Margin = new Thickness(0, 0, 0, 5) };
            grid.Children.Add(lblForeign);
            Grid.SetRow(lblForeign, 2);

            txtForeignWord = new TextBox { Margin = new Thickness(0, 0, 0, 10), Height = 30 };
            grid.Children.Add(txtForeignWord);
            Grid.SetRow(txtForeignWord, 3);

            // Перевод
            var lblTranslation = new TextBlock { Text = "Перевод:", Margin = new Thickness(0, 0, 0, 5) };
            grid.Children.Add(lblTranslation);
            Grid.SetRow(lblTranslation, 4);

            txtTranslation = new TextBox { Margin = new Thickness(0, 0, 0, 10), Height = 30 };
            grid.Children.Add(txtTranslation);
            Grid.SetRow(txtTranslation, 5);

            // Кнопки
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 10, 0, 0) };
            btnOk = new Button { Content = "OK", Width = 80, Height = 30, Margin = new Thickness(0, 0, 10, 0), IsDefault = true };
            btnOk.Click += BtnOk_Click;
            btnCancel = new Button { Content = "Отмена", Width = 80, Height = 30, IsCancel = true };
            btnCancel.Click += (s, e) => DialogResult = false;
            buttonPanel.Children.Add(btnOk);
            buttonPanel.Children.Add(btnCancel);
            grid.Children.Add(buttonPanel);
            Grid.SetRow(buttonPanel, 6);

            Content = grid;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            SelectedCategory = cmbCategories.Text;
            ForeignWord = txtForeignWord.Text.Trim();
            Translation = txtTranslation.Text.Trim();

            if (string.IsNullOrEmpty(SelectedCategory))
            {
                MessageBox.Show("Введите категорию", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(ForeignWord))
            {
                MessageBox.Show("Введите слово на иностранном языке", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(Translation))
            {
                MessageBox.Show("Введите перевод", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
        }
    }
}