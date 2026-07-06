namespace Esolang.Piet.Parser;

/// <summary>
/// The exception that is thrown when image content cannot be decoded as a Piet source image.
/// </summary>
public sealed class InvalidImageContentException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidImageContentException"/> class.
    /// </summary>
    public InvalidImageContentException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidImageContentException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InvalidImageContentException(string? message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidImageContentException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public InvalidImageContentException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
