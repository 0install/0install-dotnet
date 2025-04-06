using ZeroInstall.Services;
using ZeroInstall.Services.Feeds;

var services = new ServiceProvider(new CliTaskHandler());

var selections = services.Solver.Solve(new FeedUri("https://apps.0install.net/0install/0install-dotnet.xml"));

foreach (var implementation in services.SelectionsManager.GetUncachedImplementations(selections))
    services.Fetcher.Fetch(implementation);

services.Executor.Inject(selections)
        .AddArguments(args)
        .Start()
        ?.WaitForExit();
