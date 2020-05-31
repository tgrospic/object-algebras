# Experiments with higher-kinded types, object algebras and effects

The aim of this library is to explore solutions for the _expression problem_ with implementation of _higher-kinded types_ in C#. It is composed from three major components. 

## 1. Lightweight higher-kinded polymorphism

`App<_, _>` type definition based on [Lightweight higher-kinded polymorphism][light-hkt] paper. The purpose of this type is to provide a way to define type level application.

E.g. to define a functor, we need generic argument `F` that can represent type constructor `F<a>`. We cannot do that but we can represent this with another type `App<F, a>` which can be seen as type level application `F(a)`.

```csharp
// Functor algebra
public interface FunctorAlg<F> {
  // Surprisingly C# definition does not look very strange.
  // `map` accepts a function and returns it with parameters lifted in `F`.
  // map :: (a -> b) -> (F a -> F b)
  Func<App<F, a>, App<F, b>> map<a, b>( Func<a, b> f );
}
```
Having `App` abstraction enables us to overcome some limitations present in the great functional library for C# [language-ext][lang-ext] by Paul Louth. Most importantly, interface definition can be free of specific generic type `a` and `b` which gives a very straightforward translation from Haskell. To be more precise, this implementation follows PureScript control hierarchy, which is more granular (e.g. [Functor][purescript-functor], [Bind][purescript-bind]). ​Also defining functions in the curried form is extremely useful and well suited for definitions e.g. [JSON parser][json-parser].

## 2. Object algebras

_Object algebras_ as presented in the very inspiring paper (for OO programmer :)) [Extensibility for the Masses][ext-for-masses]. It offers a solution to the expression problem with only lightweight language features (although it's not clear to me how to express higher-order effects without represenation for higher-kinded types).

E.g. the definition of a `State` is expressed as multi-sorted object algebra (family polymorphism).

```csharp
public interface StateAlg<F, G, s> {
  // Introduction
  App<F, a> makeState<a>( Func<s, (App<G, a>, s)> f );

  // Elimination
  (App<G, a>, s) runState<a>( App<F, a> p, s init );
}
```

_This is the source of `Alg` suffix and probably also other syntax differences with C# guidelines. Generic parameters starting in lowercase are "normal" generic types while capital letters represent type constructors (brand types)._

[ext-for-masses]: https://www.cs.utexas.edu/~wcook/Drafts/2012/ecoop2012.pdf
[light-hkt]: https://www.cl.cam.ac.uk/~jdy22/papers/lightweight-higher-kinded-polymorphism.pdf
[lang-ext]: https://github.com/louthy/language-ext
[purescript-functor]: https://github.com/purescript/purescript-prelude/blob/master/src/Data/Functor.purs
[purescript-bind]: https://github.com/purescript/purescript-prelude/blob/master/src/Control/Bind.purs
[json-parser]: Parser/Parser.Json.cs

## 3. Effects - _interaction_ with the _context_

The third component is the glue for the first two and shapes the perspective on effects. It is influenced by Oleg Kiselyov excellent work presented on his site. He has numerous explanations and examples that have been very helpful in translating Haskell code to C#.  
This description of (higher-order) effects is what guided the implementation of this library and best describes how to approach it.

> We argue that the central problem of the interaction of higher-order programming with various kinds of effects can be tackled by eliminating the distinction: higher-order facility is itself an effect, not too different from state effect.  
http://okmij.org/ftp/Computation/having-effect.html#Conclusions

The _expression_ in (effectful) language is created as a tuple from `App<F, a>` which represent higher-kinded value with `F` as a type constructor applied to a generic value of type `a` and the _context_ as a generic type (named shortly `__`), but _bounded_ with one or many (effect) algebras, interfaces like the functor `FoldableAlg<F>`. C# extension methods are used to combine (extend) these _expressions_.

```csharp
public static (App<M, Unit>, __) traverse_<F, M, a, b, __>(
    this Func<a, (App<M, b>, __)> f,
         (App<F, a>, __ ctx)      x
  )
  where __ : FoldableAlg<F>, ApplicativeAlg<M> =>
    x.foldr( x.ctx.pure<M, Unit, __>( Unit.Val ) )( ( aa, unit ) => f( aa ).i_( unit ) );

public static (App<M, Unit>, __) for_<F, M, a, b, __>(
    this (App<F, a>, __)       x,
         Func<a, (App<M, b>, __)> f
  )
  where __ : FoldableAlg<F>, ApplicativeAlg<M> => f.traverse_( x );
```

These _effect_ expressions can be seen as definitions of interactions with the computation context (producing effects). They are the central tools for writing domain logic and building bigger programs from smaller pieces.

Boilerplate code is needed to _lift_ algebra functions with a given context - to expression (DSL) level.

```csharp
// Functor example with map "lifted"
public static (App<F, b>, __) map<F, a, b, __>( this (App<F, a> x, __ ctx) exp, Func<a, b> f )
  where __ : FunctorAlg<F> => (exp.ctx.map( f )( exp.x ), exp.ctx);

// Foldable used in previous examples for `traverse_` and `for_`
public static Func<Func<a, b, b>, b> foldr<F, a, b, __>( this (App<F, a> a, __ ctx) x, b init )
  where __ : FoldableAlg<F> => f => x.ctx.foldr( f )( init )( x.a );

// Note: when a method doesn't require `F` in input parameters, it must be explicitly 
// specified on the calling site. So it's useful for `F` to be part of the tuple
// although value is not used (and there is not any). The only purpose is for C# 
// compiler to infer its type.
public static (App<F, char>, __) @char<F, __>( this (F, __) exp, char c )
  where __ : ParserAlg<F, char> => exp.satisfy( ci => ci == c );
```
Techniques used in this library can be applied in any language with bounded polymorphism like F#, Kotlin, or Java, for which similar libraries exist.

# Effect handlers

Until now we didn't mentioned instances or interpreters for our embeded language. From an effects point of view, they represent effect handlers, where the _real work_ is done.

To implement a `char` parser, only one class is needed that inherits (extends) the generic implementation of the state monad with choice.

```csharp
// Char parser implementation with String as both, parser state (input) and parser error.
class ParserCharImpl : StateEitherImpl<string, string>, ParserAlg<State<string>, char> {
  StateAlg<State<string>, Either<string>, string> st = StateImpl<string, Either<string>>.Instance;
  EitherAlg<Either<string>, string>               ea = EitherImpl<string>.Instance;
  
  // Parser implementation
  App<State<string>, char> ParserAlg<State<string>, char>.satisfy( Func<char, bool> f ) =>
    st.makeState( ss => {
      if ( ss != "" && f( ss[0] ) ) {
        return ea.right( ss[0] ).pair( ss.Substring( 1 ) );
      } else if ( ss != "" ) {
        return ea.left<char>( $"Unexpected char: '{ss[0]}'" ).pair( ss );
      } else {
        return ea.left<char>( $"Unexpected EOF" ).pair( ss );
      }
    } );
}
```

# Desired language features

Combining multiple handlers is done with explicit implementation of multiple interfaces, e.g. [StateEitherImpl](Algebras.Impl/StateEitherImpl.cs). For this purpose, something like _explicit mixins_ would be a useful feature. In essence, this is where the dependency graph is created and existing techniques for dependency injection can be used. _Implicits_ also fall into this category as a constant dependency.

Although `App<F, a>` type as a representation for higher-kinds is usable in this form, it can be very hard to read for nested types. With only syntactic level support from the compiler, the improvement would be enormous.

Working with multiple generic parameters sometimes requires the explicit definition of only one parameter while others can be inferred. But because of this one, all parameters must be specified. Wildcard as a generic parameter would greatly reduce duplicating types known by the compiler.

# How to define a (custom) effect

This example shows how to create `Maybe` type which represents optional value.

## 1. Define effect algebra

First we need to define a `Maybe` algebra which means we need to define signature creation and elimination `Maybe` values. `Maybe` has two constructors, so to deconstruct we need two functions, handle each case appropriately, and unify the result type.

```csharp
// Maybe algebra
public interface MaybeAlg<F> {
  // Introduction
  App<F, a> just<a>( a aa );
  App<F, a> nothing<a>();

  // Elimination
  b runMaybe<a, b>( App<F, a> ma, Func<b> onNothing, Func<a, b> onJust );
}
```

## 2. Define _lifted_ functions with the _context_ (boilerplate code)

These are the functions we actually use in our code, not the functions on interfaces directly. They represent expressions in our DSL language.

```csharp
// nothing
public static (App<F, a>, __) nothing<F, a, __>( this (F, __ ctx) ma )
  where __ : MaybeAlg<F> => (ma.ctx.nothing<a>(), ma.ctx);

// just
public static (App<F, a>, __) just<F, a, __>( this (F, __ ctx) ma, a aa )
  where __ : MaybeAlg<F> => (ma.ctx.just<a>( aa ), ma.ctx);

// runMaybe
public static b runMaybe<F, a, b, __>( this (App<F, a> x, __ ctx) ma, Func<b> onNothing, Func<a, b> onJust )
  where __ : MaybeAlg<F> => ma.ctx.runMaybe( ma.x, onNothing, onJust );
```
## 3. Define _higher-kinded_ representation (type constructor)

The higher-kinded type is just an empty interface definition which represents `Maybe :: Type -> Type` type constructor, without generic parameter like `Maybe<a>`.  
Specific implementation of `Maybe` deals with the concrete representation where generic parameter `a` is applied (on the type level) to this type `App<Maybe, a>`.

```csharp
// Maybe lightweight HKT
public interface Maybe { }
```
