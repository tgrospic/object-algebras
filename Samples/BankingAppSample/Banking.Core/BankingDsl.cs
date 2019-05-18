using System;

namespace Banking {
  using Algebras;
  using Algebras.Control;
  using Data.Collection;

  public interface BankingDsl<F> {
    App<F, int> CreateAccount( string name );
    App<F, (int Id, string Name, decimal Balance)> AccountDetails( int accountId );
    App<F, decimal> Withdraw( decimal amount, int from );
    App<F, decimal> Deposit( decimal amount, int to );
    App<F, decimal> Balance( int accountId );
    App<F, (int Id, string Name, decimal Balance)> DepositAccountDetails();
    App<F, (int Id, string Name, decimal Balance)> WithdrawalAccountDetails();
    App<F, App<Collection, (int Credit, int Debit, decimal Amount, DateTime Date)>> BankTransactions();
  }

  public static class Extensions {

    // BankingDsl<F>

    public static (App<F, int>, __) CreateAccount<F, __>( this (F, __) x, string name )
      where __ : BankingDsl<F> => x.useSnd( y => y.CreateAccount( name ) );

    public static (App<F, (int Id, string Name, decimal Balance)>, __) AccountDetails<F, __>( this (F, __) x, int accountId )
      where __ : BankingDsl<F> => x.useSnd( y => y.AccountDetails( accountId ) );

    public static (App<F, (int Id, string Name, decimal Balance)>, __) WithdrawalAccountDetails<F, __>( this (F, __) x )
      where __ : BankingDsl<F> => x.useSnd( y => y.WithdrawalAccountDetails() );

    public static (App<F, (int Id, string Name, decimal Balance)>, __) DepositAccountDetails<F, __>( this (F, __) x )
      where __ : BankingDsl<F> => x.useSnd( y => y.DepositAccountDetails() );

    public static (App<F, decimal>, __) Deposit<F, __>( this (F, __) x, decimal amount, int to )
      where __ : BankingDsl<F> => x.useSnd( y => y.Deposit( amount, to ) );

    public static (App<F, decimal>, __) Withdraw<F, __>( this (F, __) x, decimal amount, int from )
      where __ : BankingDsl<F> => x.useSnd( y => y.Withdraw( amount, from ) );

    public static (App<F, decimal>, __) BalanceBank<F, __>( this (F, __) x, int accountId )
      where __ : BankingDsl<F> => x.useSnd( y => y.Balance( accountId ) );

    public static (App<F, (App<Collection, (int Credit, int Debit, decimal Amount, DateTime Date)>, __)>, __)
        BankTransactions<F, __>( this (F, __) x )
      where __ : BankingDsl<F>, FunctorAlg<F> =>
        /*
         * This is the place where we need to lift inner computation (Collection) to DSL level.
         * This means to send current interpreter context (__) to inner computation (in tuple with App<Collection, ...>).
         *
         * This lifting can be done by C# compiler mechanically lifting all `App` values with the current context `__`.
         */
        x.useSnd( y => y.BankTransactions() ).map( y => y.pair( x.Item2 ) );

  }
}
