using System.Collections.Concurrent;
using System.Threading;

var account = new BankAccount(2_000m);
var messages = new ConcurrentQueue<string>();
var withdrawals = new[] { 300m, 300m, 300m };

using var startGate = new ManualResetEventSlim(false);
var atms = Enumerable.Range(1, 4)
    .Select(atmNumber => Task.Factory.StartNew(
        () => UseAtm(atmNumber, withdrawals, account, startGate, messages),
        TaskCreationOptions.LongRunning))
    .ToArray();

Console.WriteLine($"Starting balance: {account.Balance}");
startGate.Set();

await Task.WhenAll(atms);

foreach (var message in messages)
    Console.WriteLine(message);

Console.WriteLine($"Final balance: {account.Balance}");

static void UseAtm(
    int atmNumber,
    IEnumerable<decimal> withdrawals,
    BankAccount account,
    ManualResetEventSlim startGate,
    ConcurrentQueue<string> messages)
{
    startGate.Wait();

    foreach (var amount in withdrawals)
    {
        var success = account.Withdraw(amount);
        messages.Enqueue(success
            ? $"ATM {atmNumber}: ${amount} withdrawn."
            : $"ATM {atmNumber}: insufficient funds.");
    }
}

internal sealed class BankAccount
{
    private readonly object _lock = new();
    private decimal _balance;

    public BankAccount(decimal balance) => _balance = balance;

    public decimal Balance
    {
        get
        {
            lock (_lock)
                return _balance;
        }
    }

    public bool Withdraw(decimal amount)
    {
        Monitor.Enter(_lock);
        try
        {
            if (_balance < amount)
                return false;

            _balance -= amount;
            return true;
        }
        finally
        {
            Monitor.Exit(_lock);
        }
    }
}
