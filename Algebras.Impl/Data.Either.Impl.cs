using System;

namespace Data.Either.Impl {
  using Algebras;
  using Algebras.Control;
  using Data.Collection;
  using Data.Collection.Impl;
  using static Algebras.Static;

  public class EitherImpl<e> : EitherAlg<Either<e>, e> {

    interface EitherC<a> { }

    abstract class Base<v, a> : EitherC<a> {
      public v Val; string lbl;
      protected Base( v value, string label ) { Val = value; lbl = label; }
      public override string ToString() => $"{lbl}: {Val}";
    }

    class Left<a> : Base<e, a> {
      public Left( e x ) : base( x, "LEFT" ) { }
    }

    class Right<a> : Base<a, a> {
      public Right( a x ) : base( x, "RIGHT" ) { }
    }

    class EitherHKT<a> : NewType1<EitherC<a>, Either<e>, a> { }

    static App<Either<e>, a> inj<a>( EitherC<a> x ) => EitherHKT<a>.I.Inj( x );
    static EitherC<a> prj<a>( App<Either<e>, a> x ) => EitherHKT<a>.I.Prj( x );

    // Introduction
    public App<Either<e>, a> left<a>( e ss ) => inj( new Left<a>( ss ) );
    public App<Either<e>, a> right<a>( a aa ) => inj( new Right<a>( aa ) );

    // Elimination
    public b either<a, b>( App<Either<e>, a> ea, Func<e, b> onLeft, Func<a, b> onRight ) {
      switch ( prj( ea ) ) {
        case Left<a> l: return onLeft( l.Val );
        case Right<a> r: return onRight( r.Val );
        default: throw new Exception( $"Unknown Either type variance {ea.GetType().FullName}" );
      }
    }

    public static EitherAlg<Either<e>, e> Instance = new EitherImpl<e>();
  }

  // Functor
  public class FunctorEither<e> : FunctorAlg<Either<e>> {
    EitherAlg<Either<e>, e> ei = EitherImpl<e>.Instance;
    public Func<App<Either<e>, a>, App<Either<e>, b>> map<a, b>( Func<a, b> f ) =>
      x => x.lifted( ei, y => y.mapBoth<Either<e>, e, e, a, b, EitherAlg<Either<e>, e>>( identity, f ) );

    public static FunctorAlg<Either<e>> Instance = new FunctorEither<e>();
  }

  // Apply : Functor
  public class ApplyEither<e> : FunctorEither<e>, ApplyAlg<Either<e>> {
    EitherAlg<Either<e>, e> ei = EitherImpl<e>.Instance;
    public Func<App<Either<e>, a>, App<Either<e>, b>> apply<a, b>( App<Either<e>, Func<a, b>> f ) => x =>
      (f, ei).either<Either<e>, e, Func<a, b>, App<Either<e>, b>, EitherAlg<Either<e>, e>>(
        ei.left<b>,
        ff => map( ff )( x )
      );

    public static new ApplyAlg<Either<e>> Instance = new ApplyEither<e>();
  }

  // Applicative : Apply
  public class ApplicativeEither<e> : ApplyEither<e>, ApplicativeAlg<Either<e>> {
    EitherAlg<Either<e>, e> ei = EitherImpl<e>.Instance;
    public App<Either<e>, a> pure<a>( a x ) => ei.right( x );

    public static new ApplicativeAlg<Either<e>> Instance = new ApplicativeEither<e>();
  }

  // Bind : Apply
  public class BindEither<e> : ApplyEither<e>, BindAlg<Either<e>> {
    EitherAlg<Either<e>, e> ei = EitherImpl<e>.Instance;
    public Func<Func<a, App<Either<e>, b>>, App<Either<e>, b>> bind<a, b>( App<Either<e>, a> x ) => f =>
      (x, ei).either<Either<e>, e, a, App<Either<e>, b>, EitherAlg<Either<e>, e>>( ei.left<b>, f );

    public static new BindAlg<Either<e>> Instance = new BindEither<e>();
  }

  // Monad : Applicative, Bind
  public class MonadEither<e> : BindEither<e>, MonadAlg<Either<e>> {
    public App<Either<e>, a> pure<a>( a x ) => ApplicativeEither<e>.Instance.pure( x );

    public static new MonadAlg<Either<e>> Instance = new MonadEither<e>();
  }

  // Foldable
  public class FoldableEither<e> : FoldableAlg<Either<e>> {
    EitherAlg<Either<e>, e> ei = EitherImpl<e>.Instance;
    public Func<b, Func<App<Either<e>, a>, b>> foldl<a, b>( Func<b, a, b> f ) =>
      init => ea => ei.either( ea, _ => init, x => f( init, x ) );
    public Func<b, Func<App<Either<e>, a>, b>> foldr<a, b>( Func<a, b, b> f ) =>
      init => ea => ei.either( ea, _ => init, x => f( x, init ) );

    public static FoldableAlg<Either<e>> Instance = new FoldableEither<e>();
  }

  // Traversable
  public class TraversableEither<e, M> : TraversableAlg<Either<e>, M> {
    EitherAlg<Either<e>, e> ei = EitherImpl<e>.Instance;
    ApplicativeAlg<M>      apM;
    public TraversableEither( ApplicativeAlg<M> x ) { apM = x; }
    public Func<App<Either<e>, a>, App<M, App<Either<e>, b>>> traverse<a, b>( Func<a, App<M, b>> f ) => x =>
      ei.either( x,
        lt => apM.pure( ei.left<b>( lt ) ),
        rt => f( rt ).lifted( apM, y => y.map( ei.right ) ) );

    public App<M, App<Either<e>, a>> sequence<a>( App<Either<e>, App<M, a>> x ) => traverse<App<M, a>, a>( identity )( x );
  }

  // Either<e> ~> Collection
  public class NatEitherCollection<e> : NatAlg<Either<e>, Collection> {
    FoldableAlg<Either<e>>    foldEi = FoldableEither<e>.Instance;
    AlternativeAlg<Collection> altCo = AlternativeCollection.Instance;
    protected NatEitherCollection() { }
    public App<Collection, a> transform<a>( App<Either<e>, a> ea ) =>
      ea.pair( foldEi ).foldl( altCo.empty<a>() )( ( acc, x ) => altCo.pure( x ) );

    public static NatAlg<Either<e>, Collection> Instance = new NatEitherCollection<e>();
  }

}
