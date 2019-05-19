# How to define a custom effect

E.g. `Maybe` type which represents optional value.

## 1. Define effect algebra

First we need to define Maybe algebra which means to define how to create `Maybe` values and also how to extract values from the inside.

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

These are the functions that we really use in our code, not the functions on interfaces directly.

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

Higher-kinded type is just an interface definition which represents `Maybe :: Type -> Type` type constructor, without generic parameter like `Maybe<a>`.  
Specific implementation of `Maybe` deals with the concrete representation where generic parameter `a` is applied (on the type level) to this type.

```csharp
// Maybe lightweight HKT
public interface Maybe { }
```

