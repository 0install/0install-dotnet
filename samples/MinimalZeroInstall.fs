open NanoByte.Common.Tasks
open ZeroInstall.Model
open ZeroInstall.Services
open ZeroInstall.Services.Feeds

let services = new ServiceProvider(new CliTaskHandler())
let solve = services.Solver.Solve
let uncached = services.SelectionsManager.GetUncachedImplementations
let fetch = services.Fetcher.Fetch
let execute = services.Executor.Start

let run requirements =
    let selections = solve requirements
    for implementation in (uncached selections) do
        fetch implementation
    execute selections

[<EntryPoint>]
let main args =
    ignore(run(new Requirements(new FeedUri(args.[0]))))
    0
