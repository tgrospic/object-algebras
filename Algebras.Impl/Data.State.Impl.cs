using System;

namespace Data.State.Impl {
  using Algebras;
  using Algebras.Category;
  using Algebras.Control;
  using Data.Either;
  using Data.State;

  public class StateImpl<s, G> : StateAlg<State<s>, G, s> {

    private StateImpl() { }

    // Singleton instance
    public static StateAlg<State<s>, G, s> Instance = new StateImpl<s, G>();

    /*
     * StateC is a concrete type which is accessible only to `StateImpl<s, G>` instance.
     *
     * newtype StateC s g = ∀a. s -> (g a, s)
     */
    delegate (App<G, a>, s) StateC<a>( s _ );

    /*
     * State higher-kinded transformer.
     *
     * The purpuse of this class is to provide two functions to transform between concrete and higher-kind representation.
     *
     * This means that `App<State<s>, a>` and `StateC<a>` are isomorphic.
     * `App` variant is what is visible to outside and what is used on the DSL level.
     */
    class StateHKT<a> : NewType1<StateC<a>, State<s>, a> { }

    /*
     * Injection function : _lifts_ a concrete type to App type.
     */
    static App<State<s>, a> inj<a>( StateC<a> x ) => StateHKT<a>.I.Inj( x );

    /*
     * Projection function - _lower_ an App type to a concrete type.
     */
    static StateC<a> prj<a>( App<State<s>, a> x ) => StateHKT<a>.I.Prj( x );

    // Create State - introduction
    public App<State<s>, a> makeState<a>( Func<s, (App<G, a>, s)> f ) => inj( new StateC<a>( f ) );
    // Run State - elimination
    public (App<G, a>, s) runState<a>( App<State<s>, a> p, s ss )     => prj( p )( ss );
  }

  // Functor
  public class FunctorState<s, G> : FunctorAlg<State<s>> {
    StateAlg<State<s>, G, s> st = StateImpl<s, G>.Instance;
    FunctorAlg<G>           fnG;
    public FunctorState( FunctorAlg<G> x ) { fnG = x; }
    public virtual Func<App<State<s>, a>, App<State<s>, b>> map<a, b>( Func<a, b> f ) => pa =>
      st.makeState( ss => {
        var (aa, sss) = st.runState( pa, ss );

        return (aa.lifted( fnG, x => x.map( f ) ), sss);
      } );
  }

  // Example with NewType2 type
  public class FunctorStateCat<s, G> : FunctorCat<State<s>> {
    public interface FunctorCat_Func : FunctorCat<G>, FunctionAlg<Func> { }
    StateAlg<State<s>, G, s> st = StateImpl<s, G>.Instance;
    FunctorCat_Func          ff;
    public FunctorStateCat( FunctorCat_Func x ) { ff = x; }
    public App<App<Func, App<State<s>, a>>, App<State<s>, b>> map<a, b>( App<App<Func, a>, b> f ) {
      Func<App<State<s>, a>, App<State<s>, b>> resFun = pa =>
        st.makeState( ss => {
          var (aa, sss) = st.runState( pa, ss );

          // Function application and abstraction is also an effect.
          return (aa.lifted( ff, x => x.map( f ) ), sss);
        } );
      /*
       * State should be defined with App<Func, ...>, so FunctorCat_Func dependency is not necessary.
       *   App<F, a> makeState<a>( App<App<Func, s, (App<G, a>, s)>> f );
       */
      return ff.makeFun( resFun );
    }
  }

  // Apply : Functor
  public class ApplyState<s, G> : FunctorState<s, G>, ApplyAlg<State<s>> {
    StateAlg<State<s>, G, s> st = StateImpl<s, G>.Instance;
    ApplyAlg<G>             apG;
    public ApplyState( ApplyAlg<G> x ) : base( x ) { apG = x; }
    public virtual Func<App<State<s>, a>, App<State<s>, b>> apply<a, b>( App<State<s>, Func<a, b>> pf ) => pa =>
      st.makeState( ss => {
        var (f, sss) = st.runState( pf, ss );
        var (x, ssss) = st.runState( pa, sss );

        return (x.lifted( apG, y => y.apply( f ) ), ssss);
      } );
  }

  // Applicative : Apply
  public class ApplicativeState<s, G> : ApplyState<s, G>, ApplicativeAlg<State<s>> {
    StateAlg<State<s>, G, s> st = StateImpl<s, G>.Instance;
    ApplicativeAlg<G>       apG;
    public ApplicativeState( ApplicativeAlg<G> x ) : base( x ) { apG = x; }
    public virtual App<State<s>, a> pure<a>( a aa ) => st.makeState( ss => (apG.pure( aa ), ss) );
  }

  // Bind : Apply
  public class BindState<s, G> : ApplyState<s, G>, BindAlg<State<s>> {
    StateAlg<State<s>, G, s>       st = StateImpl<s, G>.Instance;
    BindAlg<G>                  bindG;
    TraversableAlg<G, State<s>> travG;
    public BindState( BindAlg<G> bg, TraversableAlg<G, State<s>> tg ) : base( bg ) { bindG = bg; travG = tg; }
    public virtual Func<Func<a, App<State<s>, b>>, App<State<s>, b>> bind<a, b>( App<State<s>, a> pa ) => f =>
      st.makeState( ss => {
        var (aa, sss) = st.runState( pa, ss );

        /*
         * We are working in the interpreter level here. This means that all values must be _lifted_ with the _context_ in a pair which
         * gives us operations on DSL level (extension methods defined over (App<...>, Alg) pair).
         *
         * Here is visible how G monad interact with State monad. With G traversable State can be "extracted" inside G and continue.
         */

        var (G_State_b, _) = (aa, bindG).map( f );

        var (State_G_b, _) = (G_State_b, travG).sequence();

        var (G_G_b, rest) = st.runState( State_G_b, sss );

        var (result, _) = (G_G_b, bindG).join();

        return (result, rest);
      } );
  }

  // Monad : Applicative, Bind
  public class MonadState<s, G> : ApplicativeState<s, G>, MonadAlg<State<s>> {
    StateAlg<State<s>, G, s> st = StateImpl<s, G>.Instance;
    BindAlg<State<s>>        bn;
    public MonadState( ApplicativeAlg<G> x, BindAlg<State<s>> y ) : base( x ) { bn = y; }
    public virtual Func<Func<a, App<State<s>, b>>, App<State<s>, b>> bind<a, b>( App<State<s>, a> x ) => bn.bind<a, b>( x );
  }

  // MonadZero : Monad, Alternative
  public class MonadZeroState<s, G> : MonadState<s, G>, MonadZeroAlg<State<s>> {
    StateAlg<State<s>, G, s> st = StateImpl<s, G>.Instance;
    AlternativeAlg<State<s>> al;
    public MonadZeroState( ApplicativeAlg<G> x, BindAlg<State<s>> bn, AlternativeAlg<State<s>> an ) : base( x, bn ) { al = an; }
    public virtual Func<App<State<s>, a>, App<State<s>, a>> alt<a>( App<State<s>, a> x ) => al.alt( x );
    public virtual App<State<s>, a> empty<a>() => al.empty<a>();
  }

  // Alt : Functor
  public class AltState<s, G> : FunctorState<s, G>, AltAlg<State<s>> {
    StateAlg<State<s>, G, s> st = StateImpl<s, G>.Instance;
    EitherAlg<G, string>     ei;
    public AltState( EitherAlg<G, string> ei, FunctorAlg<G> x ) : base( x ) { this.ei = ei; }
    public virtual Func<App<State<s>, a>, App<State<s>, a>> alt<a>( App<State<s>, a> x ) => y =>
      st.makeState( ss => {
        var (aa, sss) = st.runState( x, ss );

        return ei.either( aa,
          err => st.runState( y, ss ),
          _ => (aa, sss)
        );
      } );
  }

  // ThrowError
  public class ThrowErrorState<s, G> : ThrowErrorAlg<State<s>, string> {
    StateAlg<State<s>, G, s> st = StateImpl<s, G>.Instance;
    EitherAlg<G, string>     ei;
    public ThrowErrorState( EitherAlg<G, string> ei ) { this.ei = ei; }
    public App<State<s>, a> throwError<a>( string error ) => st.makeState( ss => (ei.left<a>( error ), ss) );
  }

}
