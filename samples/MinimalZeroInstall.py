import clr
clr.AddReferenceToFile("NanoByte.Common.dll", "ZeroInstall.Services.dll", "ZeroInstall.Store.dll")

import sys
from NanoByte.Common.Tasks import CliTaskHandler
from ZeroInstall.Model import FeedUri, Requirements
from ZeroInstall.Services import ServiceProvider
from ZeroInstall.Services.Feeds import SelectionsManagerExtensions

services = ServiceProvider(CliTaskHandler())
requirements = Requirements(FeedUri(sys.argv[1]))
selections = services.Solver.Solve(requirements)
for implementation in SelectionsManagerExtensions.GetUncachedImplementations(services.SelectionsManager, selections):
    services.Fetcher.Fetch(implementation)
services.Executor.Start(selections)
