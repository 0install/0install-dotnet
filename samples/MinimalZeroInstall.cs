using NanoByte.Common.Tasks;
using ZeroInstall.Model;
using ZeroInstall.Services;

var requirements = new Requirements(new FeedUri(args[0]));
var services = new ServiceProvider(new CliTaskHandler());
var selections = services.Solver.Solve(requirements);
foreach (var implementation in services.SelectionsManager.GetUncachedImplementations(selections))
    services.Fetcher.Fetch(implementation);
services.Executor.Start(selections);
