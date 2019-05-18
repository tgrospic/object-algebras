using System;

namespace Algebras {

  public struct Unit {
    public static readonly Unit Val;
    public override string ToString() => "()";
  }

  public class Static {
    public static a identity<a>( a x ) => x;

    public static Func<b, a> konst<a, b>( a x )  => _ => x;
    public static Func<b, b> kkonst<a, b>( a _ ) => y => y;

    public static Func<a, c> pipe<a, b, c>( Func<a, b> f, Func<b, c> g )   => x => g( f( x ) );
    public static Func<a, b> pipe<a, b>( Action<a> f, Func<Unit, b> g )    => pipe( f.fnUnit(), g );
    public static Func<a, Unit> pipe<a, b, c>( Func<a, b> f, Action<b> g ) => pipe( f, g.fnUnit() );
  }

  public static class Extensions {
    public static (a, b) pair<a, b>( this a x, b y ) => (x, y);

    public static (c, b) mapFst<a, b, c>( this (a a, b b) t, Func<a, c> f ) => f( t.a ).pair( t.b );

    public static Func<a, Unit> fnUnit<a>( this Action<a> action ) => x => { action( x ); return Unit.Val; };

    public static Func<Unit> fnUnit<a>( this Action action ) => () => { action(); return Unit.Val; };

    // Helper function when implementing boiler-plate extensions
    public static (c, b) useSnd<a, b, c>( this (a a, b b) t, Func<b, c> f ) => f( t.b ).pair( t.b );

    // Execute lifted (DSL level) function
    public static b lifted<F, a, b, c>( this App<F, a> x, c y, Func<(App<F, a>, c), (b, c)> f ) => f( (x, y) ).Item1;
  }

}
