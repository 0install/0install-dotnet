open NanoByte.Common.Tasks
open ZeroInstall.Model
open ZeroInstall.Services

let services = new ServiceProvider(new CliTaskHandler())
let solve = services.Solver.Solve
let uncached = services.SelectionsManager.GetUncached
let fetch = services.Fetcher.Fetch
let execute = services.Executor.Start

let run requirements =
    let selections = solve requirements
    for implementation in (uncached selections.Implementations) do
        fetch implementation
    execute selections

[<EntryPoint>]
let main args =
    ignore(run(new Requirements(new FeedUri(args.[0]))))
    0
