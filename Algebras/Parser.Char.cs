using System;

namespace Parser.Char {
  using Algebras;
  using Algebras.Control;
  using Data.Collection;
  using Data.Collection.Impl;
  using Data.Either;
  using Data.State;
  using static Data.Collection.Impl.Static;
  using static Static;

  // class ParserAlg f a where
  //   satisfy :: (a -> bool) -> f a
  public interface ParserAlg<F, a> {
    App<F, a> satisfy( Func<a, bool> f );
  }

  // class ParserAlg2 f a where
  //   parseAny :: f a
  public interface ParserAlg2<F, a> {
    App<F, a> parseAny();
  }

  public static class Static {
    public static string charJoin<__>( (App<Collection, char>, __) xs )
      where __ : FoldableAlg<Collection> => xs.foldl( "" )( ( acc, x ) => acc + x );

    public static string strJoin<__>( (App<Collection, string>, __) xs )
      where __ : FoldableAlg<Collection> => xs.foldl( "" )( ( acc, x ) => acc + x );
  }

  public static class Extensions {

    #region Two other ways to define a parser

    // ParserAlg2<F, char> - it's nice that satisfy throw errors. The implementation
    // only handles EOF.

    public static (App<F, a>, __) anyThingOf<F, a, __>( this (F, __) impl )
      where __ : ParserAlg2<F, a> => impl.useSnd( x => x.parseAny() );

    public static (App<F, char>, __) anyChar2<F, __>( this (F, __) impl )
      where __ : ParserAlg2<F, char> => impl.anyThingOf<F, char, __>();

    public static (App<F, char>, __) satisfy2<F, __>( this (F, __) impl, Func<char, bool> f )
      where __ : ParserAlg2<F, char>, MonadAlg<F>, ThrowErrorAlg<F, string> =>
        from c in impl.anyChar2()
        from _ in f( c )
          ? impl.pure( c )
          : impl.fail<F, char, __>( $"Unexpected char: ${c}" )
        select c;

    // Parser is defined only with the State (stream of chars is fixed to String)

    public static (App<F, char>, __) anyCharST<F, __>( this (F, __) impl )
      where __ : BindAlg<F>, StateAlg<F, Either<string>, string>, ApplicativeAlg<Either<string>>, ThrowErrorAlg<F, string> =>
        from ss in impl.get<F, Either<string>, string, __>()
        from c in ss != ""
          ? impl.put<F, Either<string>, string, __>( ss.Substring( 1 ) ).map( _ => ss[0] )
          : impl.fail<F, char, __>( "Unexpected EOF" )
        select c;

    public static (App<F, char>, __) satisfyST<F, __>( this (F, __) impl, Func<char, bool> f )
      where __ : MonadAlg<F>, StateAlg<F, Either<string>, string>, ApplicativeAlg<Either<string>>, ThrowErrorAlg<F, string> =>
        from c in impl.anyCharST()
        from _ in f( c )
          ? impl.pure( c )
          : impl.fail<F, char, __>( $"Unexpected char: ${c}" )
        select c;

    #endregion

    // ParserAlg<F, char>

    public static (App<F, a>, __) satisfy<F, a, __>( this (F, __) impl, Func<a, bool> f )
      where __ : ParserAlg<F, a> => impl.useSnd( x => x.satisfy( f ) );

    public static (App<F, char>, __) satisfy<F, __>( this (F, __) impl, Func<char, bool> f )
      where __ : ParserAlg<F, char> => impl.satisfy<F, char, __>( f );

    public static (App<F, char>, __) letter<F, __>( this (F, __) impl )
      where __ : ParserAlg<F, char> => impl.satisfy( System.Char.IsLetter );

    public static (App<F, char>, __) digit<F, __>( this (F, __) impl )
      where __ : ParserAlg<F, char> => impl.satisfy( System.Char.IsDigit );

    public static (App<F, char>, __) space<F, __>( this (F, __) impl )
      where __ : ParserAlg<F, char> => impl.satisfy( System.Char.IsWhiteSpace );

    public static (App<F, char>, __) anychar<F, __>( this (F, __) impl )
      where __ : ParserAlg<F, char> => impl.satisfy( _ => true );

    public static (App<F, char>, __) @char<F, __>( this (F, __) impl, char c )
      where __ : ParserAlg<F, char> => impl.satisfy( ci => ci == c );

    public static (App<F, char>, __) oneOf<F, __>( this (F, __) impl, string s )
      where __ : ParserAlg<F, char> => impl.satisfy( ci => s.IndexOf( ci ) > -1 );

    public static (App<F, char>, __) noneOf<F, __>( this (F, __) impl, string s )
      where __ : ParserAlg<F, char> => impl.satisfy( ci => s.IndexOf( ci ) == -1 );

    public static (App<F, char>, __) hexDigit<F, __>( this (F, __) impl )
      where __ : ParserAlg<F, char>, AltAlg<F> => impl.digit().or( impl.oneOf( "abcdefABCDEF" ) );

    // number
    public static (App<F, int>, __) number<F, __>( this (F, __) impl )
      where __ : ParserAlg<F, char>, MonadAlg<F>, AltAlg<F>, PlusAlg<Collection>, FoldableAlg<Collection> =>
        impl.digit().manyS1().map( Int32.Parse );

    // fail
    public static (App<F, a>, __) fail<F, a, __>( this (F, __) impl, string message )
      where __ : ThrowErrorAlg<F, string> =>
        impl.fail<F, string, a, __>( message );

    // many
    public static (App<F, string>, __) manyS<F, __>( this (App<F, char>, __) impl )
      where __ : MonadAlg<F>, AltAlg<F>, PlusAlg<Collection>, FoldableAlg<Collection> =>
        impl.many().map( charJoin );

    public static (App<F, string>, __) manyS1<F, __>( this (App<F, char>, __) impl )
      where __ : MonadAlg<F>, AltAlg<F>, PlusAlg<Collection>, FoldableAlg<Collection> =>
        impl.many1().map( charJoin );

    // string
    public static (App<F, string>, __) @string<F, __>( this (F, __) impl, string s )
      where __ : ParserAlg<F, char>, ApplicativeAlg<F>, FunctorAlg<Collection>, FoldableAlg<Collection> =>
        // TODO: this is a hack to use injection directly, use CollectionAlg when implemented
        s.ToCharArray().Inj().pair( impl.Item2 )
          .map( x => impl.@char( x ) )
          .foldl( impl.pure( "" ) )( impl.liftA2_( ( string acc, char x ) => $"{acc}{x}" ) );

    // TODO: anyString - to specific, depends on fixed string state
    public static (App<F, string>, __) anyString<F, __>( this (F, __) impl, int n )
      where __ : BindAlg<F>, StateAlg<F, Either<string>, string>, ApplicativeAlg<Either<string>>, ThrowErrorAlg<F, string> =>
        from ss in impl.get<F, Either<string>, string, __>()
        from x in ss.Length >= n
          ? impl.put<F, Either<string>, string, __>( ss.Substring( n ) ).map( _ => ss.Substring( 0, n ) )
          : impl.fail<F, string, string, __>( "Unexpected EOF" )
        select x;

    // TODO: anyString - with traversable, make stack overflow safe
    public static (App<F, string>, __) anyString_<F, __>( this (F, __) impl, int n )
      where __ : ParserAlg<F, char>, ApplicativeAlg<F>, FoldableAlg<Collection>, TraversableAlg<Collection, F> {
      var Coll_F = replicate( impl.anychar().Item1, n );
      var F_Coll = (Coll_F, impl.Item2).sequence();

      return F_Coll.map( x => charJoin( (x, impl.Item2) ) );
    }

  }
}
