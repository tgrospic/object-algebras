using System;

namespace BankingAppSample {
  using Algebras;
  using Banking;
  using Data.Either;
  using Data.Either.Impl;

  public interface BankPrinterDsl<F>
    : PrinterDsl<F, (int Credit, int Debit, decimal Amount, DateTime Date)>
    , PrinterDsl<F, string> { }

  public class BankPrinterImpl : BankPrinterDsl<Either<string>> {

    protected readonly EitherAlg<Either<string>, string> _ea = EitherImpl<string>.Instance;

    App<Either<string>, Unit> unit() => _ea.right( Unit.Val );

    public App<Either<string>, Unit> Print( string x ) {
      Console.WriteLine( $"PRINTER: {x}" );
      return unit();
    }

    public App<Either<string>, Unit> Print( (int Credit, int Debit, decimal Amount, DateTime Date) x ) {
      Console.WriteLine( $"PRINTER: Transaction: {x.Date} {x.Debit}->{x.Credit} ${x.Amount}" );
      return unit();
    }

  }
}
