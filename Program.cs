using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace BankAccountSimulation
{
    // === Базовий клас для банківського рахунку ===
    [XmlInclude(typeof(SavingsAccount))]
    [XmlInclude(typeof(CheckingAccount))]
    public class BankAccount
    {
        // === Основні властивості банківського рахунку ===
        public string AccountNumber { get; set; }// Номер рахунку
        public double Balance { get; set; }// Баланс рахунку

        public double CashOnHand { get; set; }// Готівка на руках
        public string AccountType { get; set; }// Тип рахунку

        public BankAccount() { }// Конструктор за замовчуванням

        public BankAccount(string accountNumber, double balance)
        {
            if (balance < 0)
                throw new ArgumentException("Баланс не може бути від'ємним.");
            AccountNumber = FormatAccountNumber(accountNumber);// Форматування номера рахунку
            Balance = balance;
            CashOnHand = 0;
            AccountType = "Звичайний рахунок";// Тип за замовчуванням
        }

        public virtual void Deposit(double amount)// === Поповнення рахунку ===
        {
            if (amount <= 0)
            {
                Console.WriteLine("Сума поповнення повинна бути більшою за 0.");
                return;
            }

            double cashUsed = 0;

            if (amount <= CashOnHand)// Використовуємо готівку для поповнення
            {
                CashOnHand -= amount;
                Balance += amount;
                cashUsed = amount;
                Console.WriteLine($"Рахунок поповнено на {amount}. Готівки залишилось: {CashOnHand}. Баланс: {Balance}");
            }
            else // Готівки недостатньо — решту поповнюємо з зовнішніх коштів
            {
                cashUsed = CashOnHand;
                Balance += CashOnHand;
                CashOnHand = 0;
                Balance += (amount - cashUsed); // Додаємо залишок
                Console.WriteLine($"Рахунок поповнено. Використано готівки: {cashUsed}, додано зовнішніх коштів: {amount - cashUsed}. Баланс: {Balance}");
            }
            // Якщо є борг (від'ємний баланс), він автоматично погашається
            if (Balance < 0)
            {
                double debt = Math.Min(-Balance, amount);
                Balance += debt;
                Console.WriteLine($"Погашено борг на суму: {debt}. Поточний баланс: {Balance}");
            }
        }

        // === Зняття коштів з рахунку ===
        public virtual void Withdraw(double amount)
        {
            if (amount <= 0)
            {
                Console.WriteLine("Сума зняття повинна бути більшою за 0.");
                return;
            }

            if (amount > Balance) // Недостатньо коштів
            {
                Console.WriteLine("Недостатньо коштів.");
                return;
            }

            Balance -= amount;
            CashOnHand += amount; // Зняті кошти додаються до готівки
            Console.WriteLine($"Кошти знято. Новий баланс: {Balance}. Налічні: {CashOnHand}");
        }

        // === Форматування номера рахунку ===
        public static string FormatAccountNumber(string accountNumber)
        {
            accountNumber = accountNumber.Replace("-", "").PadLeft(9, '0');
            return $"{accountNumber.Substring(0, 3)}-{accountNumber.Substring(3, 3)}-{accountNumber.Substring(6)}";
        }

        // === Відображення інформації про рахунок ===
        public override string ToString()
        {
            return $"Тип рахунку: {AccountType}, Номер рахунку: {AccountNumber}, Баланс: {Balance}, Налічні: {CashOnHand}";
        }
    }

    // Ощадний рахунок
    public class SavingsAccount : BankAccount
    {
        public double InterestRate { get; set; }

        public SavingsAccount() { }

        public SavingsAccount(string accountNumber, double balance, double interestRate)
            : base(accountNumber, balance)
        {
            InterestRate = interestRate;
            AccountType = "Ощадний рахунок";
        }

        // Поповнення рахунку з автоматичним додаванням відсотків
        public override void Deposit(double amount)
        {
            base.Deposit(amount);
            ApplyInterest();
        }

        public override void Withdraw(double amount)
        {
            base.Withdraw(amount);
            ApplyInterest();
        }

        private void ApplyInterest()
        {
            if (Balance > 0)
            {
                double interest = Balance * (InterestRate / 100);
                Balance += interest;
                Console.WriteLine($"Нараховано відсотки: {interest}. Новий баланс: {Balance}");
            }
        }
    }

    // Клас для рахунку з кредитним лімітом
    public class CheckingAccount : BankAccount
    {
        public double CreditLimit { get; set; }// Кредитний ліміт

        public CheckingAccount() { }

        public CheckingAccount(string accountNumber, double balance, double creditLimit)
            : base(accountNumber, balance)
        {
            CreditLimit = creditLimit;
            AccountType = "Рахунок з кредитним лімітом";
        }

        // Зняття коштів з урахуванням кредитного ліміту
        public override void Withdraw(double amount)
        {
            if (amount <= 0)
            {
                Console.WriteLine("Сума зняття повинна бути більшою за 0.");
                return;
            }

            if (amount > Balance + CreditLimit)
            {
                Console.WriteLine("Перевищено кредитний ліміт. Операція скасована.");
                return;
            }

            Balance -= amount;
            CashOnHand += amount;
            Console.WriteLine($"Кошти знято. Новий баланс: {Balance}. Налічні: {CashOnHand}");
        }
    }

    // Основний клас
    class Program
    {
        static List<BankAccount> accounts = new List<BankAccount>();
        static string filePath = "accounts.xml";
        static bool isSaved = true;

        static void Main(string[] args)
        {
            LoadAccounts();

            while (true)
            {
                Console.WriteLine("\nГоловне меню:");
                Console.WriteLine("1. Створити рахунок");
                Console.WriteLine("2. Дії з рахунком");
                Console.WriteLine("3. Інформація про рахунки");
                Console.WriteLine("4. Збереження та керування рахунками");
                Console.WriteLine("5. Вийти");

                string choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        CreateAccount();
                        break;
                    case "2":
                        ManageAccount();
                        break;
                    case "3":
                        DisplayAccounts();
                        break;
                    case "4":
                        ManageSaveAccounts();
                        break;
                    case "5":
                        if (!isSaved)
                        {
                            Console.Write("Рахунки не збережено. Ви точно хочете вийти? (y/n): ");
                            if (Console.ReadLine()?.ToLower() == "n")
                                continue;
                        }
                        return;
                    default:
                        Console.WriteLine("Невірний вибір.");
                        break;
                }
            }
        }

        static void CreateAccount()
        {
            Console.WriteLine("Виберіть тип рахунку:");
            Console.WriteLine("1. Звичайний рахунок");
            Console.WriteLine("2. Ощадний рахунок");
            Console.WriteLine("3. Рахунок з кредитним лімітом");

            string type = Console.ReadLine();
            Console.Write("Введіть номер рахунку (наприклад, 123456789): ");
            string number = Console.ReadLine();
            Console.Write("Введіть початковий баланс: ");
            double balance = double.Parse(Console.ReadLine());

            if (type == "1")
                accounts.Add(new BankAccount(number, balance));
            else if (type == "2")
            {
                Console.Write("Введіть відсоткову ставку: ");
                double rate = double.Parse(Console.ReadLine());
                accounts.Add(new SavingsAccount(number, balance, rate));
            }
            else if (type == "3")
            {
                Console.Write("Введіть кредитний ліміт: ");
                double limit = double.Parse(Console.ReadLine());
                accounts.Add(new CheckingAccount(number, balance, limit));
            }
            else
            {
                Console.WriteLine("Невірний вибір.");
                return;
            }

            isSaved = false;
        }

        static void ManageAccount()
        {
            Console.Write("Введіть номер рахунку: ");
            string number = BankAccount.FormatAccountNumber(Console.ReadLine());
            BankAccount account = accounts.FirstOrDefault(a => a.AccountNumber == number);

            if (account == null)
            {
                Console.WriteLine("Рахунок не знайдено.");
                return;
            }

            Console.WriteLine("\nДії з рахунком:");
            Console.WriteLine("1. Поповнити рахунок");
            Console.WriteLine("2. Зняти кошти");
            if (account is SavingsAccount)
                Console.WriteLine("3. Додати відсотки вручну");
            if (account is CheckingAccount)
                Console.WriteLine("4. Встановити кредитний ліміт");

            string choice = Console.ReadLine();

            if (choice == "1")
            {
                Console.Write("Введіть суму поповнення: ");
                account.Deposit(double.Parse(Console.ReadLine()));
            }
            else if (choice == "2")
            {
                Console.Write("Введіть суму зняття: ");
                account.Withdraw(double.Parse(Console.ReadLine()));
            }
            else if (choice == "3" && account is SavingsAccount savings)
                savings.Deposit(0); // Додає проценти вручну
            else if (choice == "4" && account is CheckingAccount checking)
            {
                Console.Write("Введіть новий кредитний ліміт: ");
                checking.CreditLimit = double.Parse(Console.ReadLine());
                Console.WriteLine($"Кредитний ліміт встановлено: {checking.CreditLimit}");
            }
            else
            {
                Console.WriteLine("Невірний вибір.");
            }

            isSaved = false;
        }

        static void DisplayAccounts()
        {
            if (accounts.Count == 0)
                Console.WriteLine("Немає доступних рахунків.");
            else
                accounts.ForEach(account => Console.WriteLine(account));
        }

        static void ManageSaveAccounts()
        {
            Console.WriteLine("\nМеню збереження та керування рахунками:");
            Console.WriteLine("1. Зберегти рахунки");
            Console.WriteLine("2. Видалити рахунок");

            string choice = Console.ReadLine();

            if (choice == "1")
                SaveAccounts();
            else if (choice == "2")
            {
                Console.Write("Введіть номер рахунку для видалення: ");
                string number = BankAccount.FormatAccountNumber(Console.ReadLine());
                BankAccount account = accounts.FirstOrDefault(a => a.AccountNumber == number);

                if (account != null)
                {
                    accounts.Remove(account);
                    Console.WriteLine("Рахунок видалено.");
                    isSaved = false;
                }
                else
                {
                    Console.WriteLine("Рахунок не знайдено.");
                }
            }
            else
                Console.WriteLine("Невірний вибір.");
        }

        static void SaveAccounts()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<BankAccount>));
                using FileStream fs = new FileStream(filePath, FileMode.Create);
                serializer.Serialize(fs, accounts);
                Console.WriteLine("Рахунки збережено.");
                isSaved = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при збереженні рахунків: {ex.Message}");
            }
        }

        static void LoadAccounts()
        {
            if (!File.Exists(filePath)) return;

            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<BankAccount>));
                using FileStream fs = new FileStream(filePath, FileMode.Open);
                accounts = (List<BankAccount>)serializer.Deserialize(fs) ?? new List<BankAccount>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка під час завантаження рахунків: {ex.Message}. Створено новий список.");
                accounts = new List<BankAccount>();
            }
        }
    }
}
