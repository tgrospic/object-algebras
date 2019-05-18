using System;

namespace Data.State {
  using Algebras;
  using Algebras.Control;
  using Data.Either;

  // State lightweight HKT (represents a Brand `Type -> Type`)
  public interface State<s> { }

  // class StateAlg f g s where
  //   makeState :: (s -> (g a, s)) -> f a
  //   runState  :: f a -> s -> (g a, s)
  public interface StateAlg<F, G, s> {
    // Introduction
    App<F, a> makeState<a>( Func<s, (App<G, a>, s)> f );
    // Elimination
    (App<G, a>, s) runState<a>( App<F, a> p, s ss );
  }

  public static class Extensions {

    public static (App<F, a>, __) makeState<F, G, a, s, __>( this (Func<s, (App<G, a>, s)>, __) f )
      where __ : StateAlg<F, G, s> => f.useSnd( x => x.makeState( f.Item1 ) );

    public static ((App<G, a>, __), s) runState<F, G, a, s, __>( this (App<F, a>, __) p, s ss )
      where __ : StateAlg<F, G, s> => p.Item2.runState( p.Item1, ss ).mapFst( x => (x, p.Item2) );

    // More specific version to help compiler infer `G`
    public static (App<F, a>, __) makeState<F, a, s, __>( this (F, __) impl, Func<s, (App<Either<string>, a>, s)> f )
      where __ : StateAlg<F, Either<string>, s> => impl.useSnd( x => x.makeState( f ) );

    public static ((App<Either<string>, a>, __), s) runState<F, a, s, __>( this (App<F, a>, __) p, s ss )
      where __ : StateAlg<F, Either<string>, s> => p.Item2.runState( p.Item1, ss ).mapFst( x => (x, p.Item2) );

    // get
    public static (App<F, s>, __) get<F, G, s, __>( this (F, __) impl )
      where __ : StateAlg<F, G, s>, ApplicativeAlg<G> =>
        impl.useSnd( x => x.makeState( ss => ((default( G ), impl.Item2).pure( ss ).Item1, ss) ) );

    // gets
    public static (App<F, a>, __) gets<F, G, s, a, __>( this (F, __) impl, Func<s, a> f )
      where __ : StateAlg<F, G, s>, ApplicativeAlg<G> =>
        impl.useSnd( x => x.makeState( ss => ((default( G ), impl.Item2).pure( f( ss ) ).Item1, ss) ) );

    // put
    public static (App<F, Unit>, __) put<F, G, s, __>( this (F, __) impl, s ss )
      where __ : StateAlg<F, G, s>, ApplicativeAlg<G> =>
        impl.useSnd( x => x.makeState( _ => ((default( G ), impl.Item2).pure( Unit.Val ).Item1, ss) ) );

    // modify
    public static (App<F, Unit>, __) modify<F, G, s, __>( this (F, __) impl, Func<s, s> f )
      where __ : StateAlg<F, G, s>, ApplicativeAlg<G> =>
        impl.useSnd( x => x.makeState( ss => ((default( G ), impl.Item2).pure( Unit.Val ).Item1, f( ss )) ) );

  }
}
