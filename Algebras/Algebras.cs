namespace Algebras {

  /*
   * Higher-kinded types implementation is based on excellent paper Lightweight higher-kinded polymorphism.
   * They presented in OCaml how to apply defunctionalization which translates higher-order programs
   * into a first-order language.
   * https://www.cl.cam.ac.uk/~jdy22/papers/lightweight-higher-kinded-polymorphism.pdf
   */

  // Type constructor (type-level `App`lication)
  // `B` represents a Brand type
  public interface App<B, a> { } // ~~ B<a>

  // Higher-kinded type constructor with injection and projection functions.
  // `C` represents concrete type
  public class NewType1<C, B, a> {
    protected NewType1() { }
    public static NewType1<C, B, a> I = new NewType1<C, B, a>();
    // C -> B<a>
    public App<B, a> Inj( C x ) => _NewType1.Inj( x );
    // B<a> -> C
    public C Prj( App<B, a> x ) => _NewType1.Prj( x );

    struct _NewType1 : App<B, a> {
      C t; _NewType1( C x ) => t = x;
      public static App<B, a> Inj( C x ) => new _NewType1( x );
      public static C Prj( App<B, a> x ) => ( (_NewType1)x ).t;

      public override string ToString() { return $"{t}"; }
    }
  }

  // HKT with 2 arguments, App partially applied
  public class NewType2<C, B, a, b> {
    protected NewType2() { }
    public static NewType2<C, B, a, b> I = new NewType2<C, B, a, b>();
    // C -> B<a, b>
    public App<App<B, a>, b> Inj( C x ) => _NewType2.Inj( x );
    // B<a, b> -> C
    public C Prj( App<App<B, a>, b> x ) => _NewType2.Prj( x );

    struct _NewType2 : App<App<B, a>, b> {
      C t; _NewType2( C x ) => t = x;
      public static App<App<B, a>, b> Inj( C x ) => new _NewType2( x );
      public static C Prj( App<App<B, a>, b> x ) => ( (_NewType2)x ).t;

      public override string ToString() { return $"{t}"; }
    }
  }

}
