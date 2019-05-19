using System;

namespace Demo {
  using Algebras;
  using Algebras.Control;
  using Data.Collection;
  using Data.Collection.Impl;
  using Data.Either;
  using Data.Either.Impl;
  using Parser.Char;
  using Data.State;
  using Data.State.Impl;

  class ParserCharImpl1 : ParserAlg<State<string>, char> {
    StateAlg<State<string>, Either<string>, string> _st = StateImpl<string, Either<string>>.Instance;
    EitherAlg<Either<string>, string>               _ea = EitherImpl<string>.Instance;
    // Parser implementation
    App<State<string>, char> ParserAlg<State<string>, char>.satisfy( Func<char, bool> f ) =>
      _st.makeState( ss => {
        if ( ss != "" && f( ss[0] ) ) {
          return _ea.right( ss[0] ).pair( ss.Substring( 1 ) );
        } else if ( ss != "" ) {
          return _ea.left<char>( $"Unexpected char: '{ss[0]}'" ).pair( ss );
        } else {
          return _ea.left<char>( $"Unexpected EOF" ).pair( ss );
        }
      } );

    public static ParserAlg<State<string>, char> Instance = new ParserCharImpl1();
  }

  class ParserCharImpl2 : ParserAlg2<State<string>, char> {
    StateAlg<State<string>, Either<string>, string> _st = StateImpl<string, Either<string>>.Instance;
    EitherAlg<Either<string>, string>               _ea = EitherImpl<string>.Instance;
    // Parser implementation
    App<State<string>, char> ParserAlg2<State<string>, char>.parseAny() =>
      _st.makeState( ss =>
        ss != ""
          ? _ea.right( ss[0] ).pair( ss.Substring( 1 ) )
          : _ea.left<char>( $"Unexpected EOF" ).pair( ss )
      );
  }

  public class ParserImpl
    : ParserAlg<State<string>, char>
    , EitherAlg<Either<string>, string>
    , ApplicativeAlg<Either<string>>
    , MonadAlg<State<string>>
    , AltAlg<State<string>>
    , ThrowErrorAlg<State<string>, string>
    , StateAlg<State<string>, Either<string>, string>
    , TraversableAlg<Collection, State<string>>
    , FunctorAlg<Collection>
    , PlusAlg<Collection>
    , FoldableAlg<Collection> {

    // State
    ParserAlg<State<string>, char>                  parser = ParserCharImpl1.Instance;
    StateAlg<State<string>, Either<string>, string>     st = StateImpl<string, Either<string>>.Instance;
    MonadAlg<State<string>>                             mo;
    AltAlg<State<string>>                              alt;
    ThrowErrorAlg<State<string>, string>                th;
    // Either
    EitherAlg<Either<string>, string>                   ei = EitherImpl<string>.Instance;
    ApplicativeAlg<Either<string>>                      ap = ApplicativeEither<string>.Instance;
    // Collection
    PlusAlg<Collection>                              plusC = PlusCollection.Instance;
    FoldableAlg<Collection>                          foldC = FoldableCollection.Instance;
    // Traversable
    TraversableAlg<Collection, State<string>>   travColl;

    public ParserImpl() {
      var bindEi = BindEither<string>.Instance;
      // To combine State and Either we need Traversable Either to State.
      var applSt = new ApplicativeState<string, Either<string>>( ap );
      var travEi = new TraversableEither<string, State<string>>( applSt );
      // If we have Traversable Either we can _bind_ (nest) Either inside State.
      var bindSt = new BindState<string, Either<string>>( bindEi, travEi );

      mo       = new MonadState<string, Either<string>>( ap, bindSt );

      alt      = new AltState<string, Either<string>>( ei, ap );
      th       = new ThrowErrorState<string, Either<string>>( ei );
      travColl = new TraversableCollection<State<string>>( applSt );
    }

    // Parser
    App<State<string>, char> ParserAlg<State<string>, char>.satisfy( Func<char, bool> f ) => parser.satisfy( f );
    // State
    (App<Either<string>, a>, string) StateAlg<State<string>, Either<string>, string>.runState<a>( App<State<string>, a> p, string s )      => st.runState( p, s );
    App<State<string>, a> StateAlg<State<string>, Either<string>, string>.makeState<a>( Func<string, (App<Either<string>, a>, string)> f ) => st.makeState( f );
    // Monadic API
    Func<App<State<string>, a>, App<State<string>, b>> FunctorAlg<State<string>>.map<a, b>( Func<a, b> f )                     => mo.map( f );
    Func<App<State<string>, a>, App<State<string>, b>> ApplyAlg<State<string>>.apply<a, b>( App<State<string>, Func<a, b>> f ) => mo.apply( f );
    App<State<string>, a> ApplicativeAlg<State<string>>.pure<a>( a x )                                                         => mo.pure( x );
    Func<Func<a, App<State<string>, b>>, App<State<string>, b>> BindAlg<State<string>>.bind<a, b>( App<State<string>, a> x )   => mo.bind<a, b>( x );
    Func<App<State<string>, a>, App<State<string>, a>> AltAlg<State<string>>.alt<a>( App<State<string>, a> x ) => alt.alt( x );
    // Either
    App<Either<string>, a> EitherAlg<Either<string>, string>.left<a>( string ee )                                             => ei.left<a>( ee );
    App<Either<string>, a> EitherAlg<Either<string>, string>.right<a>( a aa )                                                 => ei.right( aa );
    b EitherAlg<Either<string>, string>.either<a, b>( App<Either<string>, a> ea, Func<string, b> onLeft, Func<a, b> onRight ) => ei.either( ea, onLeft, onRight );
    // Either - Applicative
    Func<App<Either<string>, a>, App<Either<string>, b>> FunctorAlg<Either<string>>.map<a, b>( Func<a, b> f )                      => ap.map( f );
    Func<App<Either<string>, a>, App<Either<string>, b>> ApplyAlg<Either<string>>.apply<a, b>( App<Either<string>, Func<a, b>> f ) => ap.apply( f );
    App<Either<string>, a> ApplicativeAlg<Either<string>>.pure<a>( a x )                                                           => ap.pure( x );
    // Collection
    Func<App<Collection, a>, App<Collection, b>> FunctorAlg<Collection>.map<a, b>( Func<a, b> f )  => plusC.map( f );
    Func<App<Collection, a>, App<Collection, a>> AltAlg<Collection>.alt<a>( App<Collection, a> x ) => plusC.alt( x );
    App<Collection, a> PlusAlg<Collection>.empty<a>()                                              => plusC.empty<a>();
    Func<b, Func<App<Collection, a>, b>> FoldableAlg<Collection>.foldl<a, b>( Func<b, a, b> f )    => foldC.foldl( f );
    Func<b, Func<App<Collection, a>, b>> FoldableAlg<Collection>.foldr<a, b>( Func<a, b, b> f )    => foldC.foldr( f );
    Func<App<Collection, a>, App<State<string>, App<Collection, b>>> TraversableAlg<Collection, State<string>>.traverse<a, b>( Func<a, App<State<string>, b>> f )
      => travColl.traverse( f );
    App<State<string>, App<Collection, a>> TraversableAlg<Collection, State<string>>.sequence<a>( App<Collection, App<State<string>, a>> x )
      => travColl.sequence( x );
    // Throw
    App<State<string>, a> ThrowErrorAlg<State<string>, string>.throwError<a>( string ex ) => th.throwError<a>( ex );
  }

}
