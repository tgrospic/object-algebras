using System;
using System.Linq;

namespace Data.Collection.Impl {
  using Algebras;
  using Algebras.Control;
  using static Algebras.Static;
  using static Data.Collection.Impl.Static;

  // Functor
  public class FunctorCollection : FunctorAlg<Collection> {
    protected FunctorCollection() { }
    public Func<App<Collection, a>, App<Collection, b>> map<a, b>( Func<a, b> f ) =>
      x => x.Prj().Select( f ).Inj();

    public static FunctorAlg<Collection> Instance = new FunctorCollection();
  }

  // Apply : Functor
  public class ApplyCollection : FunctorCollection, ApplyAlg<Collection> {
    protected ApplyCollection() { }
    public Func<App<Collection, a>, App<Collection, b>> apply<a, b>( App<Collection, Func<a, b>> f ) =>
      x => f.Prj().SelectMany( ff => x.Prj().Select( ff ) ).Inj();

    public static new ApplyAlg<Collection> Instance = new ApplyCollection();
  }

  // Bind : Apply
  public class BindCollection : ApplyCollection, BindAlg<Collection> {
    protected BindCollection() { }
    public Func<Func<a, App<Collection, b>>, App<Collection, b>> bind<a, b>( App<Collection, a> x ) =>
      f => x.Prj().SelectMany( y => f( y ).Prj() ).Inj();

    public static new BindAlg<Collection> Instance = new BindCollection();
  }

  // Alt : Functor
  public class AltCollection : FunctorCollection, AltAlg<Collection> {
    protected AltCollection() { }
    public Func<App<Collection, a>, App<Collection, a>> alt<a>( App<Collection, a> x ) =>
      y => x.Prj().Concat( y.Prj() ).Inj();

    public static new AltAlg<Collection> Instance = new AltCollection();
  }

  // Applicative : Apply
  public class ApplicativeCollection : ApplyCollection, ApplicativeAlg<Collection> {
    protected ApplicativeCollection() { }
    public App<Collection, a> pure<a>( a x ) => new[] { x }.Inj();

    public static new ApplicativeAlg<Collection> Instance = new ApplicativeCollection();
  }

  // Plus : Alt
  public class PlusCollection : AltCollection, PlusAlg<Collection> {
    protected PlusCollection() { }
    public App<Collection, a> empty<a>() => Enumerable.Empty<a>().Inj();

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
    protected FoldableCollection() { }
    public Func<b, Func<App<Collection, a>, b>> foldl<a, b>( Func<b, a, b> f ) => bb => xs => xs.Prj().Aggregate( bb, f );
    public Func<b, Func<App<Collection, a>, b>> foldr<a, b>( Func<a, b, b> f ) => bb => xs => xs.Prj().Aggregate( bb, ( acc, x ) => f( x, acc ) );

    public static FoldableAlg<Collection> Instance = new FoldableCollection();
  }

  // Traversable
  public class TraversableCollection<M> : TraversableAlg<Collection, M> {
    PlusAlg<Collection>      ap = PlusCollection.Instance;
    FoldableAlg<Collection> fld = FoldableCollection.Instance;
    ApplicativeAlg<M>       apM;
    public TraversableCollection( ApplicativeAlg<M> x ) { apM = x; }

    public Func<App<Collection, a>, App<M, App<Collection, b>>> traverse<a, b>( Func<a, App<M, b>> f ) => x =>
      (x, fld).foldl( apM.pure( ap.empty<b>() ) )( ( acc, aa ) => {
        var liftCons = (default( M ), apM).liftA2<M, b, App<Collection, b>, App<Collection, b>, ApplicativeAlg<M>>( ( x1, x2 ) => cons<ApplicativeAlg<M>, b>( x1 )( (x2, apM) ).Item1 );

        return liftCons( (f( aa ), apM) )( (acc, apM) ).Item1;
      } );

    public App<M, App<Collection, a>> sequence<a>( App<Collection, App<M, a>> x ) => traverse<App<M, a>, a>( identity )( x );
  }

}
