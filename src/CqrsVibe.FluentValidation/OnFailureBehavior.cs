namespace CqrsVibe.FluentValidation
{
    /// <summary>
    /// Behavior when validation failed
    /// </summary>
    public enum OnFailureBehavior
    {
        ThrowException,
        ReturnEither
    }
}