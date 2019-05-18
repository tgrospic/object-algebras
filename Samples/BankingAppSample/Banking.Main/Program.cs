using System;

namespace BankingAppSample {
  using Algebras;
  using Algebras.Control;
  using Banking;
  using Data.Collection;
  using Data.Either;

  class Program {
    static void Main( string[] args ) {

      /*
       * This Banking sample app is a re-implementation of the sample from very nice functional C# library **language-ext** by Paul Louth!
       * https://github.com/louthy/language-ext/tree/master/Samples/BankingAppSample
       *
       * His implementation uses Free Monad to capture effects. Here effects are algebras which constraint or define
       * interaction with the _context_ or interpreter (implementation).
       */

      /// BankStore effects <see cref="BankingStoreDsl{F}"/>
      var bankStore = new BankStoreImpl();

      /// BankPrinter effects <see cref="BankPrinterDsl{F}"/>
      var printer = new BankPrinterImpl();

      /// Banking effects <see cref="BankingDsl{F}"/>, depends on storage and printer effects
      var bank = (default( Either<string> ), new BankImpl( bankStore, printer ));

      // Run operations (side-effects), interpret BankingDemo DSL expression
      var result = bank.BankingDemo();

      // Run final result - either with an error or computed value
      result.either(
        err => Console.WriteLine( $"ERROR: {err}" ),
        val => Console.WriteLine( $"RESULT: Created Account ID: {val}" )
      );

      // Log from BankStore implementation
      Console.WriteLine( $"\nBank Store operations:\n{bankStore.Log}" );

    }
  }

  public static class BankDslCombinators {

    /* 
     * > These functions demonstrates that once you have captured all of the
     * actions that represent an interaction with the 'world' (i.e IO,
     * databases, global state, etc.) then you can compose those actions
     * without having to add new functions to the `BankingDsl` DSL.
     * 
     * To extend BankingDsl means to define new algebra with new actions.
     * This is exactly how printer is defined.
     */

    public static (App<F, int>, __) BankingDemo<F, __>( this (F, __) b ) where __
      : BankingDsl<F>
      , BankPrinterDsl<F>
      , MonadAlg<F>
      , FoldableAlg<Collection> =>
      from accountId in b.CreateAccount( name: "Paul" )
      from _1        in b.ShowBalance( accountId )
      from amount1   in b.Deposit( 100m, accountId )
      from _2        in b.ShowBalance( accountId )
      from amount2   in b.Withdraw( 75m, accountId )
      from _3        in b.ShowBalance( accountId )
      from _4        in b.ShowTransactions()
      select accountId;

    public static (App<F, Unit>, __) ShowBalance<F, __>( this (F, __) b, int id ) where __
      : BankingDsl<F>
      , BankPrinterDsl<F>
      , BindAlg<F> =>
      from ac in b.AccountDetails( id )
      from ba in b.BalanceBank( id )
      from _1 in b.Print( $"Balance of account {ac.Name} is: ${ba}" )
      from wa in b.WithdrawalAccountDetails()
      from _2 in b.Print( $"\tBalance of {wa.Name} is: ${wa.Balance}" )
      from da in b.DepositAccountDetails()
      from _3 in b.Print( $"\tBalance of {da.Name} is: ${da.Balance}" )
      select Unit.Val;

  }
}
