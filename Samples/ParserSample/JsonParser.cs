using System;
using System.Globalization;

namespace Demo {
  using Algebras;
  using Algebras.Control;
  using Data.Collection;
  using Parser.Char;

  public interface JsonAlg<j> {
    j @null();
    j @true();
    j @false();
    j str( string s );
    j num( int i );
    j arr( App<Collection, j> ar );
    j obj( App<Collection, (string, j)> kv );
  }

  static class JsonParser {

    static char to4Hex( (char a, char b, char c, char d) x ) =>
      Convert.ToChar( Int32.Parse( $"{x.a}{x.b}{x.c}{x.d}", NumberStyles.AllowHexSpecifier ) );

    static (App<F, char>, __) hexEscaped<F, __>( this (F, __) impl )
      where __ : ParserAlg<F, char>, ApplyAlg<F>, AltAlg<F> =>
        impl.hexDigit().zip( impl.hexDigit(), impl.hexDigit(), impl.hexDigit() ).map( to4Hex );

    static (App<F, char>, __) escaped<F, __>( this (F, __) impl )
      where __ : ParserAlg<F, char>, ApplyAlg<F>, AltAlg<F> =>
        impl.@char( '\\' ).i_( impl.oneOf( "\"\\" )
                               .or( impl.@char( 'b' ).map_( '\b' ) )
                               .or( impl.@char( 'f' ).map_( '\f' ) )
                               .or( impl.@char( 'n' ).map_( '\n' ) )
                               .or( impl.@char( 'r' ).map_( '\r' ) )
                               .or( impl.@char( 't' ).map_( '\t' ) )
                               .or( impl.@char( 'u' ).i_( impl.hexEscaped() ) )
                             );

    public static (App<F, j>, __) Create<F, j, __>( JsonAlg<j> json, (F, __) p ) where
      __ : ParserAlg<F, char>
         , ThrowErrorAlg<F, string>
         , MonadAlg<F>
         , AltAlg<F>
         , PlusAlg<Collection>
         , FoldableAlg<Collection> {

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
