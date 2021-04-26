using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncAwaitAndMultiThreading
{
    class Program
    {
        static void Main(string[] args)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                AgreegateServiceCallParallelly();
                #region IO Bound tests
                ////Task.Run(async () =>
                ////{
                ////    var val = await AgreegateServiceCallAsync();
                ////    Console.WriteLine(val);
                ////});
                //Console.WriteLine("Main Starts");
                //var val = AgreegateServiceCallAsync();
                //Console.WriteLine("Goes back to the caller");
                ////this is a problem, because when we do a .Result, it blocks the UI thread [In Console and windows Apps]. if we have to call it in 
                ////a sync method we need to wrap it like the above code so that the UI thread gets freed and axecutes rest of the indepedent
                ////code in main and exits because the main does not have a caller.
                ////In WebApi and MVC the Main thread only startup.cs and program.cs and then the controller and action methods gets threadPool threads.
                //Console.WriteLine(val.Result);
                //Console.WriteLine("Main ends");
                #endregion
            }
            catch (Exception ex)
            {
                throw ex;
            }
            stopwatch.Stop();
            Console.WriteLine(stopwatch.ElapsedMilliseconds);
            Console.ReadKey(true);
        }

        //await means method suspention and going back to caller. If there is an await which calls another async method having another await inside,
        //the control will drill down till the last await and then go back to caller of the root await.
        //there is no parallel execution going on here, the only difference between a sync method and this async method having multiple await is releasing of
        //the threads (main thread if windows app or console app, thread pool thread if web app) and going back to the caller and execute rest of the 
        //independentely executable code and be ready for the task to complete and come back.
        //So we have greater number of threads available in our thread pool and our Application sclability increases.
        public static async Task<string> AgreegateServiceCallAsyncMultypleAwaiter()
        {
            Console.WriteLine("AgreegateServiceCallAsyncMultypleAwaiter strats");

            var customer = await CustomerInfoAsync();

            Console.WriteLine("Some independent work");

            var quote = await QuoteInfoAsync();

            var pref = await PrefDataAsync();

            Console.WriteLine("AgreegateServiceCallAsyncMultypleAwaiter ends");

            return ("Task finished \n" + customer + "\n " + quote + "\n " + pref);
        }

        //here its parallel execution, CustomerInfoAsync() finds await inside , gets suspended and comes back and calls QuoteInfoAsync() and so on.
        // so all 3 service calls happens parallelly, does some independent work and once when it reaches whenAll() it goes back to its caller that is main()
        //and does some more independent work in the caller and then awaits for the result to come back.
        public static async Task<string> AgreegateServiceCallAsync()
        {
            try
            {
                Console.WriteLine("AgreegateServiceCallAsync strats");
                var customer = CustomerInfoAsync();
                var quote = QuoteInfoAsync();
                var pref = PrefDataAsync();

                Console.WriteLine("Some independent work");

                await Task.WhenAll(customer, quote, pref);

                Console.WriteLine("AgreegateServiceCallAsync ends");

                return ("Task finished \n" + customer.Result + "\n " + quote.Result + "\n " + pref.Result);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #region external service calls
        static async Task<string> CustomerInfoAsync()
        {
            try
            {
                Console.WriteLine("CustomerInfoAsync starts");
                var client = new WebClient();

                string data = await client.DownloadStringTaskAsync("https://jsonplaceholder.typicode.com");
                Thread.Sleep(5000);
                Console.WriteLine("CustomerInfoAsync ends");
                return string.Format("Thread Name: {0} Thread Pool Thread : {1}",
                     Thread.CurrentThread.ManagedThreadId, Thread.CurrentThread.IsThreadPoolThread);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        static async Task<string> QuoteInfoAsync()
        {
            try
            {
                Console.WriteLine("QuoteInfoAsync starts");
                var client = new WebClient();

                string data = await client.DownloadStringTaskAsync("https://jsonplaceholder.typicode.com");
                Thread.Sleep(10000);
                Console.WriteLine("QuoteInfoAsync ends");
                //throw new NullReferenceException();
                return string.Format("Thread Name: {0} Thread Pool Thread : {1}",
                     Thread.CurrentThread.ManagedThreadId, Thread.CurrentThread.IsThreadPoolThread);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        static async Task<string> PrefDataAsync()
        {
            try
            {
                Console.WriteLine("PrefDataAsync starts");
                var client = new WebClient();

                string data = await client.DownloadStringTaskAsync("https://jsonplaceholder.typicode.com");
                Thread.Sleep(5000);
                Console.WriteLine("PrefDataAsync ends");
                //throw new DivideByZeroException();
                return string.Format("Thread Name: {0} Thread Pool Thread : {1}",
                     Thread.CurrentThread.ManagedThreadId, Thread.CurrentThread.IsThreadPoolThread);
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        #endregion

        #region parallel execution of synchronous code
        //takes 16 sec
        public static string AgreegateServiceCall()
        {
            var One = RunAMillionIterations(1);
            var Two = RunAMillionIterations(2);
            var Three = RunAMillionIterations(3);

            return ("Task finished \n" + One + "\n " + Two + "\n " + Three);
        }

        //takes 8 sec
        public static string AgreegateServiceCallParallelly()
        {
            //here we have parallelized the sequential code, now we have 3 tasks which will run in parallel. But in reality waht happens is all the 3 threads
            //will run under same CPU core. So we will get an illusion of parallalism but what actually happening is call time slicing. If we really want to run things
            // in parallel, we will have to use Parallel.Foreach
            // https://stackoverflow.com/questions/5009181/parallel-foreach-vs-task-factory-startnew/5009224
            // https://www.youtube.com/watch?v=No7QqSc5cl8&ab_channel=.NETInterviewPreparationvideos
            var t1 = Task.Run(() => { RunAMillionIterations(1); });
            var t2 = Task.Run(() => RunAMillionIterations(2));
            var t3 = Task.Run(() => RunAMillionIterations(3));

            //you can't do Task.WhenAll() because its an sync method. which means your method will continue when everything's completed, 
            //and will tie up a thread to just hang around until that time.
            Task.WaitAll(t1, t2, t3);

            return ("Task finished \n" + t1 + "\n " + t2 + "\n " + t3);
        }

        static string RunAMillionIterations(int clientNumber)
        {
            for (int i = 0; i < 1000000000; i++)
            {
                int j = i + 4;
            }
            return string.Format("Client {0} Thread Name: {1} Thread Pool Thread : {2}",
                clientNumber, Thread.CurrentThread.ManagedThreadId, Thread.CurrentThread.IsThreadPoolThread);
        }
        #endregion

        //there is no difference between await and ContinueWith, when we do a Continuation on a task that task has to be completed
        //inorder for the ContinueWith code block to get executed. The first task gets passed as a parameter of the second task eg. t
        //Note: if the 1st task throws an excepton like in the below example and if we await on it, the execution stops there but if we call
        //the ContinueWith on it then the exception will be thrown , catched , and then the rest of the code will get executed.

        public async Task ContinueWithExample()
        {
            try
            {
                Task task = Task.Run(() => { throw new Exception("aaaa"); })
                .ContinueWith(t =>
                {
                    if (t.IsCanceled || t.IsFaulted)
                    {
                        Console.WriteLine(1);
                    }
                    Console.WriteLine(2);
                })
                .ContinueWith((t) =>
                {
                    Console.WriteLine(3);
                });

                await task;
                Console.WriteLine(4);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            Console.ReadLine();
        }
    }
}

