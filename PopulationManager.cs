using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using System.Text;
using System.Threading.Tasks;

namespace covidProject
{
    public class PopulationManager
    {
        public List<Person> People { get; } = new List<Person>();
        public List<Person> QuarantinePeople { get; } = new List<Person>();
        private Random Random { get; } = new Random();
        public double InfectionChance {  get; set; }
        public double InfectionRadius {  get; set; }

        public int SusceptibleCount { get; set; } //храним количество каждой из категорий для вывода в верстке
        public int InfectedCount { get; set; }
        public int RemovedCount { get; set; }
        public int DeadCount { get; set; }
        public int NeverIsolatedCount { get; set; }

        public void InitializePopulation(int populationCount, double canvasWidth, double canvasHeight)
        {
            People.Clear();

            for (int i = 0; i < populationCount; i++)
            {
                Point startPosition = new Point(Random.NextDouble() * canvasWidth, Random.NextDouble() * canvasHeight);
                Person person = new Person(startPosition, PersonStatus.Susceptible)
                {
                    Id = i + 1
                };

                People.Add(person);
            }
        }

        public void SetInfectionChance(string choosenChance)
        {
            InfectionChance = Convert.ToDouble(choosenChance) / 100;
        }

        public void SetInfectionRadius(double infectionRadius)
        {
            InfectionRadius = infectionRadius;
        }


        //проверяет, в какую категорию попадет человек при имеющихся шансах
        public void CheckRecoveryAndDeath(List<Person> people)
        {
            foreach (var person in people.Where(p => p.Status == PersonStatus.Infected || p.Status == PersonStatus.NeverIsolated))
            {
                if (person.DaysInfected >= 21)
                {
                    if (Random.NextDouble() <= 0.12) 
                    {
                        person.Status = PersonStatus.Dead;
                    }
                    else 
                    {
                        person.Status = PersonStatus.Removed;
                    }
                }
                else
                {
                    person.DaysInfected += 1; //пока длительность заражения не достигнет 21 дня, будем увеличивать это значение
                }
            }
        }

        public void UpdatePeopleCategoriesCount(List<Person> people)
        {
            SusceptibleCount = 0;
            InfectedCount = 0;
            RemovedCount = 0;
            DeadCount = 0;
            NeverIsolatedCount = 0;

            foreach (var person in people)
            {
                if (person.Status == PersonStatus.Susceptible)
                {
                    SusceptibleCount++;
                }
                else if (person.Status == PersonStatus.Infected)
                {
                    InfectedCount++;
                }
                else if (person.Status == PersonStatus.Removed)
                {
                    RemovedCount++;
                }
                else if (person.Status == PersonStatus.Dead)
                {
                    DeadCount++;
                }
                else if (person.Status == PersonStatus.NeverIsolated)
                {
                    NeverIsolatedCount++;
                }
            }
        }

        //на старте заражения инфицированных будет несколько
        public void StartInfection()
        {
            var infectedCount = Random.Next(1, 3);

            for (int i = 0; i < infectedCount; i++)
            {
                People[i].Status = PersonStatus.Infected;
            }
        }

        public double Distance(Person p1, Person p2)
        {
            return Math.Sqrt(Math.Pow(p1.Position.X - p2.Position.X, 2) + Math.Pow(p1.Position.Y - p2.Position.Y, 2));
        }

        public void CheckInfections(List<Person> people)
        {
            foreach (var infectedPerson in people.Where(p => p.Status == PersonStatus.Infected || p.Status == PersonStatus.NeverIsolated))
            {
                foreach (var susceptiblePerson in people.Where(p => p.Status == PersonStatus.Susceptible))
                {
                    double distance = Distance(infectedPerson, susceptiblePerson);
                    if (distance <= InfectionRadius)
                    {
                        if (Random.NextDouble() <= InfectionChance) //если сработал шанс инфицирования
                        {
                            susceptiblePerson.Status = PersonStatus.Infected;
                        }
                    }
                }
            }
        }
    }
}
