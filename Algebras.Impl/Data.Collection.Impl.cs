using System;
using System.Collections.Generic;
using System.Linq;

namespace Data.Collection.Impl {
  using Algebras;
  using Algebras.Control;
  using static Algebras.Static;

  public class CollectionImpl : CollectionAlg<Collection> {

    public static CollectionAlg<Collection> Instance = new CollectionImpl();

    class Collection<a> : NewType1<IEnumerable<a>, Collection, a> { }

    static App<Collection, a> inj<a>( IEnumerable<a> x ) => Collection<a>.I.Inj( x );
    static IEnumerable<a> prj<a>( App<Collection, a> x ) => Collection<a>.I.Prj( x );

    static IEnumerable<a> cons<a>( a x, IEnumerable<a> xs ) { yield return x; foreach ( var xx in xs ) yield return xx; }

    public App<Collection, a> cons<a>( a head, App<Collection, a> tail ) => inj( cons( head, prj( tail ) ) );

    public App<Collection, a> nil<a>() => inj( Enumerable.Empty<a>() );

    public App<Collection, a> list<a>( IEnumerable<a> xs ) => inj( xs );

    public IEnumerable<a> enumerable<a>( App<Collection, a> c ) => prj( c );
  }

  // Functor
  public class FunctorCollection : FunctorAlg<Collection> {
    CollectionAlg<Collection> co = CollectionImpl.Instance;
    public Func<App<Collection, a>, App<Collection, b>> map<a, b>( Func<a, b> f ) =>
      x => co.list( co.enumerable( x ).Select( f ) );

    public static FunctorAlg<Collection> Instance = new FunctorCollection();
  }

  // Apply : Functor
  public class ApplyCollection : FunctorCollection, ApplyAlg<Collection> {
    CollectionAlg<Collection> co = CollectionImpl.Instance;
    public Func<App<Collection, a>, App<Collection, b>> apply<a, b>( App<Collection, Func<a, b>> f ) =>
      x => co.list( co.enumerable( f ).SelectMany( ff => co.enumerable( x ).Select( ff ) ) );

    public static new ApplyAlg<Collection> Instance = new ApplyCollection();
  }

  // Bind : Apply
  public class BindCollection : ApplyCollection, BindAlg<Collection> {
    CollectionAlg<Collection> co = CollectionImpl.Instance;
    public Func<Func<a, App<Collection, b>>, App<Collection, b>> bind<a, b>( App<Collection, a> x ) =>
      f => co.list( co.enumerable( x ).SelectMany( y => co.enumerable( f( y ) ) ) );

    public static new BindAlg<Collection> Instance = new BindCollection();
  }

  // Alt : Functor
  public class AltCollection : FunctorCollection, AltAlg<Collection> {
    CollectionAlg<Collection> co = CollectionImpl.Instance;
    public Func<App<Collection, a>, App<Collection, a>> alt<a>( App<Collection, a> x ) =>
      y => co.list( co.enumerable( x ).Concat( co.enumerable( y ) ) );

    public static new AltAlg<Collection> Instance = new AltCollection();
  }

  // Applicative : Apply
  public class ApplicativeCollection : ApplyCollection, ApplicativeAlg<Collection> {
    CollectionAlg<Collection> co = CollectionImpl.Instance;
    public App<Collection, a> pure<a>( a x ) => co.list( new[] { x } );

    public static new ApplicativeAlg<Collection> Instance = new ApplicativeCollection();
  }

  // Plus : Alt
  public class PlusCollection : AltCollection, PlusAlg<Collection> {
    CollectionAlg<Collection> co = CollectionImpl.Instance;
    public App<Collection, a> empty<a>() => co.list( Enumerable.Empty<a>() );

    public static new PlusAlg<Collection> Instance = new PlusCollection();
  }

  // Alternative
  public class AlternativeCollection : ApplicativeCollection, AlternativeAlg<Collection> {
    protected AlternativeCollection() { }
    public Func<App<Collection, a>, App<Collection, a>> alt<a>( App<Collection, a> x ) => AltCollection.Instance.alt( x );
    public App<Collection, a> empty<a>() => PlusCollection.Instance.empty<a>();

    public static new AlternativeAlg<Collection> Instance = new AlternativeCollection();
  }

  // Monad
  public class MonadCollection : BindCollection, MonadAlg<Collection> {
    protected MonadCollection() { }
    public App<Collection, a> pure<a>( a x ) => ApplicativeCollection.Instance.pure( x );

    public static new MonadAlg<Collection> Instance = new MonadCollection();
  }

  // MonadZero
  public class MonadZeroCollection : MonadCollection, MonadZeroAlg<Collection> {
    public Func<App<Collection, a>, App<Collection, a>> alt<a>( App<Collection, a> x ) => AlternativeCollection.Instance.alt( x );
    public App<Collection, a> empty<a>() => AlternativeCollection.Instance.empty<a>();

    public static new MonadZeroAlg<Collection> Instance = new MonadZeroCollection();
  }

  // Foldable
  public class FoldableCollection : FoldableAlg<Collection> {
    CollectionAlg<Collection> co = CollectionImpl.Instance;
    public Func<b, Func<App<Collection, a>, b>> foldl<a, b>( Func<b, a, b> f ) => bb => xs => co.enumerable( xs ).Aggregate( bb, f );
    public Func<b, Func<App<Collection, a>, b>> foldr<a, b>( Func<a, b, b> f ) => bb => xs => co.enumerable( xs ).Aggregate( bb, ( acc, x ) => f( x, acc ) );

    public static FoldableAlg<Collection> Instance = new FoldableCollection();
  }

  // Traversable
  public class TraversableCollection<M> : TraversableAlg<Collection, M> {
    CollectionAlg<Collection> co = CollectionImpl.Instance;
    FoldableAlg<Collection>  fld = FoldableCollection.Instance;
    ApplicativeAlg<M>        apM;
    public TraversableCollection( ApplicativeAlg<M> x ) { apM = x; }

    public Func<App<Collection, a>, App<M, App<Collection, b>>> traverse<a, b>( Func<a, App<M, b>> f ) => x =>
      (x, fld).foldl( apM.pure( co.nil<b>() ) )( ( acc, aa ) => {
        var liftCons = (default( M ), apM).liftA2<M, b, App<Collection, b>, App<Collection, b>, ApplicativeAlg<M>>( ( x1, x2 ) => co.cons( x1 )( (x2, co) ).Item1 );

        return liftCons( (f( aa ), apM) )( (acc, apM) ).Item1;
      } );

    public App<M, App<Collection, a>> sequence<a>( App<Collection, App<M, a>> x ) => traverse<App<M, a>, a>( identity )( x );
  }

}
