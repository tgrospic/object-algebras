using System;
using System.Collections.Generic;
using System.Linq;

namespace Data.Collection {
  using Algebras;
  using Algebras.Control;

  using static Data.Collection.Impl.Static;

  // Lightweight HKT
  public interface Collection { }

  public static class Extensions {
    // many
    public static (App<F, (App<Collection, a>, __)>, __) many<F, a, __>( this (App<F, a>, __ __) x )
      where __ : MonadAlg<F>, AltAlg<F>, PlusAlg<Collection> =>
      x.bind( h => x.many().map( cons<__, a>( h ) ) ).or( x.__.pure( x.__.empty<a>().pair( x.__ ) ).pair( x.__ ) );

    public static (App<F, (App<Collection, a>, __)>, __) many1<F, a, __>( this (App<F, a>, __ __) x )
      where __ : MonadAlg<F>, AltAlg<F>, PlusAlg<Collection> =>
      x.map( cons<__, a> ).apply( x.many() );

    public static (App<F, (App<Collection, a>, __)>, __) sepBy<F, a, sep, __>( this (App<F, a>, __ __) aa, (App<F, sep>, __) sepa )
      where __ : MonadAlg<F>, AltAlg<F>, PlusAlg<Collection> =>
      sepBy1( aa, sepa ).or( aa.__.pure( aa.__.empty<Collection, a, __>() ).pair( aa.__ ) );

    public static (App<F, (App<Collection, a>, __)>, __) sepBy1<__, F, a, sep>( this (App<F, a>, __ __) aa, (App<F, sep>, __) sepa )
      where __ : MonadAlg<F>, AltAlg<F>, PlusAlg<Collection> =>
      aa.map( cons<__, a> ).apply( sepa.i_( aa ).many() );
  }

}

// TODO: Add CollectionAlg<F> (intro, elim) and move this to implementation project.
namespace Data.Collection.Impl {
  using Algebras;

  class Collection<a> : NewType1<IEnumerable<a>, Collection, a> { }

  public // TODO: In real implementation injection and projection should be visible only to intrepreter.
  static class HKT {
    public static App<Collection, a> Inj<a>( this IEnumerable<a> x ) => Collection<a>.I.Inj( x );
    public static IEnumerable<a> Prj<a>( this App<Collection, a> x ) => Collection<a>.I.Prj( x );
  }

  public static class Static {
    static IEnumerable<a> cons<a>( a x, IEnumerable<a> xs ) { yield return x; foreach ( var xx in xs ) yield return xx; }
    public static Func<(App<Collection, a>, __), (App<Collection, a>, __)> cons<__, a>( a x ) => xs => cons( x, xs.Item1.Prj() ).Inj().pair( xs.Item2 );

    public static App<Collection, a> replicate<a>( a x, int i ) => Enumerable.Repeat( x, i ).Inj();
  }

}
