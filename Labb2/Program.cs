using System;
using System.Collections.Generic;
using System.IO;

abstract class LevelElement
{
    public int X { get; set; }
    public int Y { get; set; }

    public char Symbol { get; protected set; }
    public ConsoleColor Color { get; protected set; }

    public void Draw()
    {
        Console.ForegroundColor = Color;
        Console.SetCursorPosition(X, Y);
        Console.Write(Symbol);
        Console.ResetColor();
    }
}



class Wall : LevelElement
{
    public bool Discovered { get; set; }

    public Wall(int x, int y)
    {
        X = x;
        Y = y;
        Symbol = '#';
        Color = ConsoleColor.Gray;
        Discovered = false;
    }
}




class Dice
{
    private int numberOfDice;
    private int sidesPerDice;
    private int modifier;

    private static Random random = new Random();

    public Dice(int numberOfDice, int sidesPerDice, int modifier)
    {
        this.numberOfDice = numberOfDice;
        this.sidesPerDice = sidesPerDice;
        this.modifier = modifier;
    }


    public int Throw()
    {
        int sum = 0;
        for (int i = 0; i < numberOfDice; i++)
            sum += random.Next(1, sidesPerDice + 1);
        return sum + modifier;
    }

    public override string ToString() => $"{numberOfDice}d{sidesPerDice}+{modifier}";
}

    
    
    
abstract class Enemy : LevelElement
{
    public string name { get; protected set; }
    public int HP { get; set; }
    public Dice AttackDice { get; protected set; }
    public Dice DefenseDice { get; protected set; }

    public abstract void Update(Player player);
}

class Rat : Enemy
{
    private static Random random = new Random();

    public Rat(int x, int y)
    {
        X = x; Y = y;
        Symbol = 'r'; Color = ConsoleColor.Yellow;
        name = "Rat"; HP = 10;
        AttackDice = new Dice(1, 6, 3);
        DefenseDice = new Dice(1, 6, 1);
    }

    public override void Update(Player player)
    {
        int newX = X;
        int newY = Y;

        int direction = random.Next(4);

        switch (direction)
        {
            case 0:
                newY--;
                break;

            case 1:
                newY++;
                break;

            case 2:
                newX--;
                break;

            case 3:
                newX++;
                break;
        }

        X = newX;
        Y = newY;
    }


}

    class Snake : Enemy
{
    public Snake(int x, int y)
    {
        X = x; Y = y;
        Symbol = 's'; Color = ConsoleColor.Green;
        name = "Snake"; HP = 25;
        AttackDice = new Dice(3, 4, 2);
        DefenseDice = new Dice(1, 8, 5);
    }

    public override void Update(Player player)
    {
        double distance = Math.Sqrt(
            Math.Pow(X - player.X, 2) + Math.Pow(Y - player.Y, 2));
        if (distance <= 2)
        {
            if (player.X < X) X++;
            else if (player.X > X) X--;

            if (player.Y < Y) Y++;
            else if (player.Y > Y) Y--;
        }
    }
}

class Player : LevelElement
{
    public int HP { get; set; }
    public Dice AttackDice { get; set; }
    public Dice DefenseDice { get; set; }

    public Player(int x, int y)
    {
        X = x; Y = y;
        Symbol = '@'; Color = ConsoleColor.Cyan;
        HP = 100;
        AttackDice = new Dice(2, 6, 2);
        DefenseDice = new Dice(2, 6, 0);
    }
}

class LevelData
{
    private List<LevelElement> elements = new();
    public IReadOnlyList<LevelElement> Elements => elements;
    public int PlayerStartX { get; private set; }
    public int PlayerStartY { get; private set; }

    public void Load(string fileName)
    {
        string[] lines = File.ReadAllLines(fileName);
        for (int y = 0; y < lines.Length; y++)
        {
            for (int x = 0; x < lines[y].Length; x++)
            {
                char c = lines[y][x];
                switch (c)
                {
                    case '#': elements.Add(new Wall(x, y)); break;
                    case 'r': elements.Add(new Rat(x, y)); break;
                    case 's': elements.Add(new Snake(x, y)); break;
                    case '@': PlayerStartX = x; PlayerStartY = y; break;
                }
            }
        }
    }

    public void RemoveDeadEnemies() => elements.RemoveAll(e => e is Enemy enemy && enemy.HP <= 0);
}

class Program
{
    static void Main()
    {
        Console.CursorVisible = false;
        LevelData level = new LevelData();
        level.Load("Level1.txt");
        Player player = new Player(level.PlayerStartX, level.PlayerStartY);

        bool running = true;
        while (running)
        {
            DrawMap(level, player);
            ConsoleKey key = Console.ReadKey(true).Key;
            int newX = player.X;
            int newY = player.Y;

            switch (key)
            {
                case ConsoleKey.W: newY--; break;
                case ConsoleKey.S: newY++; break;
                case ConsoleKey.A: newX--; break;
                case ConsoleKey.D: newX++; break;
                case ConsoleKey.Escape: running = false; break;
            }

            Enemy targetEnemy = GetEnemyAt(level, newX, newY);
            if (targetEnemy != null)
            {
                PlayerAttack(player, targetEnemy);

                if (targetEnemy.HP <= 0)
                {
                    player.X = newX;
                    player.Y = newY;
                }
            }
            else if (!IsWall(level, newX, newY))
            {
                player.X = newX; player.Y = newY;
            }

            foreach (LevelElement element in level.Elements)
            {
                if (element is Enemy e)
                {
                    int oldX = e.X;
                    int oldY = e.Y;

                    e.Update(player);

                    if (IsWall(level, e.X, e.Y) ||
                        IsEnemyAt(level, e, e.X, e.Y))
                    {
                        e.X = oldX;
                        e.Y = oldY;
                    }

                    if (
                        (e.X == player.X && Math.Abs(e.Y - player.Y) == 1)
                        ||
                        (e.Y == player.Y && Math.Abs(e.X - player.X) == 1)
                    )
                    {
                        EnemyAttack(player, e);
                    }
                }
            }

            level.RemoveDeadEnemies();

            if (player.HP <= 0)
            {
                Console.Clear(); Console.WriteLine("GAME OVER"); break;
            }
        }
    }

    static bool IsVisible(Player player, int x, int y)
    {
        double distance = Math.Sqrt(Math.Pow(x - player.X, 2) + Math.Pow(y - player.Y, 2));
        return distance <= 5;
    }

    static Enemy GetEnemyAt(LevelData level, int x, int y)
    {
        foreach (LevelElement element in level.Elements)
        {
            if (element is Enemy enemy && enemy.X == x && enemy.Y == y)
                return enemy;
        }
        return null;
    }

    static bool IsEnemyAt(LevelData level, Enemy currentEnemy, int x, int y)
    {
        foreach (LevelElement element in level.Elements)
        {
            if (element is Enemy enemy &&
                !ReferenceEquals(enemy, currentEnemy) &&
                enemy.X == x &&
                enemy.Y == y)
            {
                return true;
            }
        }

        return false;
    }

    static void PlayerAttack(Player player, Enemy enemy)
    {
        int attack = player.AttackDice.Throw();
        int defence = enemy.DefenseDice.Throw();

        int damage = attack - defence;

        if (damage > 0)
            enemy.HP -= damage;
        if (enemy.HP <= 0)
        {
            Console.SetCursorPosition(0, 20);
            Console.Write($"{enemy.name} died! ");
            return;
        }

        Console.SetCursorPosition(0, 20);

        if (damage > 0)
            Console.Write($"You dealt {damage} damage! ");
        else
            Console.Write("You missed! ");

        attack = enemy.AttackDice.Throw();
        defence = player.DefenseDice.Throw();

        damage = attack - defence;

        if (damage > 0)
            player.HP -= damage;

        Console.SetCursorPosition(0, 21);
        Console.Write($"Player HP: {player.HP} ");

        Console.SetCursorPosition(0, 22);
        Console.Write($"{enemy.name} HP: {enemy.HP} ");
    }

    static void EnemyAttack(Player player, Enemy enemy)
    {
        int attack = enemy.AttackDice.Throw();
        int defence = player.DefenseDice.Throw();

        int damage = attack - defence;

        if (damage > 0)
            player.HP -= damage;

        Console.SetCursorPosition(0, 23);

        if (damage > 0)
            Console.Write($"{enemy.name} dealt {damage} damage! ");
        else
            Console.Write($"{enemy.name} missed! ");

        if (player.HP <= 0)
            return;

        attack = player.AttackDice.Throw();
        defence = enemy.DefenseDice.Throw();

        damage = attack - defence;

        if (damage > 0)
            enemy.HP -= damage;

        Console.SetCursorPosition(0, 24);
        Console.Write($"Player HP: {player.HP} ");

        Console.SetCursorPosition(0, 25);
        Console.Write($"{enemy.name} HP: {enemy.HP} ");
    }

    static void DrawMap(LevelData level, Player player)
    {
        Console.Clear();
        foreach (LevelElement element in level.Elements)
        {
            if (element is Wall wall)
            {
                if (IsVisible(player, wall.X, wall.Y))
                {
                    wall.Discovered = true;
                }

                if (wall.Discovered)
                {
                    wall.Draw();
                }
            }
            else if (IsVisible(player, element.X, element.Y))
            {
                element.Draw();
            }
        }

        player.Draw();
        Console.SetCursorPosition(0, 19);
        Console.Write($"Player HP: {player.HP} ");
    }

    static bool IsWall(LevelData level, int x, int y)
    {
        foreach (LevelElement element in level.Elements)
        {
            if (element is Wall && element.X == x && element.Y == y) return true;
        }
        return false;
    }
}
