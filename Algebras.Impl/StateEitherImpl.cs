using System;

namespace Data.Impl {
  using Algebras;
  using Algebras.Control;
  using Data.Collection;
  using Data.Collection.Impl;
  using Data.Either;
  using Data.Either.Impl;
  using Data.State;
  using Data.State.Impl;
  using System.Collections.Generic;

  public class StateEitherImpl<st, err>
    : EitherAlg<Either<err>, err>
    , ApplicativeAlg<Either<err>>
    , MonadAlg<State<st>>
    , AltAlg<State<st>>
    , ThrowErrorAlg<State<st>, err>
    , StateAlg<State<st>, Either<err>, st>
    , TraversableAlg<Collection, State<st>>
    , CollectionAlg<Collection>
    , FunctorAlg<Collection>
    , PlusAlg<Collection>
    , FoldableAlg<Collection> {

    // State
    StateAlg<State<st>, Either<err>, st> se = StateImpl<st, Either<err>>.Instance;
    MonadAlg<State<st>>                  mo;
    AltAlg<State<st>>                   alt;
    ThrowErrorAlg<State<st>, err>        th;
    // Either
    EitherAlg<Either<err>, err>          ei = EitherImpl<err>.Instance;
    ApplicativeAlg<Either<err>>          ap = ApplicativeEither<err>.Instance;
    // Collection
    CollectionAlg<Collection>          coll = CollectionImpl.Instance;
    PlusAlg<Collection>               plusC = PlusCollection.Instance;
    FoldableAlg<Collection>           foldC = FoldableCollection.Instance;
    // Traversable
    TraversableAlg<Collection, State<st>> trC;

    public StateEitherImpl() {
      var bindEi = BindEither<err>.Instance;
      // To combine State and Either we need Traversable Either to State.
      var applSt = new ApplicativeState<st, Either<err>>( ap );
      var travEi = new TraversableEither<err, State<st>>( applSt );
      // If we have Monad and Traversable Either we can _bind_ (nest) Either inside State.
      var bindSt = new BindState<st, Either<err>>( bindEi, travEi );
      // Monad State with Either inside
      mo  = new MonadState<st, Either<err>>( ap, bindSt );
      alt = new AltState<st, Either<err>, err>( ei, ap );
      th  = new ThrowErrorState<st, Either<err>, err>( ei );
      trC = new TraversableCollection<State<st>>( applSt );
    }

    // State
    (App<Either<err>, a>, st) StateAlg<State<st>, Either<err>, st>.runState<a>( App<State<st>, a> p, st s )      => se.runState( p, s );
    App<State<st>, a> StateAlg<State<st>, Either<err>, st>.makeState<a>( Func<st, (App<Either<err>, a>, st)> f ) => se.makeState( f );
    // Either
    App<Either<err>, a> EitherAlg<Either<err>, err>.left<a>( err ee )                                             => ei.left<a>( ee );
    App<Either<err>, a> EitherAlg<Either<err>, err>.right<a>( a aa )                                              => ei.right( aa );
    b EitherAlg<Either<err>, err>.either<a, b>( App<Either<err>, a> ea, Func<err, b> onLeft, Func<a, b> onRight ) => ei.either( ea, onLeft, onRight );
    // Collection
    App<Collection, a> CollectionAlg<Collection>.nil<a>()                                   => coll.nil<a>();
    App<Collection, a> CollectionAlg<Collection>.cons<a>( a head, App<Collection, a> tail ) => coll.cons( head, tail );
    App<Collection, a> CollectionAlg<Collection>.list<a>( IEnumerable<a> xs )               => coll.list( xs );
    IEnumerable<a> CollectionAlg<Collection>.enumerable<a>( App<Collection, a> c )          => coll.enumerable( c );
    // Throw
    App<State<st>, a> ThrowErrorAlg<State<st>, err>.throwError<a>( err ex ) => th.throwError<a>( ex );

    // Monadic API
    Func<App<State<st>, a>,   App<State<st>, b>>   FunctorAlg<State<st>>  .map<a, b>( Func<a, b> f ) => mo.map( f );
    Func<App<Either<err>, a>, App<Either<err>, b>> FunctorAlg<Either<err>>.map<a, b>( Func<a, b> f ) => ap.map( f );
    Func<App<Collection, a>,  App<Collection, b>>  FunctorAlg<Collection> .map<a, b>( Func<a, b> f ) => plusC.map( f );

    Func<App<State<st>, a>,   App<State<st>, b>>   ApplyAlg<State<st>>  .apply<a, b>( App<State<st>, Func<a, b>> f )   => mo.apply( f );
    Func<App<Either<err>, a>, App<Either<err>, b>> ApplyAlg<Either<err>>.apply<a, b>( App<Either<err>, Func<a, b>> f ) => ap.apply( f );

    App<State<st>, a>   ApplicativeAlg<State<st>>  .pure<a>( a x ) => mo.pure( x );
    App<Either<err>, a> ApplicativeAlg<Either<err>>.pure<a>( a x ) => ap.pure( x );

    Func<Func<a, App<State<st>, b>>, App<State<st>, b>> BindAlg<State<st>>.bind<a, b>( App<State<st>, a> x ) => mo.bind<a, b>( x );

    Func<App<State<st>, a>,  App<State<st>, a>>  AltAlg<State<st>> .alt<a>( App<State<st>, a> x )  => alt.alt( x );
    Func<App<Collection, a>, App<Collection, a>> AltAlg<Collection>.alt<a>( App<Collection, a> x ) => plusC.alt( x );

    App<Collection, a> PlusAlg<Collection>.empty<a>() => plusC.empty<a>();

    Func<b, Func<App<Collection, a>, b>> FoldableAlg<Collection>.foldl<a, b>( Func<b, a, b> f ) => foldC.foldl( f );
    Func<b, Func<App<Collection, a>, b>> FoldableAlg<Collection>.foldr<a, b>( Func<a, b, b> f ) => foldC.foldr( f );

    Func<App<Collection, a>, App<State<st>, App<Collection, b>>> TraversableAlg<Collection, State<st>>.traverse<a, b>( Func<a, App<State<st>, b>> f )
      => trC.traverse( f );
    App<State<st>, App<Collection, a>> TraversableAlg<Collection, State<st>>.sequence<a>( App<Collection, App<State<st>, a>> x )
      => trC.sequence( x );

  }
}
