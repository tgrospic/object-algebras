using System;

namespace Demo {
  using Algebras;
  using Data.Either;
  using Data.Either.Impl;
  using Data.Impl;
  using Data.State;
  using Data.State.Impl;
  using Parser.Char;

  // Parser inherit generic implementation of State with Either. Different implementation can
  // use different state or error type e.g. StateEitherImpl<ParserContext, ParserError>.
  class ParserCharImpl1 : StateEitherImpl<string, string>, ParserAlg<State<string>, char> {
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

  class ParserCharImpl2 : StateEitherImpl<string, string>, ParserAlg2<State<string>, char> {
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
}
