---
uid: ZeroInstall.Services.Executors
summary: Launches implementations and injects the selected dependencies in various execution environments.
remarks: |
  The executor system supports running applications in different environments:
  - **Native**: Direct execution on the current OS (default)
  - **Docker**: Run Linux binaries in Docker containers
  - **Wine**: Run Windows binaries on Linux
  - **WSL**: Run Linux binaries on Windows
  - **Windows Sandbox**: Run Windows binaries in isolated sandbox
  
  ## Usage
  
  ### Native Execution (Default)
  ```csharp
  var executor = new Executor(implementationStore);
  executor.Start(selections);
  ```
  
  ### Docker Execution
  ```csharp
  var executor = new Executor(implementationStore, new DockerStrategy("ubuntu:latest"));
  executor.Start(selections);
  ```
  
  ### Wine Execution
  ```csharp
  var executor = new Executor(implementationStore, new WineStrategy());
  executor.Start(selections);
  ```
  
  ### WSL Execution
  ```csharp
  var executor = new Executor(implementationStore, new WSLStrategy("Ubuntu"));
  executor.Start(selections);
  ```
  
  ### Windows Sandbox Execution
  ```csharp
  var executor = new Executor(implementationStore, new WindowsSandboxStrategy());
  executor.Start(selections);
  ```
  
  ## Creating Custom Strategies
  
  You can create custom execution strategies by implementing `IExecutionStrategy`:
  
  ```csharp
  public class CustomStrategy : IExecutionStrategy
  {
      public IPathMapper PathMapper { get; }
      public IExecutionContext CreateContext() { /* ... */ }
      public void ApplyEnvironmentBinding(...) { /* ... */ }
      public string DeployExecutable(...) { /* ... */ }
      public void FinalizeExecution(...) { /* ... */ }
      public Process Start(...) { /* ... */ }
  }
  ```
---
