using System;
using System.Globalization;

namespace Parser.Json {
  using Algebras;
  using Algebras.Control;
  using Data.Collection;
  using Parser.Char;

  /*
   * This interface represents the relationship between syntax and semantics.
   *
   * It gives the JSON syntax and its instances gives the JSON semantics. Although for JSON
   * there is no operational rules, syntax is not constrained in any way.
   */
  public interface JsonSymantics<j> {
    j @null();
    j @true();
    j @false();
    j str( string s );
    j num( int i );
    j arr( App<Collection, j> ar );
    j obj( App<Collection, (string, j)> kv );
  }

  public static class Static {

    static char to4Hex( (char a, char b, char c, char d) x ) =>
      Convert.ToChar( Int32.Parse( $"{x.a}{x.b}{x.c}{x.d}", NumberStyles.AllowHexSpecifier ) );

    static (App<F, char>, __) hexEscaped<F, __>( this (F, __) exp )
      where __ : ParserAlg<F, char>, ApplyAlg<F>, AltAlg<F> =>
        exp.hexDigit().zip( exp.hexDigit(), exp.hexDigit(), exp.hexDigit() ).map( to4Hex );

    static (App<F, char>, __) escaped<F, __>( this (F, __) exp )
      where __ : ParserAlg<F, char>, ApplyAlg<F>, AltAlg<F> =>
        exp.@char( '\\' ).i_( exp.oneOf( "\"\\" )
                               .or( exp.@char( 'b' ).map_( '\b' ) )
                               .or( exp.@char( 'f' ).map_( '\f' ) )
                               .or( exp.@char( 'n' ).map_( '\n' ) )
                               .or( exp.@char( 'r' ).map_( '\r' ) )
                               .or( exp.@char( 't' ).map_( '\t' ) )
                               .or( exp.@char( 'u' ).i_( exp.hexEscaped() ) )
                             );

    /*
     * The parser transforms ParserAlg, MonadAlg, AltAlg, ... algebras to JSON syntax algebra (JsonSymantics).
     *
     * Comments contains Haskell definitions for easier understanding and comparison.
     *
     * Type-classes in Haskell implicitly "transfer" implementations (dictionary of functions) and here this
     * _context_ is explicitly passed through with the help of C# extension methods.
     */
    public static (App<F, j>, __) CreateJsonParser<F, j, __>( JsonSymantics<j> json, (F, __) p ) where
                                     // To make a JSON parser we need these ingredients:
                                     // - each dependency brings specific capability
      __ : ParserAlg<F, char>        // a language for parsing characters,
         , MonadAlg<F>               // which operations can be sequenced
         , AltAlg<F>                 // with a choice (branching),
         , ThrowErrorAlg<F, string>  // support to throw errors,
         , CollectionAlg<Collection> // a way to collect,
         , FoldableAlg<Collection>   // and reduce results
         , PlusAlg<Collection> {     // which can be empty.

      // spaces = many space
      var spaces = p.space().many();
      // quoted = char '"' *> (many (noneOf "\\\"\b\f\n\r\t" <|> escaped)) <* char '"'
      var quoted = p.@char( '"' ).i_( p.noneOf( "\\\"\b\f\n\r\t" ).or( p.escaped() ).manyS1() )._i( p.@char( '"' ) );

      // spaced c = spaces *> char c <* spaces
      (App<F, char>, __) spaced( char c ) => spaces.i_( p.@char( c ) )._i( spaces );

      // key x = (,) <$> quoted <* spaced ':' <*> x
      Func<(App<F, (string, j)>, __)> key( Func<(App<F, j>, __)> x ) => () =>
        quoted._i( spaced( ':' ) ).zip( x() );

      // between l r x = spaced l >>= (\_ -> sepBy x (spaced ',')) <* spaced r
      (App<F, (App<Collection, a>, __)>, __) between<a>( char l, char r, Func<(App<F, a>, __)> x ) =>
        spaced( l ).bind( _ => x().sepBy( spaced( ',' ) ) )._i( spaced( r ) );

      // Parse base JSON types
      // jNull = string "null" $> json.@null
      var jNull   = p.@string( "null" ).map_( json.@null() );
      var jTrue   = p.@string( "true" ).map_( json.@true() );
      var jFalse  = p.@string( "false" ).map_( json.@false() );
      var jNumber = p.number().map( json.num );
      var jString = quoted.map( json.str );

      // jArray = fmap json.arr . between '[' ']'
      (App<F, j>, __) jArray( Func<(App<F, j>, __)> x ) =>
        between( '[', ']', x ).map( y => json.arr( y.Item1 ) );

      // jObject = fmap json.obj . between '{' '}'
      (App<F, j>, __) jObject( Func<(App<F, (string, j)>, __)> x ) =>
        between( '{', '}', x ).map( y => json.obj( y.Item1 ) );

      // jValue = jNull <|> jTrue <|> jFalse <|> jNumber <|> jString <|> jArray jValue <|> jObject (key jValue)
      (App<F, j>, __) jValue() =>
             jNull
        .or( jTrue )
        .or( jFalse )
        .or( jNumber )
        .or( jString )
        .or( jArray( jValue ) )
        .or( jObject( key( jValue ) ) );

      return jValue();
    }
  }
}
