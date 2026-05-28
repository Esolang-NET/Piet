namespace Esolang.Generator;

/// <summary>
/// Specifies the return type of the generated method.
/// </summary>
public enum MethodReturnKind
{
    /// <summary>The return type is invalid or unsupported.</summary>
    Invalid,
    /// <summary>The method returns void.</summary>
    Void,
    /// <summary>The method returns int.</summary>
    Int32,
    /// <summary>The method returns string.</summary>
    String,
    /// <summary>The method returns string (nullable).</summary>
    NullableString,
    /// <summary>The method returns Task.</summary>
    Task,
    /// <summary>The method returns Task&lt;int&gt;.</summary>
    TaskInt32,
    /// <summary>The method returns Task&lt;string&gt;.</summary>
    TaskString,
    /// <summary>The method returns Task&lt;string?&gt;.</summary>
    TaskNullableString,
    /// <summary>The method returns ValueTask.</summary>
    ValueTask,
    /// <summary>The method returns ValueTask&lt;int&gt;.</summary>
    ValueTaskInt32,
    /// <summary>The method returns ValueTask&lt;string&gt;.</summary>
    ValueTaskString,
    /// <summary>The method returns ValueTask&lt;string?&gt;.</summary>
    ValueTaskNullableString,
    /// <summary>The method returns IEnumerable&lt;byte&gt;.</summary>
    IEnumerableByte,
    /// <summary>The method returns IAsyncEnumerable&lt;byte&gt;.</summary>
    IAsyncEnumerableByte,
}
