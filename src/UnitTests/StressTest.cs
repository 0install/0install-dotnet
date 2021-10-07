// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ZeroInstall
{
    public static class StressTest
    {
        /// <summary>
        /// Runs the given <paramref name="action"/> many times in parallel.
        /// </summary>
        /// <exception cref="AggregateException">One or more of the executions of the action threw an exception.</exception>
        public static void Run(Action action, int threadCount = 100)
        {
            var exceptions = new ConcurrentBag<Exception>();
            var threads = new Thread[threadCount];

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        action();
                    }
                    catch (Exception ex)
                    {
                        exceptions.Add(ex);
                    }
                });
                threads[i].Start();
            }

            foreach (var thread in threads)
                thread.Join();

            if (!exceptions.IsEmpty) throw new AggregateException(exceptions);
        }
    }
}
