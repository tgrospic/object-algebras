using System;

namespace BankingAppSample {
  using Algebras;
  using Algebras.Control;
  using Banking;
  using Data.Collection;
  using Data.Collection.Impl;
  using Data.Either;
  using Data.Either.Impl;

  public static class BankStoreCombinators {

    public static (App<F, decimal>, __) Transfer<F, __>( this (F, __) x, decimal amount, int from, int to ) where __
      : BankingStoreDsl<F>
      , BindAlg<F> =>
      // Find accounts with applicative, implementation can be done in parallel.
      from acc in x.FindAccount( @from ).zip( x.FindAccount( to ) )
      let debAc = acc.Item1
      let crdAc = acc.Item2
      from _1  in x.UpdateAccount( debAc.Debit( amount ) )
      from _2  in x.UpdateAccount( crdAc.Credit( amount ) )
      from _3  in x.AddTransaction( new Transaction( @from, to, amount, DateTime.UtcNow ) )
      select amount;
  }

  public class BankImpl
    // Banking effects (dependencies)
    : BankingDsl<Either<string>>
    , BankingStoreDsl<Either<string>>
    , BankPrinterDsl<Either<string>>
    // Either - effects handler
    , EitherAlg<Either<string>, string>
    // Either - higher-order effects
    , MonadAlg<Either<string>>
    , NatAlg<Either<string>, Collection>
    // Collection - higher-order effects
    , FunctorAlg<Collection>
    , FoldableAlg<Collection> {

    readonly FunctorAlg<Collection> _collFunctor = FunctorCollection.Instance;
    readonly FoldableAlg<Collection> _collFold   = FoldableCollection.Instance;

    readonly MonadAlg<Either<string>>             _eitherMonad = MonadEither<string>.Instance;
    readonly NatAlg<Either<string>, Collection> _eitherCollNat = NatEitherCollection<string>.Instance;

    readonly EitherAlg<Either<string>, string> _either = EitherImpl<string>.Instance;

    readonly (Either<string>, BankImpl)  __;
    readonly BankingStoreDsl<Either<string>> _store;
    readonly BankPrinterDsl<Either<string>>  _printer;
    
    readonly int DepositedAccountId  = 1;
    readonly int WithdrawalAccountId = 2;

    // BankInstance depends on other DSL's
    public BankImpl( BankingStoreDsl<Either<string>> store, BankPrinterDsl<Either<string>> printer ) {
      _store = store;
      _printer = printer;

      // We are inside interpreter, it's useful to have lifted context. Later we will see how it's used.
      // _default()_ is a convenient way to represent instance of higher-kinded type which doesn't have concrete value.
      __ = (default(Either<string>), this);

      // Create initial bank accounts
      __.AddAccount( new Account( DepositedAccountId, "Deposits", 100_000 ) );
      __.AddAccount( new Account( WithdrawalAccountId, "Withdrawals", 0 ) );
    }

    /*
     * Here we use _higher-order facilities_ from interpreted language so we need to _reify_ the result.
     *
     * _Higher-order_ have a meaning here as presented by Oleg Kiselyov in http://okmij.org/ftp/Computation/having-effect.html.
     *
     *   > We argue that the central problem of the interaction of higher-order programming with various kinds of effects can be
     *   tackled by eliminating the distinction: higher-order facility is itself an effect, not too different from state effect.
     *
     * For example `__.AddAccount( name ).map( ac => ac.Id ).Item1` will
     * - create a new account (update state in the context)
     * - `map` over newly created account to get Id (higher-order effect)
     * - get the value from the value/context tuple (reify interpreted value)
     *
     * Another higher-order effect can be to change the type of the container. In this implementation BankingStoreDsl and BankingDsl
     * use the same container type `App<Either<string>, a>` so reified value can be returned directly.
     *
     * `NatAlg<C, D>` can be used as natural transformation `C ~> D`, where C and D represents higher-kinded types.
     *   e.g. `Either<e> ~> Collection` or `Maybe ~> Either<e>`
     */

    // BankingDsl
    App<Either<string>, int> BankingDsl<Either<string>>.CreateAccount( string name ) =>
      __.AddAccount( name ).map( ac => ac.Id ).Item1;
    App<Either<string>, (int Id, string Name, decimal Balance)> BankingDsl<Either<string>>.AccountDetails( int accountId ) =>
      __.FindAccount( accountId ).map( x => (x.Id, x.Name, x.Balance) ).Item1;
    App<Either<string>, (int Id, string Name, decimal Balance)> BankingDsl<Either<string>>.DepositAccountDetails() =>
      __.AccountDetails( DepositedAccountId ).Item1;
    App<Either<string>, (int Id, string Name, decimal Balance)> BankingDsl<Either<string>>.WithdrawalAccountDetails() =>
      __.AccountDetails( WithdrawalAccountId ).Item1;
    App<Either<string>, decimal> BankingDsl<Either<string>>.Balance( int accountId ) =>
      __.Balance( accountId ).Item1;
    App<Either<string>, decimal> BankingDsl<Either<string>>.Deposit( decimal amount, int to ) =>
      __.Transfer( amount, DepositedAccountId, to ).Item1;
    App<Either<string>, decimal> BankingDsl<Either<string>>.Withdraw( decimal amount, int from ) =>
      __.Transfer( amount, from, WithdrawalAccountId ).Item1;
    App<Either<string>, App<Collection, (int Credit, int Debit, decimal Amount, DateTime Date)>> BankingDsl<Either<string>>.BankTransactions() =>
      __.Transactions().map( ts => ts.map( t => (t.Credit, t.Debit, t.Amount, t.Date) ).Item1 ).Item1;

    /*
     * The implementation of other methods is mechanical and can be (derived) generated from input dependencies.
     */

    // BankingStoreDsl
    App<Either<string>, int> BankingStoreDsl<Either<string>>.AddAccount( Account account )              => _store.AddAccount( account );
    App<Either<string>, Account> BankingStoreDsl<Either<string>>.FindAccount( int accountId )           => _store.FindAccount( accountId );
    App<Either<string>, Unit> BankingStoreDsl<Either<string>>.UpdateAccount( Account account )          => _store.UpdateAccount( account );
    App<Either<string>, int> BankingStoreDsl<Either<string>>.NextAccountId()                            => _store.NextAccountId();
    App<Either<string>, Unit> BankingStoreDsl<Either<string>>.AddTransaction( Transaction transaction ) => _store.AddTransaction( transaction );
    App<Either<string>, App<Collection, Transaction>> BankingStoreDsl<Either<string>>.Transactions()    => _store.Transactions();

    // Printer
    App<Either<string>, Unit> PrinterDsl<Either<string>, (int Credit, int Debit, decimal Amount, DateTime Date)>
      .Print( (int Credit, int Debit, decimal Amount, DateTime Date) x )           => _printer.Print( x );
    App<Either<string>, Unit> PrinterDsl<Either<string>, string>.Print( string x ) => _printer.Print( x );

    // Either
    App<Either<string>, a> EitherAlg<Either<string>, string>.left<a>( string ee )                                             => _either.left<a>( ee );
    App<Either<string>, a> EitherAlg<Either<string>, string>.right<a>( a aa )                                                 => _either.right( aa );
    b EitherAlg<Either<string>, string>.either<a, b>( App<Either<string>, a> ea, Func<string, b> onLeft, Func<a, b> onRight ) => _either.either( ea, onLeft, onRight );

    // Collection API
    Func<App<Collection, a>, App<Collection, b>> FunctorAlg<Collection>.map<a, b>( Func<a, b> f ) => _collFunctor.map( f );
    Func<b, Func<App<Collection, a>, b>> FoldableAlg<Collection>.foldl<a, b>( Func<b, a, b> f )   => _collFold.foldl( f );
    Func<b, Func<App<Collection, a>, b>> FoldableAlg<Collection>.foldr<a, b>( Func<a, b, b> f )   => _collFold.foldr( f );

    // Monadic API
    Func<App<Either<string>, a>, App<Either<string>, b>> FunctorAlg<Either<string>>.map<a, b>( Func<a, b> f )                      => _eitherMonad.map( f );
    Func<App<Either<string>, a>, App<Either<string>, b>> ApplyAlg<Either<string>>.apply<a, b>( App<Either<string>, Func<a, b>> f ) => _eitherMonad.apply( f );
    App<Either<string>, a> ApplicativeAlg<Either<string>>.pure<a>( a x )                                                           => _eitherMonad.pure( x );
    Func<Func<a, App<Either<string>, b>>, App<Either<string>, b>> BindAlg<Either<string>>.bind<a, b>( App<Either<string>, a> x )   => _eitherMonad.bind<a, b>( x );

    // Nat : Either<e> ~> Collection
    App<Collection, a> NatAlg<Either<string>, Collection>.transform<a>( App<Either<string>, a> ea ) => _eitherCollNat.transform( ea );
  }
}
