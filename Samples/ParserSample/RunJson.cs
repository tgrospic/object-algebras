using System;

namespace Demo.RunJson {
  using Algebras.Control;
  using Data.Collection;
  using Data.Either;
  using Parser.Char;
  using Parser.Json;
  using Data.State;
  using static Parser.Json.Static;

  class Static {

    public static void Run() {
      // Create implementation instances (effect handlers) / pull dependencies
      var parser = ( default( State<string> ), new ParserCharImpl1() );
      var json   = new JsonImpl();

      // Run parser
      RunJsonParser( parser, json );
    }

    static void RunJsonParser<F, j, __>( (F, __) p, JsonSymantics<j> json ) where
      __ : ParserAlg<F, char>
         , StateAlg<F, Either<string>, string>
         , EitherAlg<Either<string>, string>
         , ThrowErrorAlg<F, string>
         , MonadAlg<F>
         , AltAlg<F>
         , CollectionAlg<Collection>
         , PlusAlg<Collection>
         , FoldableAlg<Collection> {

      var str1 = $@"{{
          ""innerObject"" : {{ ""number"": 34534543 }},
          ""array_sample"": [
            1, 2, 3,
            {{ ""specialStr"": ""@$%aaaa\tbbbbbb"" }}
          ]
        }}";

      var str2 = $@"{{
          ""nothing"": null,
          ""yes_no"": false,
          ""sample_obj"": {str1},
          ""unicode"": ""\u03BB""
        }}";

      var jsonParser = CreateJsonParser( json, p );

      // Run parser (State effect)
      var (res, rest) = jsonParser.runState( str2 );

      // Run final result (Either effect) - either with an error or parsed value
      res.either(
        err => {
          Console.WriteLine( $"\n > {rest.Replace( Environment.NewLine, "" )}" );
          Console.WriteLine( $"\n   ^---- {err}\n" );
        },
        val => Console.WriteLine( $"{val}\n" )
      );

    }
  }
}
