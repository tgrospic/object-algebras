using System.Linq;

namespace Demo {
  using Algebras;
  using Algebras.Control;
  using Data.Collection;
  using Data.Collection.Impl;
  using Parser.Json;

  interface JValue { }

  class JNull : JValue {
    public override string ToString() => "null";
  }

  class JTrue : JValue {
    public override string ToString() => "true";
  }

  class JFalse : JValue {
    public override string ToString() => "false";
  }

  class JString : JValue {
    public string Val;
    public JString( string v ) => Val = v;
    public override string ToString() => $"\"{Val}\"";
  }

  class JNumber : JValue {
    public int Val;
    public JNumber( int v ) => Val = v;
    public override string ToString() => $"{Val}";
  }

  class JObject : JValue {
    CollectionAlg<Collection> co = CollectionImpl.Instance;
    public App<Collection, (string, JValue)> Val;
    public JObject( App<Collection, (string, JValue)> v ) => Val = v;
    public override string ToString() {
      var kv = co.enumerable( Val ).Select( x => $"\"{x.Item1}\": {x.Item2}" );
      return $"{{ {string.Join( ", ", kv )} }}";
    }
  }

  class JArray : JValue {
    CollectionAlg<Collection> co = CollectionImpl.Instance;
    public App<Collection, JValue> Val;
    public JArray( App<Collection, JValue> v ) => Val = v;
    public override string ToString() => $"[ {string.Join( ", ", co.enumerable( Val ) )} ]";
  }

  class JsonImpl : JsonSymantics<JValue> {
    JValue JsonSymantics<JValue>.@null()                                     => new JNull();
    JValue JsonSymantics<JValue>.@true()                                     => new JTrue();
    JValue JsonSymantics<JValue>.@false()                                    => new JFalse();
    JValue JsonSymantics<JValue>.str( string s )                             => new JString( s );
    JValue JsonSymantics<JValue>.num( int i )                                => new JNumber( i );
    JValue JsonSymantics<JValue>.arr( App<Collection, JValue> ar )           => new JArray( ar );
    JValue JsonSymantics<JValue>.obj( App<Collection, (string, JValue)> kv ) => new JObject( kv );
  }

}
