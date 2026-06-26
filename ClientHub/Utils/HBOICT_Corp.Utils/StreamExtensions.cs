using System;
using System.Collections.Generic;
using System.Text;

namespace WinterRose.Web.Utils;

/// <summary>
/// Provides extension methods for working with streams
/// </summary>
public static class StreamExtensions
{
    extension(Stream s)
    {
        /// <summary>
        /// Creates a StreamReader for the given stream and executes the provided action with it. The StreamReader will be disposed after the action is executed. By default, the underlying stream will remain open after the StreamReader is disposed, but this can be changed by setting leaveOpen to false. The encoding can also be specified, defaulting to UTF-8 if not provided.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="leaveOpen"></param>
        /// <param name="encoding"></param>
        public void UseStreamReader(Action<StreamReader> action, bool leaveOpen = true, Encoding? encoding = null)
        {
            using var reader = new StreamReader(s, encoding ?? Encoding.UTF8, true, 1024, leaveOpen: leaveOpen);
            action(reader);
        }

        /// <summary>
        /// Creates a StreamReader for the given stream and executes the provided function with it, returning the result. The StreamReader will be disposed after the function is executed. By default, the underlying stream will remain open after the StreamReader is disposed, but this can be changed by setting leaveOpen to false. The encoding can also be specified, defaulting to UTF-8 if not provided.
        /// </summary>
        /// <param name="leaveOpen"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public StreamReader UseStreamReader(bool leaveOpen = true, Encoding? encoding = null)
        {
            return new StreamReader(s, encoding ?? Encoding.UTF8, true, 1024, leaveOpen: leaveOpen);
        }
    }
}
