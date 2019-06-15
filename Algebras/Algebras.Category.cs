using System;

namespace Algebras.Category {

  // Semigrupoid
  public interface SemigrupoidAlg<F> {
    Func<App<App<F, a>, b>, App<App<F, a>, c>> compose<a, b, c>( App<App<F, b>, c> g );
  }

  // Category : Semigrupoid
  public interface CategoryAlg<F> : SemigrupoidAlg<F> {
    App<App<F, a>, a> id<a>();
  }

  // Function
  public interface FunctionAlg<F> {
    App<App<F, a>, b> makeFun<a, b>( Func<a, b> f );
    Func<a, b> runFun<a, b>( App<App<F, a>, b> f );
  }

  // Functor
  public interface FunctorCat<F> {
    App<App<Func, App<F, a>>, App<F, b>> map<a, b>( App<App<Func, a>, b> f );
  }

  // TODO: implement algebraic effects with delimited continuations :)
  // A right Kan extension transformer for a monad
  // http://hackage.haskell.org/package/monad-ran-0.1.0/docs/src/Control-Monad-Ran.html
  // data Ran m a = Ran { getRan :: forall b. (a -> G m b) -> H m b }
  public interface RanAlg<M, G, H, a> {
    App<H, App<M, b>> makeRan<b>( Func<a, App<G, App<M, b>>> f );
    Func<a, App<G, App<M, b>>> runRan<b>( App<H, App<M, b>> f );
  }

  public static class Extensions {

    // Semigrupoid
    public static (App<App<F, a>, c>, __) compose<F, a, b, c, __>( this (App<App<F, b>, c> g, __ __) exp, App<App<F, a>, b> f )
      where __ : SemigrupoidAlg<F> => exp.useSnd( x => x.compose<a, b, c>( exp.g )( f ) );

    // Category : Semigrupoid
    public static (App<App<F, a>, a>, __) id<F, a, __>( this __ ctx )
      where __ : CategoryAlg<F> => ctx.id<a>().pair( ctx );

    // Function
    public static (Func<a, b>, __) runFun<F, a, b, __>( this (App<App<F, a>, b>, __) exp )
      where __ : FunctionAlg<F> => exp.useSnd( y => y.runFun( exp.Item1 ) );

    // Functor
    public static (App<F, b>, __) map<F, a, b, __>( this (App<F, a>, __) exp, App<App<Func, a>, b> f )
      where __ : FunctorCat<F>, FunctionAlg<Func> => exp.useSnd( y => y.runFun( y.map( f ) )( exp.Item1 ) );
  }

  // Function _brand_
  public interface Func { }

  public class FunctionFunc : FunctionAlg<Func> {
    class FuncHKT<a, b> : NewType2<Func<a, b>, Func, a, b> { }

    static App<App<Func, a>, b> inj<a, b>( Func<a, b> x ) => FuncHKT<a, b>.I.Inj( x );
    static Func<a, b> prj<a, b>( App<App<Func, a>, b> x ) => FuncHKT<a, b>.I.Prj( x );

    public App<App<Func, a>, b> makeFun<a, b>( Func<a, b> f ) => inj( f );
    public Func<a, b> runFun<a, b>( App<App<Func, a>, b> f )  => prj( f );

    public static FunctionAlg<Func> Instance = new FunctionFunc();
  }

  public class CategoryFunc : CategoryAlg<Func> {
    FunctionAlg<Func> fn = FunctionFunc.Instance;

    public Func<App<App<Func, a>, b>, App<App<Func, a>, c>> compose<a, b, c>( App<App<Func, b>, c> f ) => g => {
      Func<a, c> res = x => fn.runFun( f )( fn.runFun( g )( x ) );
      return fn.makeFun( res );
    };

    public App<App<Func, a>, a> id<a>() {
      Func<a, a> f = x => x;
      return fn.makeFun( f );
    }
  }

}
