﻿namespace CqrsVibe.Queries
{
    public interface IQuery
    {
    }
    
    public interface IQuery<out TResult> : IQuery
    {
    }
}
