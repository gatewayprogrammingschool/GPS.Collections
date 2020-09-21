# GPS.Collections

[![Build status](https://ci.appveyor.com/api/projects/status/lgpu5pgn12nnl91x/branch/master?svg=true)](https://ci.appveyor.com/project/sharpninja/gps-collections/branch/master)

Library of specialized data structures .

* (_new_) __OrderedConcurrentDictionary__ - `IDictionary<TKey, TValue>` that guarantees order of data insertion is preserved on the `Keys` collection and enumerator while maintaining thread-safe inserts and random access.
* __MatrixArray__ - Spacial data structure providing always-sorted direct access for integer-indexed data.

## Installation

__GPS.Collections__ is available through Nuget.

```powershell

nuget install GPS.Collections
```

## GPS.Collections.OrderedConcurrentDictionary&lt;TKey, TValue&gt;

`IDictionary<TKey, TValue>` that guarantees order of data insertion is preserved on the `Keys` collection and enumerator while maintaining thread-safe inserts and random access.  

### Additional Methods

```csharp
/// <summary>
/// Reorders the data in the collection according the supplied selector,
/// IComparer and ReorderDirection specified.
/// </summary>
/// <param name="selector">Func of the selector.</param>
/// <param name="direction">ReorderDirection specifying the direction
/// to sort the data.</param>
public void Reorder(Func<(TKey Key, TValue Value), TKey> selector
            , ReorderDirection direction = ReorderDirection.Ascending) { ... }


/// <summary>
/// Reorders the data in the collection according the supplied selector,
/// IComparer and ReorderDirection specified.
/// </summary>
/// <param name="selector">Func of the selector.</param>
/// <param name="comparer">IComparer instance that performs the test
/// to determine the ordinality of two datum in the collection.</comparer>
/// <param name="direction">ReorderDirection specifying the direction
/// to sort the data.</param>
public void Reorder(Func<(TKey Key, TValue Value), TKey> selector
            , IComparer<TKey> comparer
            , ReorderDirection direction = ReorderDirection.Ascending) { ... }
```

## GPS.Collections.MatrixArray&lt;T&gt;

### Benchmarks

| | Loading | Positive Random | Open-Ended Random | Find Last | Enumerate | Sum | Memory |
|-|---------|-----------------|-------------------|-----------|-----------|-----|--------|
| MatrixArray | 49 | 67 | 61 | _39_ | 24 | 240 | _1494190_ |
| Dictionary | __38__ | __28__ | __28__ | __0__ | __7__ | __101__ | __668789__ |
| SortedDictionary | _409_ | _421_ | _464_ | 25 | _28_ | _1347_ | 670201 |

### Description of MatrixArray

The `MatrixArray` is an auto-growing array of arrays that provide a contiguous indexing mechanism.  At full depth, indices may be any `Int32` value, both positive and negative.  This allows for natural indexing of data without sliding offsets.

### MatrixArray Usage

#### Negative Indices

```csharp
var matrixArray = new MatrixArray<string>();

matrixArray[-1] = "negative one";
matrixArray[-2] = "negative two";

Console.WriteLine($"Range: {matrixArray.Lowest} to {matrixArray.Highest}");
// Range: -2 to -1
```

#### Sparse Indices

```csharp
var matrixArray = new MatrixArray<string>();

matrixArray[-1000] = "negative one-thousand";
matrixArray[1000] = "one-thousand";

Console.WriteLine($"Range: {matrixArray.Lowest} to {matrixArray.Highest}");
// Range: -1000 to 1000

Console.WriteLine($"Count: {matrixArray.Count}");
// Count: 2001
```

#### Auto-growing

```csharp
var matrixArray = new MatrixArray<string>();

matrixArray[-1] = "negative one";
matrixArray[1] = "one";

Console.WriteLine($"Count: {matrixArray.Count}");
// Count: 3

matrixArray[3] = "three";
Console.WriteLine($"Count: {matrixArray.Count}");
// Count: 5

matrixArray[2] = "two";
Console.WriteLine($"Count: {matrixArray.Count}");
// Count: 5
```

#### Add Range

```csharp
var matrixArray = new MatrixArray<string>();

matrixArray[0] = "zero";
matrixArray.AddRange(new [] { "one", "two" } );
Console.WriteLine($"Range: {matrixArray.Lowest} to {matrixArray.Highest}");
// Range: 0 to 2
```

#### Add Range At

```csharp
var matrixArray = new MatrixArray<string>();

matrixArray.AddRange(21, new [] { "twenty-one", "twenty-two" } );
Console.WriteLine($"Range: {matrixArray.Lowest} to {matrixArray.Highest}");
// Range: 21 to 22
```

#### IndexOf

```csharp
var matrixArray = new MatrixArray<string>();

matrixArray[10] = "find me";
matrixArray.AddRange(new [] { "me too", "me three" } );
Console.WriteLine($"IndexOf: \"find me\" = {matrixArray.IndexOf("find me")}");
// IndexOf: "find me" = 10

Console.WriteLine($"IndexOf: \"me three\" = {matrixArray.IndexOf("me three")}");
// IndexOf: "me three" = 12
```

#### Note about LinkedArray

`LinkedArray` was left in the package for backwards compatibility, but there is no
reason to use it for new code.  Replacing it with `MatrixArray` is a simple search-replace
process.
