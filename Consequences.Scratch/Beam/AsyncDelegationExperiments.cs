namespace Consequences.Scratch.Beam;

// =====================================================================================
//  DELEGATING OVERLOADS, AND WHY THEY ARE NOT MARKED `async`
// =====================================================================================
//
//  NsiImporter has an overload pair like this:
//
//      public static Task<List<Building>> ProcessCollection(string bbox, CancellationToken ct = default) =>
//          ProcessCollection(bbox, BuildingMapper.WithDefaultOccupancyTypes(), ct);   // <- forwards
//
//      public static async Task<List<TReceptor>> ProcessCollection<TReceptor>(...)    // <- does the work
//
//  The first one returns a Task but is NOT marked `async`. That is deliberate and it is
//  what the BCL does — all eight HttpClient.GetAsync overloads forward this way, and only
//  the innermost one does real work. e.g. HttpClient.cs:365
//
//      public Task<HttpResponseMessage> GetAsync(string? requestUri) =>
//          GetAsync(CreateUri(requestUri));
//
//  NOTE ON THE WORD: "delegating overload" has NOTHING to do with C# `delegate` types
//  (Func<>, Action<>, event). It just means "this overload fills in some defaults and
//  hands off to a richer overload of the same name."
//
//  Two things follow, and Demo2/Demo3 are the ones that can actually bite you.
//
// =====================================================================================

public static class AsyncDelegationExperiments
{
    public static void RunAll()
    {
        Demo1_ForwardingAllocatesNothing().GetAwaiter().GetResult();
        Demo2_NonAsyncForwarderThrowsAtTheCallSite().GetAwaiter().GetResult();
        Demo3_AsyncMethodCapturesTheThrowOnTheTask().GetAwaiter().GetResult();
        Demo4_WhyThisMattersForNsiImporter();
    }


    // =================================================================================
    //  DEMO 1 — a forwarder hands back the SAME Task; `async` allocates a new one
    // =================================================================================
    /// <summary>
    /// `async` compiles to a state machine: it attaches a continuation to the inner task,
    /// unwraps the result when it completes, and wraps that result in a SECOND task. A
    /// plain forwarder skips all of that and returns the task it was already holding.
    /// We can prove it with reference equality.
    /// </summary>
    public static async Task Demo1_ForwardingAllocatesNothing()
    {
        Header("DEMO 1 — forwarding returns the same Task object");

        Task<int> forwarded = Forward();
        bool sameObject = ReferenceEquals(forwarded, _lastInnerTask);

        Task<int> wrapped = Wrap();
        bool alsoSameObject = ReferenceEquals(wrapped, _lastInnerTask);

        Console.WriteLine($"  Forward()  => ReferenceEquals(result, inner task) = {sameObject}");
        Console.WriteLine($"  Wrap()     => ReferenceEquals(result, inner task) = {alsoSameObject}");
        Console.WriteLine();
        Console.WriteLine("  Forward() is `=> Inner();`        — no state machine, no allocation.");
        Console.WriteLine("  Wrap()    is `=> await Inner();`  — a state machine wraps the result in a NEW Task.");
        Console.WriteLine($"  (both still produce {await forwarded} — the difference is invisible to the caller)");
        Console.WriteLine();
    }

    private static Task<int>? _lastInnerTask;

    private static Task<int> Inner() => _lastInnerTask = Task.FromResult(42);

    /// <summary>The forwarder. Returns Task, but is not `async` — nothing to await.</summary>
    private static Task<int> Forward() => Inner();

    /// <summary>The wasteful version. Same observable result, extra machinery.</summary>
    private static async Task<int> Wrap() => await Inner();


    // =================================================================================
    //  DEMO 2 — a non-async forwarder throws IMMEDIATELY
    // =================================================================================
    /// <summary>
    /// Because there is no state machine, a synchronous throw inside the method body
    /// escapes the moment you CALL it. You never receive a Task at all.
    /// </summary>
    public static Task Demo2_NonAsyncForwarderThrowsAtTheCallSite()
    {
        Header("DEMO 2 — non-async forwarder: throws at the CALL");

        try
        {
            // Note: not awaited. Just called.
            Task<List<int>> task = EagerAsync("   ");
            Console.WriteLine($"  got a Task back, status = {task.Status}");
        }
        catch (ArgumentException e)
        {
            Console.WriteLine($"  💥 threw at the CALL, before any Task existed: {e.GetType().Name}");
        }

        Console.WriteLine();
        Console.WriteLine("  A caller can wrap the CALL in try/catch and it works.");
        Console.WriteLine();
        return Task.CompletedTask;
    }

    /// <summary>NOT async. Validation throws synchronously, straight out to the caller.</summary>
    private static Task<List<int>> EagerAsync(string request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request);
        return CoreAsync(request);
    }


    // =================================================================================
    //  DEMO 3 — an `async` method hides the same throw inside the Task
    // =================================================================================
    /// <summary>
    /// Mark the SAME method `async` and every exception — even from the first line, before
    /// any await — gets captured and parked on the returned Task. Nothing throws until the
    /// caller awaits.
    /// </summary>
    public static async Task Demo3_AsyncMethodCapturesTheThrowOnTheTask()
    {
        Header("DEMO 3 — async method: the throw rides on the Task");

        Task<List<int>> task;

        try
        {
            task = DeferredAsync("   ");        // identical call, identical validation
            Console.WriteLine($"  no throw at the call. Task status = {task.Status}");
        }
        catch (ArgumentException)
        {
            Console.WriteLine("  threw at the call — (this line will not run)");
            return;
        }

        // The exception was real, it just went somewhere else.
        try
        {
            await task;
        }
        catch (ArgumentException e)
        {
            Console.WriteLine($"  💥 surfaced only on await: {e.GetType().Name}");
        }

        Console.WriteLine();
        Console.WriteLine("  Same code, same bug, different moment. A caller who wraps only the CALL");
        Console.WriteLine("  in try/catch catches NOTHING here.");
        Console.WriteLine();
    }

    /// <summary>`async`. Validation is captured by the state machine, not thrown.</summary>
    private static async Task<List<int>> DeferredAsync(string request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request);
        return await CoreAsync(request);
    }


    // The "real work", stubbed: turn a request into some numbers.
    private static async Task<List<int>> CoreAsync(string request)
    {
        await Task.Delay(1);
        return [.. request.Trim().Select(c => (int)c)];
    }


    // =================================================================================
    //  DEMO 4 — what this means for NsiImporter
    // =================================================================================
    public static void Demo4_WhyThisMattersForNsiImporter()
    {
        Header("DEMO 4 — what this means for NsiImporter");

        Console.WriteLine("  1. NAMING. The `Async` suffix follows the RETURN TYPE, not the keyword.");
        Console.WriteLine("     Demo1's Forward() has no `async` but still returns Task, so it would");
        Console.WriteLine("     still be named ForwardAsync. Both ProcessCollection overloads should");
        Console.WriteLine("     be ProcessCollectionAsync — splitting the suffix across an overload");
        Console.WriteLine("     set implies one of them is synchronous, which is worse than neither.");
        Console.WriteLine();
        Console.WriteLine("  2. VALIDATION. Today ProcessCollection does no argument checking, so the");
        Console.WriteLine("     eager/deferred difference is invisible. The moment you add");
        Console.WriteLine();
        Console.WriteLine("         ArgumentException.ThrowIfNullOrWhiteSpace(boundingBox);");
        Console.WriteLine();
        Console.WriteLine("     it matters: in the non-async forwarder it throws at the call site,");
        Console.WriteLine("     in the async generic overload it is parked on the Task instead.");
        Console.WriteLine();
        Console.WriteLine("  3. SAME IDEA AS THE ITERATORS. StreamCollection has this problem already,");
        Console.WriteLine("     via a different mechanism: an async iterator defers its ENTIRE body");
        Console.WriteLine("     until first enumeration, so the bbox, the FWLink resolve and the HTTP");
        Console.WriteLine("     request all happen later. Fix is the same shape — a non-iterator,");
        Console.WriteLine("     non-async outer method that validates, then hands off to the inner one.");
        Console.WriteLine();
    }


    private static void Header(string title)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 78));
        Console.WriteLine($"  {title}");
        Console.WriteLine(new string('=', 78));
    }
}
