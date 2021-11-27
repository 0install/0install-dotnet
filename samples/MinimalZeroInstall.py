import clr
clr.AddReferenceToFile("NanoByte.Common.dll", "ZeroInstall.Services.dll", "ZeroInstall.Store.dll")

import sys
from NanoByte.Common.Tasks import CliTaskHandler
from ZeroInstall.Model import FeedUri, Requirements
from ZeroInstall.Services import ServiceProvider

services = ServiceProvider(CliTaskHandler())
requirements = Requirements(FeedUri(sys.argv[1]))
selections = services.Solver.Solve(requirements)
for implementation in services.SelectionsManager.GetUncachedImplementations(selections):
    services.Fetcher.Fetch(implementation)
services.Executor.Start(selections)
