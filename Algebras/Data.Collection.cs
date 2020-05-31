using System;
using System.Collections.Generic;
using System.Linq;

namespace Data.Collection {
  using Algebras;
  using Algebras.Control;

  // Lightweight HKT
  public interface Collection { }

  public interface CollectionAlg<F> {
    // Introduction
    App<F, a> nil<a>();
    App<F, a> cons<a>( a head, App<F, a> tail );
    App<F, a> list<a>( IEnumerable<a> xs );
    // Elimination
    IEnumerable<a> enumerable<a>( App<F, a> c );
  }

  public static class Extensions {

    public static Func<a, Func<(App<Collection, a>, __), (App<Collection, a>, __)>> cons<__, a>( this __ ctx )
      where __ : CollectionAlg<Collection>  => x => xs => ctx.cons( x, xs.Item1 ).pair( xs.Item2 );

    public static Func<(App<Collection, a>, __), (App<Collection, a>, __)> cons<__, a>( this __ ctx, a x )
      where __ : CollectionAlg<Collection> => xs => ctx.cons( x, xs.Item1 ).pair( xs.Item2 );

    public static (App<Collection, a>, __) list<__, a>( this IEnumerable<a> xs, __ ctx )
      where __ : CollectionAlg<Collection> => ctx.list( xs ).pair( ctx );

    public static (App<Collection, a>, __) replicate<__, a>( this __ ctx, a x, int i )
      where __ : CollectionAlg<Collection> => ctx.list( Enumerable.Empty<a>() ).pair( ctx );

    // many
    public static (App<F, (App<Collection, a>, __)>, __) many<F, a, __>( this (App<F, a>, __ __) x )
      where __ : MonadAlg<F>, AltAlg<F>, CollectionAlg<Collection> =>
      x.bind( h => x.many().map( x.__.cons( h ) ) ).or( x.__.pure( x.__.nil<a>().pair( x.__ ) ).pair( x.__ ) );

    public static (App<F, (App<Collection, a>, __)>, __) many1<F, a, __>( this (App<F, a>, __ __) x )
      where __ : MonadAlg<F>, AltAlg<F>, CollectionAlg<Collection> =>
      x.map( x.__.cons<__, a>() ).apply( x.many() );

    public static (App<F, (App<Collection, a>, __)>, __) sepBy<F, a, sep, __>( this (App<F, a>, __ __) x, (App<F, sep>, __) sp )
      where __ : MonadAlg<F>, AltAlg<F>, CollectionAlg<Collection> =>
      sepBy1( x, sp ).or( x.__.pure( x.__.nil<a>().pair( x.__ ) ).pair( x.__ ) );

    public static (App<F, (App<Collection, a>, __)>, __) sepBy1<__, F, a, sep>( this (App<F, a>, __ __) x, (App<F, sep>, __) sp )
      where __ : MonadAlg<F>, AltAlg<F>, CollectionAlg<Collection> =>
      x.map( x.__.cons<__, a>() ).apply( sp.i_( x ).many() );

    // filter
    // http://hackage.haskell.org/package/base-4.12.0.0/docs/Control-Monad.html
    //filterM   :: (Applicative m) => (a -> m Bool) -> [a] -> m [a]
    //filterM p = foldr (\ x -> liftA2 (\ flg -> if flg then (x:) else id) (p x)) (pure [])

  }
}
