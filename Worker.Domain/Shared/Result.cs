using System;
using System.Collections.Generic;
using System.Text;

namespace Worker.Domain.Shared
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T? Value { get; }
        public string? Error { get; }
        public bool IsFailure => !IsSuccess;

        private Result(bool isSuccess, T? value, string? error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }
        public static Result<T> Success(T value) => new Result<T>(true, value, null);
        public static Result<T> Failure(string errorMessage) => new Result<T>(false, default, errorMessage);
    }

    public class Result
    {
        public bool IsSuccess { get; }
        public string? Error { get; }
        public bool IsFailure => !IsSuccess;
        private Result(bool isSuccess, string? error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }
        public static Result Success() => new (true, null);
        public static Result Failure(string error) => new Result(false, error);
    }
}
