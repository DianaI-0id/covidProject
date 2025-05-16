using Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace covidProject
{
    public class Person
    {
        public int Id { get; set; }
        public Point Position { get; set; }
        public PersonStatus Status { get; set; }
        public double DaysInfected { get; set; }
        public double SpeedX { get; set; }
        public double SpeedY { get; set; }
        public bool IsInQuarantine { get; set; }
        private Random Random { get; } = new Random();

        public Person(Point position, PersonStatus status = PersonStatus.Susceptible)
        {
            Position = position;
            Status = status;
            DaysInfected = 0;
            IsInQuarantine = false;

            SpeedX = Random.NextDouble() * 2 - 1;
            SpeedY = Random.NextDouble() * 2 - 1;
        }

        public void Move(double canvasWidth, double canvasHeight)
        {
            Position = new Point(Position.X + SpeedX, Position.Y + SpeedY);

            if (Position.X < 0 || Position.X > canvasWidth)
            {
                SpeedX *= -1;
            }
            if (Position.Y < 0 || Position.Y > canvasHeight)
            {
                SpeedY *= -1; // Исправлено
            }
        }
    }

}
