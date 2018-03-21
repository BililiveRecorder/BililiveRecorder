/**
 * Author: Roger Lipscombe
 * Source: https://stackoverflow.com/a/7472334
 * */
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BililiveRecorder.Core
{
    internal static class Repeat
    {
        public static Task Interval(
            TimeSpan pollInterval,
            Action action,
            CancellationToken token)
        {
            // We don't use Observable.Interval:
            // If we block, the values start bunching up behind each other.
            return Task.Factory.StartNew(
                () =>
                {
                    for (; ; )
                    {
                        action();
                        if (token.WaitCancellationRequested(pollInterval))
                            break;
                    }
                }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }
    }

    static class CancellationTokenExtensions
    {
        public static bool WaitCancellationRequested(
            this CancellationToken token,
            TimeSpan timeout)
        {
            return token.WaitHandle.WaitOne(timeout);
        }
    }
}
