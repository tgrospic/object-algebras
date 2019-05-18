using System;

namespace Data.Either {
  using Algebras;
  using Algebras.Control;
  using Data.Collection;
  using static Algebras.Static;

  // Either lightweight HKT
  public interface Either<e> { }

  public interface EitherAlg<F, e> {
    // Introduction
    App<F, a> left<a>( e ee );
    App<F, a> right<a>( a aa );
    // Elimination
    // TODO: App<G, b> more general?
    b either<a, b>( App<F, a> ea, Func<e, b> onLeft, Func<a, b> onRight );
  }

  public static class Extensions {

    // left
    public static (App<F, a>, __) left<F, e, a, __>( this (F, __) ea, e ee )
      where __ : EitherAlg<F, e> => ea.useSnd( x => x.left<a>( ee ) );

    public static (App<F, a>, __) left<F, e, a, __>( this __ ea, e ee )
      where __ : EitherAlg<F, e> => (default( F ), ea).left<F, e, a, __>( ee );

    public static Func<e, (App<F, a>, __)> left<F, e, a, __>( this __ ea )
      where __ : EitherAlg<F, e> => ee => (default( F ), ea).left<F, e, a, __>( ee );

    // right
    public static (App<F, a>, __) right<F, e, a, __>( this (F, __) ea, a aa )
      where __ : EitherAlg<F, e> => ea.useSnd( x => x.right( aa ) );

    public static (App<F, a>, __) right<F, e, a, __>( this __ ea, a aa )
      where __ : EitherAlg<F, e> => (default( F ), ea).right<F, e, a, __>( aa );

    public static Func<a, (App<F, a>, __)> right<F, e, a, __>( this __ ea )
      where __ : EitherAlg<F, e> => aa => (default( F ), ea).right<F, e, a, __>( aa );

    // either
    public static b either<F, e, a, b, __>( this (App<F, a> x, __ ctx) ea, Func<e, b> onLeft, Func<a, b> onRight )
      where __ : EitherAlg<F, e> => ea.ctx.either( ea.x, onLeft, onRight );

    public static Unit either<F, e, a, __>( this (App<F, a>, __) ea, Action<e> onLeft, Action<a> onRight )
      where __ : EitherAlg<F, e> => ea.either( onLeft.fnUnit(), onRight.fnUnit() );

    // TODO: define Bifunctor interface
    public static (App<F, b>, __) mapBoth<F, e, f, a, b, __>( this (App<F, a>, __) ea, Func<e, f> onLeft, Func<a, b> onRight )
      where __ : EitherAlg<F, e>, EitherAlg<F, f> =>
        ea.either(
          pipe( onLeft, ea.Item2.left<F, f, b, __>() ),
          pipe( onRight, ea.Item2.right<F, f, b, __>() )
        );

    public static (App<F, Unit>, __) mapBoth<F, e, a, __>( this (App<F, a>, __) ea, Action<e> onLeft, Action<a> onRight )
      where __ : EitherAlg<F, e>, EitherAlg<F, Unit> =>
        ea.mapBoth( onLeft.fnUnit(), onRight.fnUnit() );

    // either - more specific versions to help the compiler
    public static b either<e, a, b, __>( this (App<Either<e>, a>, __) ea, Func<e, b> onLeft, Func<a, b> onRight )
      where __ : EitherAlg<Either<e>, e> => ea.either<Either<e>, e, a, b, __>( onLeft, onRight );

    public static Unit either<e, a, __>( this (App<Either<e>, a>, __) ea, Action<e> onLeft, Action<a> onRight )
      where __ : EitherAlg<Either<e>, e> => ea.either( onLeft.fnUnit(), onRight.fnUnit() );

    // Nat : Either<e> ~> Collection
    public static (App<Collection, a>, __) ToCollection<e, a, __>( this (App<Either<e>, a>, __) ea )
      where __ : NatAlg<Either<e>, Collection> => ea.useSnd( x => x.transform( ea.Item1 ) );

  }
}
