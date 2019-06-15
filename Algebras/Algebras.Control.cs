using System;

namespace Algebras.Control {
  using static Algebras.Static;

  /*
   * Algebraic definitions are based on Object algebras as presented in very exciting paper Extensibility for the Masses.
   * The difference here is the use of higher-kinded types to express higher-order effects.
   * https://www.cs.utexas.edu/~wcook/Drafts/2012/ecoop2012.pdf
   */

  // Functor
  public interface FunctorAlg<F> {
    Func<App<F, a>, App<F, b>> map<a, b>( Func<a, b> f );
  }

  // Apply : Functor
  public interface ApplyAlg<F> : FunctorAlg<F> {
    Func<App<F, a>, App<F, b>> apply<a, b>( App<F, Func<a, b>> f );
  }

  // Applicative : Apply
  public interface ApplicativeAlg<F> : ApplyAlg<F> {
    App<F, a> pure<a>( a x );
  }

  // Bind : Apply
  public interface BindAlg<F> : ApplyAlg<F> {
    Func<Func<a, App<F, b>>, App<F, b>> bind<a, b>( App<F, a> x );
  }

  // Alt : Functor
  public interface AltAlg<F> : FunctorAlg<F> {
    Func<App<F, a>, App<F, a>> alt<a>( App<F, a> x );
  }

  // Plus : Alt
  public interface PlusAlg<F> : AltAlg<F> {
    App<F, a> empty<a>();
  }

  // Alternative : Applicative, Plus
  public interface AlternativeAlg<F> : ApplicativeAlg<F>, PlusAlg<F> { }

  // Monad : Applicative, Bind
  public interface MonadAlg<F> : ApplicativeAlg<F>, BindAlg<F> { }

  // MonadZero : Monad, Alternative
  public interface MonadZeroAlg<F> : MonadAlg<F>, AlternativeAlg<F> { }

  // MonadZero : Monad, Alternative
  public interface MonadPlusAlg<F> : MonadZeroAlg<F> { }

  // Extend : Functor
  public interface ExtendAlg<W> : FunctorAlg<W> {
    Func<App<W, a>, App<W, b>> extend<a, b>( Func<App<W, a>, b> f );
  }

  // Comonad : Extend
  public interface ComonadAlg<W> : ExtendAlg<W> {
    a extract<a>( App<W, a> x );
  }

  // ThrowError
  public interface ThrowErrorAlg<F, e> {
    App<F, a> throwError<a>( e ex );
  }

  // CatchError
  public interface CatchErrorAlg<F, e> : ThrowErrorAlg<F, e> {
    Func<Func<e, App<F, a>>, App<F, a>> catchError<a>( App<F, a> x );
  }

  //class Foldable f where
  //  foldr :: forall a b. (a -> b -> b) -> b -> f a -> b
  //  foldl :: forall a b. (b -> a -> b) -> b -> f a -> b
  //  foldMap :: forall a m. Monoid m => (a -> m) -> f a -> m

  // Foldable
  public interface FoldableAlg<F> {
    Func<b, Func<App<F, a>, b>> foldr<a, b>( Func<a, b, b> f );
    Func<b, Func<App<F, a>, b>> foldl<a, b>( Func<b, a, b> f );
    //Func<App<F, a>, App<M, b>> foldMap<M, a, b>( Func<a, App<M, b>> f );
  }

  //class (Functor t, Foldable t) <= Traversable t where
  //  traverse :: forall a b m. Applicative m => (a -> m b) -> t a -> m (t b)
  //  sequence :: forall a m. Applicative m => t (m a) -> m (t a)

  // Traversable
  public interface TraversableAlg<F, M> /* ApplicativeAlg<M> */ {
    Func<App<F, a>, App<M, App<F, b>>> traverse<a, b>( Func<a, App<M, b>> f );
    App<M, App<F, a>> sequence<a>( App<F, App<M, a>> x );
  }

  // Natural transformation : C ~> D
  public interface NatAlg<C, D> {
    App<D, a> transform<a>( App<C, a> ca );
  }

  public static class Extensions {

    // Functor
    public static (App<F, b>, __) map<F, a, b, __>( this (Func<a, b> f, __ ctx) exp, (App<F, a> a, __) x )
      where __ : FunctorAlg<F> => (exp.ctx.map( exp.f )( x.a ), exp.ctx);

    public static (App<F, b>, __) map<F, a, b, __>( this (App<F, a> x, __ ctx) exp, Func<a, b> f )
      where __ : FunctorAlg<F> => (exp.ctx.map( f )( exp.x ), exp.ctx);

    public static (App<F, b>, __) map_<F, a, b, __>( this (App<F, a>, __) x, b bb )
      where __ : FunctorAlg<F> => x.map( _ => bb );

    // Apply
    public static (App<F, b>, __) apply<F, a, b, __>( this (App<F, Func<a, b>> f, __ ctx) exp, (App<F, a> a, __) x )
      where __ : ApplyAlg<F> => (exp.ctx.apply( exp.f )( x.a ), exp.ctx);

    public static (App<F, b>, __) apply<F, a, b, __>( this (App<F, a> a, __ __) exp, (App<F, Func<a, b>>, __) f )
      where __ : ApplyAlg<F> => exp.useSnd( y => y.apply( f.Item1 )( exp.a ) );

    public static (App<F, b>, __) apply<F, a, b, __>( this (App<F, a> a, __ __) exp, App<F, Func<a, b>> f )
      where __ : ApplyAlg<F> => exp.useSnd( y => y.apply( f )( exp.a ) );

    // <* *>
    public static (App<F, a>, __) _i<F, a, b, __>( this (App<F, a>, __) x, (App<F, b>, __) y )
      where __ : ApplyAlg<F> => x.map( konst<a, b> ).apply( y );

    public static (App<F, b>, __) i_<F, a, b, __>( this (App<F, a>, __) x, (App<F, b>, __) y )
      where __ : ApplyAlg<F> => x.map( kkonst<a, b> ).apply( y );

    // zip
    public static (App<F, c>, __) zip<F, a, b, c, __>( this (App<F, a>, __) x, (App<F, b>, __) y, Func<a, Func<b, c>> f )
      where __ : ApplyAlg<F> => x.map( f ).apply( y );

    public static (App<F, c>, __) zip<F, a, b, c, __>( this (App<F, a>, __) x, (App<F, b>, __) y, Func<a, b, c> f )
      where __ : ApplyAlg<F> => x.map<F, a, Func<b, c>, __>( x1 => y1 => f( x1, y1 ) ).apply( y );

    public static (App<F, d>, __) zip<F, a, b, c, d, __>( this (App<F, a>, __) x, (App<F, b>, __) y, (App<F, c>, __) z, Func<a, b, c, d> f )
      where __ : ApplyAlg<F> => x.map<F, a, Func<b, Func<c, d>>, __>( x1 => y1 => z1 => f( x1, y1, z1 ) ).apply( y ).apply( z );

    public static (App<F, e>, __) zip<F, a, b, c, d, e, __>( this (App<F, a>, __) x, (App<F, b>, __) y, (App<F, c>, __) z, (App<F, d>, __) u, Func<a, b, c, d, e> f )
      where __ : ApplyAlg<F> => x.map<F, a, Func<b, Func<c, Func<d, e>>>, __>( x1 => y2 => z1 => u1 => f( x1, y2, z1, u1 ) ).apply( y ).apply( z ).apply( u );

    // zip - tuple
    public static (App<F, (a, b)>, __) zip<F, a, b, __>( this (App<F, a>, __) x, (App<F, b>, __) y )
      where __ : ApplyAlg<F> => x.zip( y, ValueTuple.Create );

    public static (App<F, (a, b, c)>, __) zip<F, a, b, c, __>( this (App<F, a>, __) x, (App<F, b>, __) y, (App<F, c>, __) z )
      where __ : ApplyAlg<F> => x.zip( y, z, ValueTuple.Create );

    public static (App<F, (a, b, c, d)>, __) zip<F, a, b, c, d, __>( this (App<F, a>, __) x, (App<F, b>, __) y, (App<F, c>, __) z, (App<F, d>, __) u )
      where __ : ApplyAlg<F> => x.zip( y, z, u, ValueTuple.Create );

    // liftA2
    public static Func<(App<F, a>, __), Func<(App<F, b>, __), (App<F, c>, __)>> liftA2<F, a, b, c, __>( this (F, __) exp,
      Func<a, b, c> f )
      where __ : ApplyAlg<F> => aa => bb => aa.zip( bb ).map( x => f( x.Item1, x.Item2 ) );

    // liftA2_ - tuple version
    public static Func<(App<F, a>, __), (App<F, b>, __), (App<F, c>, __)> liftA2_<F, a, b, c, __>( this (F, __) exp,
      Func<a, b, c> f )
      where __ : ApplyAlg<F> => ( aa, bb ) => aa.zip( bb ).map( x => f( x.Item1, x.Item2 ) );

    // liftA3
    public static Func<(App<F, a>, __), Func<(App<F, b>, __), Func<(App<F, c>, __), (App<F, d>, __)>>> liftA3<F, a, b, c, d, __>( this (F, __) exp,
      Func<a, b, c, d> f )
      where __ : ApplyAlg<F> => aa => bb => cc => aa.zip( bb, cc ).map( x => f( x.Item1, x.Item2, x.Item3 ) );

    // liftA3 - tuple version
    public static Func<(App<F, a>, __), (App<F, b>, __), (App<F, c>, __), (App<F, d>, __)> liftA3_<F, a, b, c, d, __>( this (F, __) exp,
      Func<a, b, c, d> f )
      where __ : ApplyAlg<F> => ( aa, bb, cc ) => aa.zip( bb, cc ).map( x => f( x.Item1, x.Item2, x.Item3 ) );

    public static (App<F, a>, __) between<F, opn, clo, a, __>( this (App<F, a>, __) aa, (App<F, opn>, __) open, (App<F, clo>, __) close )
      where __ : ApplyAlg<F> => open.i_( aa )._i( close );

    // Bind
    public static (App<F, b>, __) bind<F, a, b, __>( this (App<F, a> a, __ ctx) exp, Func<a, (App<F, b>, __)> f )
      where __ : BindAlg<F> => exp.useSnd( y => y.bind<a, b>( exp.a )( x => f( x ).Item1 ) );

    public static (App<F, b>, __) bind<F, a, b, __>( this (App<F, a> a, __) exp, Func<a, App<F, b>> f )
      where __ : BindAlg<F> => exp.useSnd( y => y.bind<a, b>( exp.a )( x => f( x ) ) );

    // Join
    public static (App<F, a>, __) join<F, a, __>( this (App<F, App<F, a>> a, __) exp )
      where __ : BindAlg<F> => exp.useSnd( y => y.bind<App<F, a>, a>( exp.a )( identity ) );

    // LINQ syntax (once and) for all `FunctorAlg<F>` and `BindAlg<F>`
    public static (App<F, b>, __) Select<F, a, b, __>( this (App<F, a>, __) exp, Func<a, b> f )
      where __ : FunctorAlg<F> => exp.map( f );

    public static (App<F, r>, __) SelectMany<F, a, b, r, __>( this (App<F, a>, __) exp, Func<a, (App<F, b>, __)> f, Func<a, b, r> result )
      where __ : BindAlg<F> => exp.bind( x => f( x ).map( y => result( x, y ) ) );

    // Applicative
    public static (App<F, a>, __) pure<F, a, __>( this __ ctx, a x )
      where __ : ApplicativeAlg<F> => ctx.pure( x ).pair( ctx );

    public static (App<F, a>, __) pure<F, a, __>( this (F, __) exp, a x )
      where __ : ApplicativeAlg<F> => exp.useSnd( y => y.pure( x ) );

    // Plus
    public static (App<F, a>, __) empty<F, a, __>( this __ ctx )
      where __ : PlusAlg<F> => ctx.empty<a>().pair( ctx );

    public static (App<F, a>, __) empty<F, a, __>( this (F, __) exp )
      where __ : PlusAlg<F> => exp.useSnd( x => x.empty<a>() );

    // Alt
    public static (App<F, a>, __) or<F, a, __>( this (App<F, a> x, __) exp, (App<F, a> a, __) y )
      where __ : AltAlg<F> => exp.useSnd( z => z.alt( exp.x )( y.a ) );

    // ThrowError
    public static (App<F, a>, __) fail<F, s, a, __>( this (F, __) exp, s message )
      where __ : ThrowErrorAlg<F, s> => exp.useSnd( x => x.throwError<a>( message ) );

    // Foldable
    public static Func<Func<a, b, b>, b> foldr<F, a, b, __>( this (App<F, a> a, __ ctx) x, b init )
      where __ : FoldableAlg<F> => f => x.ctx.foldr( f )( init )( x.a );

    public static Func<Func<b, a, b>, b> foldl<F, a, b, __>( this (App<F, a> a, __ ctx) x, b init )
      where __ : FoldableAlg<F> => f => x.ctx.foldl( f )( init )( x.a );

    public static (App<M, Unit>, __) traverse_<F, M, a, b, __>(
        this Func<a, (App<M, b>, __)> f,
        (App<F, a>, __ ctx) x
      )
      where __ : FoldableAlg<F>, ApplicativeAlg<M> =>
      x.foldr( x.ctx.pure<M, Unit, __>( Unit.Val ) )( ( aa, unit ) => f( aa ).i_( unit ) );

    public static (App<M, Unit>, __) for_<F, M, a, b, __>(
        this (App<F, a>, __) x,
        Func<a, (App<M, b>, __)> f
      )
      where __ : FoldableAlg<F>, ApplicativeAlg<M> => f.traverse_( x );

    //public static (App<M, b>, __) foldMap<F, M, a, b, __>(
    //    this (App<F, a> xs, __ __) i,
    //    Func<a, (App<M, b>, __)> f
    //  )
    //  // TODO: define MonoidAlg (mempty, append)
    //  where __ : FoldableAlg<F>, MonoidAlg<M> =>
    //  i.foldr( i.__.mempty<M, b>() )( ( aa, acc ) => acc.append( f( aa ) ) );

    // Traversable
    public static (App<M, App<F, b>>, __) traverse<F, M, a, b, __>(
        this Func<a, (App<M, b>, __)> f,
        (App<F, a>, __ __) x
      )
      where __ : TraversableAlg<F, M> =>
      x.useSnd( y => y.traverse<a, b>( z => f( z ).Item1 )( x.Item1 ) );

    public static (App<M, App<F, b>>, __) @for<F, M, a, b, __>(
        this (App<F, a>, __) x,
        Func<a, (App<M, b>, __)> f
      )
      where __ : TraversableAlg<F, M> => f.traverse( x );

    public static (App<M, App<F, a>>, __) sequence<F, M, a, __>(
        this (App<F, App<M, a>>, __) x
      )
      where __ : TraversableAlg<F, M> =>
      x.useSnd( y => y.sequence( x.Item1 ) );
  }
}
