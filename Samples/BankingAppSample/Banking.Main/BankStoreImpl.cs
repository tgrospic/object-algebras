using System.Collections.Generic;
using System.Text;

namespace BankingAppSample {
  using Algebras;
  using Banking;
  using Data.Collection;
  using Data.Collection.Impl;
  using Data.Either;
  using Data.Either.Impl;

  public class BankStoreImpl : BankingStoreDsl<Either<string>> {

    // This is our sample storage to save transactions, accounts and logs.
    IList<Transaction> transactions = new List<Transaction>();
    IDictionary<int, Account> accounts = new Dictionary<int, Account>();
    StringBuilder activityLog = new StringBuilder();

    EitherAlg<Either<string>, string> _ea = EitherImpl<string>.Instance;

    App<Either<string>, a> value<a>( a x )        => _ea.right( x );
    App<Either<string>, a> error<a>( string err ) => _ea.left<a>( err );

    public string Log => activityLog.ToString();

    public App<Either<string>, int> AddAccount( Account ac ) {
      if ( !accounts.ContainsKey( ac.Id ) ) {
        activityLog.AppendLine( $"AddAccount: ID {ac.Id}, Name {ac.Name}, Balance {ac.Balance}" );
        accounts.Add( ac.Id, ac );
        return value( ac.Id );
      } else {
        return error<int>( $"Account with ID ${ac.Id} already exist." );
      }
    }

    public App<Either<string>, Account> FindAccount( int accountId ) {
      activityLog.AppendLine( $"FindAccount: ID {accountId}" );
      if ( accounts.ContainsKey( accountId ) )
        return value( accounts[accountId] );
      else
        return error<Account>( $"Account ID {accountId} doesn't exist" );
    }

    public App<Either<string>, Unit> UpdateAccount( Account ac ) {
      activityLog.AppendLine( $"UpdateAccount: ID {ac.Id}, Name {ac.Name}, Balance {ac.Balance}" );
      if ( accounts.ContainsKey( ac.Id ) ) {
        accounts[ac.Id] = ac;
        return value( Unit.Val );
      } else
        return error<Unit>( $"Account ID {ac.Id} doesn't exist" );
    }

    public App<Either<string>, int> NextAccountId() {
      var nextId = accounts.Count + 1;
      activityLog.AppendLine( $"NextAccountId: {nextId}" );
      return value( nextId );
    }

    public App<Either<string>, Unit> AddTransaction( Transaction tr ) {
      activityLog.AppendLine( $"AddTransaction: {tr.Date} {tr.Debit}->{tr.Credit} ${tr.Amount}" );
      transactions.Add( tr );
      return value( Unit.Val );
    }

    public App<Either<string>, App<Collection, Transaction>> Transactions() {
      activityLog.AppendLine( $"Transactions: - count {transactions.Count}" );
      return value( transactions.Inj() );
    }

  }
}
