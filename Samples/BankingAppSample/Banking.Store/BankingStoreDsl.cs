namespace Banking {
  using Algebras;
  using Algebras.Control;
  using Data.Collection;

  public interface BankingStoreDsl<F> {
    App<F, int> AddAccount( Account account );
    App<F, Account> FindAccount( int accountId );
    App<F, Unit> UpdateAccount( Account account );
    App<F, int> NextAccountId();

    App<F, Unit> AddTransaction( Transaction transaction );
    App<F, App<Collection, Transaction>> Transactions();
  }

  public static class Extensions {

    // BankingStoreDsl<F>, Accounts

    public static (App<F, int>, __) AddAccount<F, __>( this (F, __) x, Account ac )
      where __ : BankingStoreDsl<F> => x.useSnd( y => y.AddAccount( ac ) );

    public static (App<F, Account>, __) FindAccount<F, __>( this (F, __) x, int accountId )
      where __ : BankingStoreDsl<F> => x.useSnd( y => y.FindAccount( accountId ) );

    public static (App<F, Unit>, __) UpdateAccount<F, __>( this (F, __) x, Account account )
      where __ : BankingStoreDsl<F> => x.useSnd( y => y.UpdateAccount( account ) );

    public static (App<F, int>, __) NextAccountId<F, __>( this (F, __) x )
      where __ : BankingStoreDsl<F> => x.useSnd( y => y.NextAccountId() );

    // BankingStoreDsl<F>, Transactions

    public static (App<F, Unit>, __) AddTransaction<F, __>( this (F, __) b, Transaction transaction )
      where __ : BankingStoreDsl<F> => b.useSnd( y => y.AddTransaction( transaction ) );

    public static (App<F, (App<Collection, Transaction>, __)>, __) Transactions<F, __>( this (F, __) x )
      where __ : BankingStoreDsl<F>, FunctorAlg<F> =>
        x.useSnd( y => y.Transactions() ).map( y => y.pair( x.Item2 ) );

    // BankingStoreDsl extensions - functions built from existing BankingStoreDsl functions.

    /* This is expression on DSL level. It depends only on interpreted language.
     * It also uses higher-order effect (monad `bind`) to sequence the computation which allows
     * LINQ syntax for all instances of `BindAlg<F>`.
     *
     * `NextAccountId` is an effect just like `bind` but they serve different purpose in the computation.
     */
    public static (App<F, Account>, __) AddAccount<F, __>( this (F, __) x, string name )
      where __ : BankingStoreDsl<F>, BindAlg<F> =>
        from accountId in x.NextAccountId()
        let account     = new Account( accountId, name, 0 )
        from _         in x.AddAccount( account )
        select account;

    public static (App<F, decimal>, __) Balance<F, __>( this (F, __) x, int accountId )
      // Here LINQ expression is a Functor only, it's visible from the signature.
      where __ : BankingStoreDsl<F>, FunctorAlg<F> =>
        from account in x.FindAccount( accountId )
        select account.Balance;

  }
}
