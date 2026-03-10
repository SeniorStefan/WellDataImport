using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using WellDataImport.Models;
using WellDataImport.Services;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.IO;

namespace WellDataImport.ViewModels
{
    /// <summary>
    /// Главная ViewModel приложения.
    /// Содержит коллекции для привязки к UI, команды и логику загрузки/экспорта.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        // Сервис, отвечающий за чтение CSV, валидацию и построение сводок.
        private readonly WellDataService _service = new WellDataService();

        private bool _isBusy;
        private string _statusText;
        private string _currentFilePath;
        private WellSummary _selectedWell;

        // Список сводок по скважинам для отображения в главной таблице.
        public ObservableCollection<WellSummary> Wells { get; } = new ObservableCollection<WellSummary>();
        // Интервалы выбранной скважины.
        public ObservableCollection<Interval> Intervals { get; } = new ObservableCollection<Interval>();
        // Ошибки валидации, найденные при чтении CSV.
        public ObservableCollection<ValidationError> Errors { get; } = new ObservableCollection<ValidationError>();

        // Команда для асинхронной загрузки CSV-файла.
        public ICommand LoadCommand { get; }
        // Команда для экспорта сводки по скважинам в JSON.
        public ICommand ExportJsonCommand { get; }

        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusText
        {
            get { return _statusText; }
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CurrentFilePath
        {
            get { return _currentFilePath; }
            set
            {
                if (_currentFilePath != value)
                {
                    _currentFilePath = value;
                    OnPropertyChanged();
                }
            }
        }

        public WellSummary SelectedWell
        {
            get { return _selectedWell; }
            set
            {
                if (_selectedWell != value)
                {
                    _selectedWell = value;
                    OnPropertyChanged();
                    LoadIntervalsForSelectedWell();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainViewModel()
        {
            // При инициализации задаём команды.
            LoadCommand = new RelayCommand(async _ => await LoadAsync(), _ => !IsBusy);
            ExportJsonCommand = new RelayCommand(_ => ExportJson(), _ => Wells.Count > 0 && !IsBusy);
        }

        /// <summary>
        /// Обработчик команды загрузки CSV.
        /// Показывает диалог выбора файла и асинхронно читает его содержимое.
        /// </summary>
        private async Task LoadAsync()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSV файлы (*.csv)|*.csv|Все файлы (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            CurrentFilePath = dialog.FileName;
            IsBusy = true;
            StatusText = "Загрузка файла...";

            try
            {
                var result = await _service.LoadFromCsvAsync(CurrentFilePath);

                // очищаем предыдущие данные перед загрузкой
                Wells.Clear();
                Errors.Clear();
                Intervals.Clear();

                var summaries = _service.BuildSummaries(result.wells);
                foreach (var s in summaries)
                {
                    Wells.Add(s);
                }

                foreach (var err in result.errors)
                {
                    Errors.Add(err);
                }

                StatusText = string.Format("Загружено скважин: {0}, ошибок: {1}", Wells.Count, Errors.Count);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при загрузке файла: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText = "Ошибка при загрузке файла.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Загрузка интервалов только для выбранной в таблице скважины.
        /// </summary>
        private void LoadIntervalsForSelectedWell()
        {
            Intervals.Clear();

            if (SelectedWell == null || string.IsNullOrEmpty(CurrentFilePath))
            {
                return;
            }

            // перечитываем файл и берём интервалы только выбранной скважины
            try
            {
                var result = _service.LoadFromCsv(CurrentFilePath);
                foreach (var well in result.wells)
                {
                    if (string.Equals(well.WellId, SelectedWell.WellId, StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var interval in well.Intervals)
                        {
                            Intervals.Add(interval);
                        }
                        break;
                    }
                }
            }
            catch
            {
                // игнорируем, список интервалов просто останется пустым
            }
        }

        /// <summary>
        /// Экспорт текущей сводки по скважинам в JSON-файл.
        /// </summary>
        private void ExportJson()
        {
            if (Wells.Count == 0)
            {
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "JSON файлы (*.json)|*.json|Все файлы (*.*)|*.*",
                FileName = "WellSummary.json"
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            try
            {
                var json = JsonConvert.SerializeObject(Wells, Formatting.Indented);
                File.WriteAllText(dialog.FileName, json, Encoding.UTF8);
                StatusText = "Сводка экспортирована в JSON.";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при экспорте JSON: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}

