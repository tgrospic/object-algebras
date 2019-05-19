using System;

namespace Demo {

  class Program {

    static void Main( string[] args ) {

      Console.WriteLine( $"\nParse JSON ====================================================================\n" );
      RunJson.Static.Run();

      Console.WriteLine( $"\nParse puzzle ==================================================================\n" );
      RunPuzzle.Static.Run();

    }

  }
}
