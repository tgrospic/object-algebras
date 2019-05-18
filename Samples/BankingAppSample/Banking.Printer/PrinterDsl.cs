using System;

namespace Banking {
  using Algebras;
  using Algebras.Control;
  using Data.Collection;

  public interface PrinterDsl<F, a> {
    App<F, Unit> Print( a x );
  }

  public static class PrinterExtensions {

    // PrintDsl<F>

    public static (App<F, Unit>, __) Print<F, a, __>( this (F, __) x, a y )
      where __ : PrinterDsl<F, a> => x.useSnd( z => z.Print( y ) );

    // It is possible to write more specific versions of the generic Print function e.g. for string and transaction (tuple) input.

    public static (App<F, Unit>, __) Print<F, __>( this (F, __) x, string name )
      where __ : PrinterDsl<F, string> => x.useSnd( y => y.Print( name ) );

    public static (App<F, Unit>, __) Print<F, __>( this (F, __) x, (int Credit, int Debit, decimal Amount, DateTime Date) tran )
      where __ : PrinterDsl<F, (int Credit, int Debit, decimal Amount, DateTime Date)> => x.useSnd( y => y.Print( tran ) );

    // Extended BankingDsl with new operation for printing, old functions for BankingDsl are still valid.

    public static (App<F, Unit>, __) ShowTransactions<F, __>( this (F, __) x ) where __
      : BankingDsl<F>
      , PrinterDsl<F, (int Credit, int Debit, decimal Amount, DateTime Date)>
      , MonadAlg<F>
      , FoldableAlg<Collection> =>
      from ts in x.BankTransactions()
      from _  in ts.for_( y => x.Print( y ) )
      select _;

  }
}
