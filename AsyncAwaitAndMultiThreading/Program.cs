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
        #region Initial notes and basics:

        //async methods can't return just any type, it has to be either async void(“fire and forget”) or a Task
        //FooBar1 and FooBar2 are not async methods, they are just normal methods which return a Task. So this method has to be awaited somewhere down in the callers.
        //An async method has to have an await in it. Once you've awaited inside the method, it doest have to specify a return statement, by default it returns a Task eg. FooBar3
        //If you don't need FooBar 3 to return anything and act like a fire and forget function, use async void. But remember, when you use async void, you basically strip away the ability
        //of the caller from awaiting it, or pause/suspend the execution flow. eg. FooBar6 
        //FooBar5 returns some value/type wrapped inside a Task because an async method can't return anything other than a Task or void.

        public Task FooBar1()
        {
            var serviceCall = CustomerInfoAsync();

            return serviceCall;
        }

        public Task FooBar2()
        {
            var allTasks = new List<Task>();
            var serviceCall = CustomerInfoAsync();
            var serviceCall2 = QuoteInfoAsync();

            allTasks.Add(serviceCall);
            allTasks.Add(serviceCall2);

            return Task.WhenAll(allTasks);
        }

        public async Task FooBar3()
        {
            var allTasks = new List<Task>();
            var serviceCall = CustomerInfoAsync();
            var serviceCall2 = QuoteInfoAsync();

            allTasks.Add(serviceCall);
            allTasks.Add(serviceCall2);

            await Task.WhenAll(allTasks);
        }

        /// <summary>
        /// async void should only be used for event handlers. async void is the only way to allow asynchronous event
        /// handlers to work because events do not have return types(thus need not make use of Task and Task<T>).
        /// Event handlers are self sufficient and would have their own try catch. As any exception thrown from
        /// async void can only be handled from inside the method
        /// </summary>
        public async void FooBar4()
        {
            var allTasks = new List<Task>();
            var serviceCall = CustomerInfoAsync();
            var serviceCall2 = QuoteInfoAsync();

            allTasks.Add(serviceCall);
            allTasks.Add(serviceCall2);

            await Task.WhenAll(allTasks);
        }

        public async Task<string> FooBar5()
        {
            var dataFromServiceCall = await CustomerInfoAsync();

            return dataFromServiceCall;
        }

        public async Task FooBar6()
        {
            //can't be awaited, can't be blocked.
            //Exceptions thrown in an async void method can't be caught outside of that method
            FooBar4();

            //you want this line to execute after FooBar4 is completed but it's not possible

            //here we are capable of suspending the execution if we need to
            await FooBar3();

            //note FooBar3 can't be assigned to a variable
            //var val = await FooBar3(); -- can not assign Task to an implicitly typed variable

            var tasks = new List<Task>
      {
        FooBar3(),
        FooBar5()
      };

            await Task.WhenAll(tasks);
        }

        #endregion

        static void Main(string[] args)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try
            {
                //AgreegateServiceCallParallelly();
                ContinueWithExample().Wait();
                #region IO Bound tests
                //Task.Run(async () =>
                //{
                //    var val = await AgreegateServiceCallAsync();
                //    Console.WriteLine(val);
                //});
                //Console.WriteLine("Main Starts");
                //var val = AgreegateServiceCallAsync();
                //Console.WriteLine("Goes back to the caller");
                ////this is a problem, because when we do a .Result, it blocks the UI thread [In Console and Windows Apps]. if we have to call it in 
                ////a sync method we need to wrap it like the above code so that the UI thread gets freed and executes rest of the independent
                ////code in main and exits because the main does not have a caller.
                ////In WebApi and MVC the Main thread only runs startup.cs and program.cs and then the controller and action methods get threadPool threads.
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

        //await means method suspension and going back to the caller. If there is an await which calls another async method having another await inside,
        //the control will drill down till the last await and then go back to the caller of the root await.
        //there is no parallel execution going on here, the only difference between a sync method and this async method having multiple await is releasing of
        //the threads (main thread if windows app or console app, thread pool thread if web app) and going back to the caller and execute rest of the 
        //independently executable code and be ready for the task to complete and come back.
        //So we have a greater number of threads available in our thread pool and our Application scalability increases.
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
        // so all 3 service calls happen parallelly, does some independent work and once when it reaches whenAll() it goes back to its caller that is main()
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

        //always configure await false on library functions which return tasks to avoid issues caused by the calling function.
        //ConfigureAwait(false) tells the await that you do not need to resume on the current context aka any thread from the thread pool is fine when the Task resumes.
        //in case of UI functions you need to have .ConfigureAwait(true) to have the rest of the code get executed on the current context" means "on the UI thread"
        public static async Task<string> IfThisWasALibraryFunctionAsync()
        {
            try
            {
                Console.WriteLine("AgreegateServiceCallAsync strats");
                var customer = CustomerInfoAsync();
                var quote = QuoteInfoAsync();
                var pref = PrefDataAsync();

                Console.WriteLine("Some independent work");

                await Task.WhenAll(customer, quote, pref).ConfigureAwait(false);

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

        //An old way of doing things. Potential long lags incoming because of the heavy CPU bound operations
        public static string AgreegatedCPUBoundOperations()
        {
            var One = RunAMillionIterations(1);
            var Two = RunAMillionIterations(2);
            var Three = RunAMillionIterations(3);

            return ("Task finished \n" + One + "\n " + Two + "\n " + Three);
        }

        //you should have very good reasons to use Task.Run. A good read : https://www.pluralsight.com/guides/using-task-run-async-await
        //1. When you want to await a method that is synchronous, you put it inside Task.Run and wait it, thus you can have an async operation out of a synchronous method
        //2. You want to run synchronous things in parallel which are resource heavy etc

        public async static Task<string> AgreegatedCPUBoundOperationsEfficientAsync()
        {
            var t1 = Task.Run(() => { RunAMillionIterations(1); });
            var t2 = Task.Run(() => RunAMillionIterations(2));
            var t3 = Task.Run(() => RunAMillionIterations(3));

            await Task.WhenAll(t1, t2, t3);

            return ("Task finished \n" + t1 + "\n " + t2 + "\n " + t3);
        }

        //Let's say you have a scenario where the calling function can't be an async, but you still want to achieve parallel execution
        //basically here a synchronous function is calling a bunch of synchronous operations and then wrapping them in a task to execute in parallel.
        //But keep in mind, calling async functions in synchronous code has to be waited.
        public static string AgreegatedCPUBoundOperationsEfficient()
        {
            var t1 = Task.Run(() => RunAMillionIterations(1));
            var t2 = Task.Run(() => RunAMillionIterations(2));
            var t3 = Task.Run(() => RunAMillionIterations(3));

            //you can't do Task.WhenAll() because it's an sync method. which means your method will continue when everything's completed, 
            //and will tie up a thread to just hang around until that time.
            Task.WaitAll(t1, t2, t3);

            return ("Task finished \n" + t1 + "\n " + t2 + "\n " + t3);
        }

        //sync over async will cause deadlock if the synchronization context is a UI context or a ASP.NET request context. Dotnet core doesn't use
        //Synchronization context, so the possibilities of a deadlock is limited.
        public void CallingAsyncFunctionsFromSynchronousCode()
        {
            //using .Result you are blocking execution until it's ready, .Result can only be applied if its a Task<T> not a void Task, see example FooBar3 which returns a void Task
            var val = CustomerInfoAsync().Result; //prone to cause deadlock in UI or ASP.NET legacy apps

            //var val = FooBar3().Result; not allowed
            FooBar3().Wait(); //prone to cause deadlock in UI or ASP.NET legacy apps

            var avoidDeadlock = Task.Run(() => CustomerInfoAsync()).Result; //setting the continuation to Threadpool context to avoid deadlock in legacy apps

            //execution continues before FooBar3 is completed, not recommended unless it's a fire and forget
            FooBar3();
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

        //Note: We have parallelized the synchronous code, now we have 3 tasks which will run in parallel. But in reality sometimes what happens is all the 3 threads
        //will run under the same CPU core. So we will get an illusion of parallelism but what actually happens is called time slicing. If we really want to run things
        // in parallel, we will have to use Parallel.Foreach, but dotnet core latest versions are trying to fight the threads core affinity 
        // https://stackoverflow.com/questions/5009181/parallel-foreach-vs-task-factory-startnew/5009224
        // https://www.youtube.com/watch?v=No7QqSc5cl8&ab_channel=.NETInterviewPreparationvideos

        #endregion


        //there is no difference between await and ContinueWith, when we do a Continuation on a task that task has to be completed
        //inorder for the ContinueWith code block to get executed. The first task gets passed as a parameter of the second task eg. t
        //Note: if the 1st task throws an exception like in the below example and if we await on it, the execution stops there but if we call
        //the ContinueWith on it then the exception will be thrown , catched , and then the rest of the code will get executed.

        public static async Task ContinueWithExample()
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
