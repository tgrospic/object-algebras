using System;

namespace Banking {

  /// <summary>
  /// Represents a transaction. Which is the debitting of monies
  /// from one account and the crediting of another.
  /// </summary>
  public class Transaction {
    public readonly int Debit;
    public readonly int Credit;
    public readonly decimal Amount;
    public readonly DateTime Date;

    public Transaction( int debit, int credit, decimal amount, DateTime date ) {
      Credit = credit;
      Debit = debit;
      Amount = amount;
      Date = date;
    }
  }

  /// <summary>
  /// Individual account in a bank
  /// </summary>
  public class Account {
    public readonly int Id;
    public readonly string Name;
    public readonly decimal Balance;

    public Account( int id, string name, decimal balance ) {
      Id = id;
      Name = name;
      Balance = balance;
    }

    public Account Credit( decimal amount ) =>
      new Account( Id, Name, Balance + amount );

    public Account Debit( decimal amount ) =>
      new Account( Id, Name, Balance - amount );
  }
}
