using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Collections.ObjectModel;
using LiveChartsCore.SkiaSharpView;

namespace covidProject
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private PopulationManager _populationManager;

        //определим при нажатии на кнопку старта:
        private int populationSize;
        private double infectionRadius;

        //поля, относящиеся к canvas для основного поля и поля карантина
        private Canvas InfectionField;
        private Canvas QuarantineField;

        private double infectionFieldWidth;
        private double infectionFieldHeight;

        private double quarantineFieldWidth;
        private double quarantineFieldHeight;

        //шанс инфицирования
        private double infectionChance;

        //списки для работы внутри данного класса
        private List<Person> _people = new List<Person>();
        private List<Ellipse> _personEllipses = new List<Ellipse>();
        private List<Ellipse> _infectionRadiusEllipses = new List<Ellipse>();

        //списки для карантина
        private List<Person> _quarantinePeople = new List<Person>();
        private List<Ellipse> _quarantineEllipses = new List<Ellipse>();
        private List<Ellipse> _quarantineInfectionRadiusEllipses = new List<Ellipse>();

        // Вероятность попадания в карантин
        private double _quarantineProbability = 0.8; 

        private bool _quarantineActivated = false; //проверяем статус карантинной зоны - достигло ли число заболевших 50 для работы карантина

        private int maxInfectionCount; //сколько при карантинной зоне рандомно заразится людей в основной зоне

        private bool IsQuarantineZoneAvaliable = false;

        string infectChanceItem; //вероятность заражения, которую получаем из combobox

        private Random Random = new Random();

        private int accidentalInfections = 0; //счетчик случаййных заражений

        public SolidColorPaint LegendTextPaint { get; set; } =
            new SolidColorPaint
            {
                Color = new SKColor(0, 0, 0)
            };

        public ISeries[] Series { get; set; }

        //для отображения графика
        public ObservableCollection<int> SusceptibleList { get; set; } 
        public ObservableCollection<int> InfectedList { get; set; } 
        public ObservableCollection<int> RemovedList { get; set; } 
        public ObservableCollection<int> DeadList { get; set; } 

        //количество зараженных каждой категории
        private int _susceptible;
        public int Susceptible
        {
            get => _susceptible;
            set
            {
                _susceptible = value;
                OnPropertyChanged(nameof(Susceptible));
            }
        }

        private int _neverIsolated;
        public int NeverIsolated
        {
            get => _neverIsolated;
            set
            {
                _neverIsolated = value;
                OnPropertyChanged(nameof(NeverIsolated));
            }
        }

        private int _infected;
        public int Infected
        {
            get => _infected;
            set
            {
                _infected = value;
                OnPropertyChanged(nameof(Infected));
            }
        }

        private int _removed;
        public int Removed
        {
            get => _removed;
            set
            {
                _removed = value;
                OnPropertyChanged(nameof(Removed));
            }
        }

        private int _dead;
        public int Dead
        {
            get => _dead;
            set
            {
                _dead = value;
                OnPropertyChanged(nameof(Dead));
            }
        }

        private double _daysCount;
        public double DaysCount
        {
            get => _daysCount;
            set
            {
                _daysCount = value;
                OnPropertyChanged(nameof(DaysCount));
            }
        }

        //таймеры
        private DispatcherTimer moveTimer;
        private DispatcherTimer dayTimer;  

        public MainWindow()
        {
            InitializeComponent();

            _populationManager = new PopulationManager();

            InitializeFields();
            InitializeTimers();
            InitializeGraphData();

            maxInfectionCount = Random.Next(2, 20); //сколько при карантинной зоне рандомно заразится людей

            DataContext = this;
        }

        private void InitializeGraphData()
        {
            // Инициализируем коллекции ObservableCollection
            SusceptibleList = new ObservableCollection<int>();
            InfectedList = new ObservableCollection<int>();
            RemovedList = new ObservableCollection<int>();
            DeadList = new ObservableCollection<int>();

            // Создаем Series только один раз
            Series = new ISeries[]
            {
                new StackedAreaSeries<int>
                {
                    Name = "Suspectible",
                    Fill = new SolidColorPaint(SKColors.Blue),
                    Values = SusceptibleList
                },
                new StackedAreaSeries<int>
                {
                    Name = "Infected",
                    Fill = new SolidColorPaint(SKColors.Red),
                    Values = InfectedList
                },
                new StackedAreaSeries<int>
                {
                    Name = "Removed",
                    Fill = new SolidColorPaint(SKColors.Gray),
                    Values = RemovedList
                },
                new StackedAreaSeries<int>
                {
                    Name = "Dead",
                    Fill = new SolidColorPaint(SKColors.DarkViolet),
                    Values = DeadList
                }
            };
        }

        private void InitializeFields()
        {
            // Инициализация полей Canvas
            InfectionField = this.FindControl<Canvas>("Field");
            QuarantineField = this.FindControl<Canvas>("Quarantine");

            // Инициализация размеров полей
            infectionFieldWidth = InfectionField.Width;
            infectionFieldHeight = InfectionField.Height;
            
            quarantineFieldWidth = QuarantineField.Width;
            quarantineFieldHeight = QuarantineField.Height;
        }

        private void InitializeTimers()
        {
            moveTimer = new DispatcherTimer();
            moveTimer.Interval = TimeSpan.FromMilliseconds(30);
            moveTimer.Tick += MoveTimer_Tick;

            dayTimer = new DispatcherTimer();
            dayTimer.Interval = TimeSpan.FromMilliseconds(500);
            dayTimer.Tick += DayTimer_Tick;
        }


        private void DayTimer_Tick(object? sender, EventArgs e)
        {
            DaysCount++;
            _populationManager.CheckRecoveryAndDeath(_people);
            _populationManager.CheckRecoveryAndDeath(_quarantinePeople);

            _populationManager.UpdatePeopleCategoriesCount(_people);

            if (IsQuarantineZoneAvaliable)
            {
                // Проверка на карантин
                if (_people.Count(p => p.Status == PersonStatus.Infected) >= 50)
                {
                    _quarantineActivated = true;
                    var person = _people.FirstOrDefault(p => p.IsInQuarantine == false);
                    if (person != null)
                    {
                        MoveToQuarantine(person, _personEllipses[_people.IndexOf(person)], _people.IndexOf(person));
                    }
                }
            }

            if (_quarantineActivated)
            {
                // Продолжать перемещать новых зараженных в карантинную зону
                for (int i = _people.Count - 1; i >= 0; i--)
                {
                    var person = _people[i];
                    if (person.Status == PersonStatus.Infected) // Проверяем статус
                    {                
                        MoveToQuarantine(person, _personEllipses[i], i);
                    }
                }
            }

            //с вероятностью, равной вероятности заражения на поле после ввода карантинной зоны появится один заболевший
            if (Random.NextDouble() <= infectionChance && accidentalInfections <= maxInfectionCount && _quarantineActivated)
            {
                var person = _people[Random.Next(0, _people.Count - 1)];
                if (person.Status == PersonStatus.Susceptible)
                {
                    if (Random.NextDouble() <= _quarantineProbability) //шанс карантина 80%
                    {
                        person.Status = PersonStatus.Infected;
                        CreateInfectionRadiusEllipse(person);
                    }
                    else
                    {
                        person.Status = PersonStatus.NeverIsolated;

                        var personEllipse = _personEllipses[_people.IndexOf(person)];
                        personEllipse.Fill = Brushes.Yellow;
                        CreateInfectionRadiusEllipse(person);

                        var infectionRadiusEllipse = _infectionRadiusEllipses.FirstOrDefault(e => (int)e.Tag == person.Id);
                        if (infectionRadiusEllipse != null)
                        {
                            infectionRadiusEllipse.Stroke = Brushes.Yellow;
                        }
                    }
                    accidentalInfections++;
                }
            }
        }

        private void MoveTimer_Tick(object? sender, EventArgs e)
        {
            // Обновление позиций людей в основной зоне
            foreach (var person in _people)
            {
                person.Move(infectionFieldWidth, infectionFieldHeight);
            }

            // Добавление радиусов заражения для новых инфицированных в основной зоне
            foreach (var person in _people.Where(p => p.Status == PersonStatus.Infected && !_infectionRadiusEllipses.Any(e => (int)e.Tag == p.Id)))
            {
                CreateInfectionRadiusEllipse(person);
            }

            RedrawCanvas(_people, _personEllipses, false);
            _populationManager.CheckInfections(_people); 

            if (IsQuarantineZoneAvaliable)
            {
                // Обновление позиций людей в карантинной зоне
                foreach (var person in _quarantinePeople)
                {
                    person.Move(quarantineFieldWidth, quarantineFieldHeight); // Обновление позиций в карантинной зоне
                }
                RedrawCanvas(_quarantinePeople, _quarantineEllipses, true);
                _populationManager.CheckInfections(_quarantinePeople);

                // Добавление радиусов заражения для новых инфицированных в карантинной зоне
                foreach (var person in _quarantinePeople.Where(p => p.Status == PersonStatus.Infected && !_quarantineInfectionRadiusEllipses.Any(e => (int)e.Tag == p.Id)))
                {
                    CreateQuarantineInfectionRadiusEllipse(person);
                }
            }

            // Обновляем количество людей в каждой группе
            Susceptible = _people.Count(p => p.Status == PersonStatus.Susceptible);
            Infected = _people.Count(p => p.Status == PersonStatus.Infected) + _quarantinePeople.Count(p => p.Status == PersonStatus.Infected);
            Removed = _people.Count(p => p.Status == PersonStatus.Removed) + _quarantinePeople.Count(p => p.Status == PersonStatus.Removed);
            Dead = _people.Count(p => p.Status == PersonStatus.Dead) + _quarantinePeople.Count(p => p.Status == PersonStatus.Dead);
            NeverIsolated = _people.Count(p => p.Status == PersonStatus.NeverIsolated);

            // Добавляем новые данные в ObservableCollection
            SusceptibleList.Add(Susceptible);
            InfectedList.Add(Infected);
            RemovedList.Add(Removed);
            DeadList.Add(Dead);
        }

        private void MoveToQuarantine(Person person, Ellipse personEllipse, int index)
        {
            // Сработал шанс 100% - отправляем в карантин
            person.Position = new Point(Random.NextDouble() * QuarantineField.Width, Random.NextDouble() * QuarantineField.Height);

            person.IsInQuarantine = true;
            _quarantinePeople.Add(person);
            _people.RemoveAt(index);

            // Перемещение эллипса в карантинную зону
            _personEllipses.RemoveAt(index);
            InfectionField.Children.Remove(personEllipse);
            QuarantineField.Children.Add(personEllipse);
            personEllipse.Fill = Brushes.Red; // Устанавливаем красный цвет при перемещении в карантин

            // Обновление координат эллипса
            Canvas.SetLeft(personEllipse, person.Position.X - 3);
            Canvas.SetTop(personEllipse, person.Position.Y - 3);

            // Перемещение радиуса заражения в карантинную зону
            var infectionRadiusEllipse = _infectionRadiusEllipses.FirstOrDefault(e => (int)e.Tag == person.Id);
            if (infectionRadiusEllipse != null)
            {
                InfectionField.Children.Remove(infectionRadiusEllipse); // Удаляем из основной зоны
                QuarantineField.Children.Add(infectionRadiusEllipse);

                _infectionRadiusEllipses.Remove(infectionRadiusEllipse);

                Canvas.SetLeft(infectionRadiusEllipse, person.Position.X - infectionRadius);
                Canvas.SetTop(infectionRadiusEllipse, person.Position.Y - infectionRadius);

                infectionRadiusEllipse.Stroke = Brushes.Red; // Устанавливаем красный цвет радиуса в карантине
                _quarantineInfectionRadiusEllipses.Add(infectionRadiusEllipse);
            }
            else
            {
                // Если радиуса заражения нет, создаем новый в карантинной зоне
                CreateQuarantineInfectionRadiusEllipse(person);
            }

            _quarantineEllipses.Add(personEllipse);
        }

        //перерисовываем холст для каждого из полей
        private void RedrawCanvas(List<Person> people, List<Ellipse> ellipses, bool isQuarantineZone)
        {
            for (int i = 0; i < people.Count; i++)
            {
                var person = people[i];
                var personEllipse = ellipses[i];

                Canvas.SetLeft(personEllipse, person.Position.X - 3);
                Canvas.SetTop(personEllipse, person.Position.Y - 3);

                // Обновление цвета в зависимости от статуса
                if (person.Status == PersonStatus.Infected)
                {
                    personEllipse.Fill = Brushes.Red;
                    if (isQuarantineZone)
                    {
                        UpdateQuarantineInfectionRadiusEllipse(person);
                    }
                    else
                    {
                        UpdateInfectionRadiusEllipse(person); // <---  Добавлено для основной зоны
                    }
                }
                else if (person.Status == PersonStatus.NeverIsolated)
                {
                    personEllipse.Fill = Brushes.Yellow;
                    UpdateInfectionRadiusEllipse(person);
                }
                else if (person.Status == PersonStatus.Removed)
                {
                    personEllipse.Fill = Brushes.Gray;

                    if (person.IsInQuarantine)
                    {
                        RemoveQuarantineInfectionRadiusEllipse(person);
                    }
                    else
                    {
                        RemoveInfectionRadiusEllipse(person);
                    }               
                }
                else if (person.Status == PersonStatus.Dead)
                {
                    personEllipse.Fill = Brushes.Purple;
                    
                    if (person.IsInQuarantine)
                    {
                        RemoveQuarantineInfectionRadiusEllipse(person);
                    }
                    else
                    {
                        RemoveInfectionRadiusEllipse(person);
                    }                
                }
                else
                {
                    personEllipse.Fill = Brushes.Blue;
                }
            }
        }

        private void CreateQuarantineInfectionRadiusEllipse(Person person)
        {
            Ellipse infectionRadiusEllipse = new Ellipse
            {
                Width = infectionRadius * 2,
                Height = infectionRadius * 2,
                Stroke = Brushes.Red,
                StrokeThickness = 1,
                Fill = Brushes.Transparent,
                Tag = person.Id
            };

            Canvas.SetLeft(infectionRadiusEllipse, person.Position.X - infectionRadius);
            Canvas.SetTop(infectionRadiusEllipse, person.Position.Y - infectionRadius);

            QuarantineField.Children.Add(infectionRadiusEllipse);
            _quarantineInfectionRadiusEllipses.Add(infectionRadiusEllipse);
        }

        private void UpdateQuarantineInfectionRadiusEllipse(Person person)
        {
            var infectionRadiusEllipse = _quarantineInfectionRadiusEllipses.FirstOrDefault(e => (int)e.Tag == person.Id);
            if (infectionRadiusEllipse != null)
            {
                Canvas.SetLeft(infectionRadiusEllipse, person.Position.X - infectionRadius);
                Canvas.SetTop(infectionRadiusEllipse, person.Position.Y - infectionRadius);
            }
        }

        private void RemoveQuarantineInfectionRadiusEllipse(Person person)
        {
            var infectionRadiusEllipseToRemove = _quarantineInfectionRadiusEllipses.FirstOrDefault(e => (int)e.Tag == person.Id);
            if (infectionRadiusEllipseToRemove != null)
            {
                QuarantineField.Children.Remove(infectionRadiusEllipseToRemove);
                _quarantineInfectionRadiusEllipses.Remove(infectionRadiusEllipseToRemove);
            }
        }

        private void SelectInfectChance_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            if (combo != null)
            {
                ComboBoxItem item = combo.SelectedItem as ComboBoxItem;
                if (item != null)
                {
                    infectChanceItem = item.Content as string;
                    _populationManager.SetInfectionChance(infectChanceItem);
                    infectionChance = _populationManager.InfectionChance;
                }
            }
        }

        private void CreateInfectionRadiusEllipse(Person person) //создаем эллипс для конкретного человека
        {
            Ellipse infectionRadiusEllipse = new Ellipse
            {
                Width = infectionRadius * 2,
                Height = infectionRadius * 2,
                Stroke = Brushes.Red,
                StrokeThickness = 1,
                Fill = Brushes.Transparent,
                Tag = person.Id
            };

            Canvas.SetLeft(infectionRadiusEllipse, person.Position.X - infectionRadius);
            Canvas.SetTop(infectionRadiusEllipse, person.Position.Y - infectionRadius);

            InfectionField.Children.Add(infectionRadiusEllipse);
            _infectionRadiusEllipses.Add(infectionRadiusEllipse);
        }

        private void RemoveInfectionRadiusEllipse(Person person) //удаляем эллипс-радиус с канваса и из списка
        {
            var infectionRadiusEllipseToRemove = _infectionRadiusEllipses.FirstOrDefault(e => (int)e.Tag == person.Id);
            if (infectionRadiusEllipseToRemove != null)
            {
                InfectionField.Children.Remove(infectionRadiusEllipseToRemove);
                _infectionRadiusEllipses.Remove(infectionRadiusEllipseToRemove);
            }
        }

        private void UpdateInfectionRadiusEllipse(Person person) //обновляем положение радиуса заражения для человека
        {
            var infectionRadiusEllipse = _infectionRadiusEllipses.FirstOrDefault(e => (int)e.Tag == person.Id);
            if (infectionRadiusEllipse != null)
            {
                Canvas.SetLeft(infectionRadiusEllipse, person.Position.X - infectionRadius);
                Canvas.SetTop(infectionRadiusEllipse, person.Position.Y - infectionRadius);
            }
        }

        private void StartInfection_ButtonClick(object sender, RoutedEventArgs e)
        {
            populationSize = Convert.ToInt32(PopulationSizeInput.Text);
            infectionRadius = Convert.ToDouble(InfectionRadiusInput.Text);

            _populationManager.SetInfectionRadius(infectionRadius);
            _populationManager.InitializePopulation(populationSize, infectionFieldWidth, infectionFieldHeight);
            _populationManager.StartInfection();

            _people = _populationManager.People.ToList();
            CreateEllipsesForPeople();

            moveTimer.Start();
            dayTimer.Start();
        }

        private void CreateEllipsesForPeople()
        {
            foreach (var person in _people)
            {
                Ellipse ellipse = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Tag = person.Id,
                    Fill = Brushes.Blue
                };

                Canvas.SetLeft(ellipse, person.Position.X - 3); // Смещение на половину ширины эллипса
                Canvas.SetTop(ellipse, person.Position.Y - 3); // Смещение на половину высоты эллипса

                InfectionField.Children.Add(ellipse);
                _personEllipses.Add(ellipse);

                if (person.Status == PersonStatus.Infected)
                {
                    CreateInfectionRadiusEllipse(person);
                }
            }
        }

        private void Stop_ButtonClick(object sender, RoutedEventArgs e)
        {
            // Остановка таймеров
            moveTimer.Stop();
            dayTimer.Stop();

            // Очистка холста
            InfectionField.Children.Clear();
            _personEllipses.Clear();
            _infectionRadiusEllipses.Clear();

            QuarantineField.Children.Clear();
            _quarantineActivated = false;
            _quarantineEllipses.Clear();
            _quarantineInfectionRadiusEllipses.Clear();

            // Очистка списков
            _people.Clear();
            _quarantinePeople.Clear();

            // Очищаем списки и график
            SusceptibleList.Clear();
            InfectedList.Clear();
            RemovedList.Clear();
            DeadList.Clear();

            Susceptible = 0;
            Infected = 0;
            Removed = 0;
            Dead = 0;
            NeverIsolated = 0;

            // Сброс переменных
            DaysCount = 0;

            InfectChance_ComboBox.SelectedIndex = -1;
            PopulationSizeInput.Clear();
            InfectionRadiusInput.Clear();

            // Сброс состояния PopulationManager
            _populationManager.People.Clear();
        }

        private void CheckBox_Checked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            IsQuarantineZoneAvaliable = true;
            QuarantineZoneStackPanel.IsVisible = true;
            TitlePageText.Text = "USE QUARANTINE ZONE";
        }

        private void CheckBox_Unchecked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            IsQuarantineZoneAvaliable = false;
            QuarantineZoneStackPanel.IsVisible = false;
            TitlePageText.Text = "DO NOTHING";
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}